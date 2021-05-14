using System;
using Godot;

// TODO: Allow for initially private integrated server to open itself up to the public.
public class Server : Game
{
    internal Player LocalPlayer { get; private set; }
    private bool _isLocalPlayerConnected = false;

    public NetworkedMultiplayerENet Peer => (NetworkedMultiplayerENet)Multiplayer.NetworkPeer;
    public bool IsRunning => Peer != null;
    public bool IsSingleplayer { get; private set; }

    public override void _Ready()
    {
        base._Ready();
        Multiplayer.Connect("network_peer_connected", this, nameof(OnPeerConnected));
        Multiplayer.Connect("network_peer_disconnected", this, nameof(OnPeerDisconnected));
    }

    public ushort StartSingleplayer()
    {
        for (var retries = 0; ; retries++) {
            try {
                IsSingleplayer = true;
                // TODO: When `get_local_port` is available, just use port 0 for an auto-assigned port.
                //       Also see this PR: https://github.com/godotengine/godot/pull/48235
                var port = (ushort)GD.RandRange(42000, 43000);
                Start(port, "127.0.0.1", 1);
                return port;
            } catch (Exception ex) {
                // Do throw the "Server is already running" exception.
                // 3 retries should be good enough to find a random unused port.
                if ((ex is InvalidOperationException) || (retries == 2)) throw;
            }
        }
    }
    public void Start(ushort port)
        => Start(port, "*", 32);
    private void Start(ushort port, string bindIP, int maxClients)
    {
        if (IsRunning) throw new InvalidOperationException("Server is already running");

        var peer = new NetworkedMultiplayerENet();
        peer.SetBindIp(bindIP);
        peer.ServerRelay = false;

        var error = peer.CreateServer(port, maxClients);
        if (error != Error.Ok) throw new Exception($"Error when starting the server: {error}");

        Multiplayer.NetworkPeer = peer;
    }

    public void Stop()
    {
        if (!IsRunning) throw new InvalidOperationException("Server is not running");

        Peer.CloseConnection();
        Multiplayer.NetworkPeer = null;

        IsSingleplayer = false;
        _isLocalPlayerConnected = false;

        foreach (var player in this.GetWorld().Players)
            if (player != LocalPlayer)
                player.RemoveFromParent();
    }


    private void OnPeerConnected(int networkID)
    {
        if (IsSingleplayer) {
            if (Peer.GetPeerAddress(networkID) != "127.0.0.1")
                { Peer.DisconnectPeer(networkID, true); return; }
            Multiplayer.RefuseNewNetworkConnections = true;
        }

        if ((LocalPlayer != null) && !_isLocalPlayerConnected &&
            (Peer.GetPeerAddress(networkID) == "127.0.0.1")) {
            LocalPlayer.NetworkID   = networkID;
            _isLocalPlayerConnected = true;
        } else {
            var world = this.GetWorld();

            foreach (var player in world.Players) {
                world.RpcId(networkID, nameof(World.SpawnPlayer), player.NetworkID, player.Position);

                // Send player's appearance.
                player.Rset(nameof(Player.DisplayName), player.DisplayName);
                player.Rset(nameof(Player.Color), player.Color);

                // Send player's currently equipped item.
                if (player.Items.Current != null) ((Node2D)player.Items).RpcId(
                    networkID, nameof(Items.DoSetCurrent), player.Items.Current.Name);
            }

            foreach (var block in world.Blocks)
                world.RpcId(networkID, nameof(World.SpawnBlock),
                    block.Position.X, block.Position.Y,
                    block.Color, block.Unbreakable);

            world.Rpc(nameof(World.SpawnPlayer), networkID, Vector2.Zero);
            if (IsSingleplayer) LocalPlayer = world.GetPlayer(networkID);
        }
    }

    private void OnPeerDisconnected(int networkID)
    {
        var world  = this.GetWorld();
        var player = world.GetPlayer(networkID);

        // Local player stays around for reconnecting.
        if (LocalPlayer == player) return;

        world.Rpc(nameof(World.Despawn), world.GetPathTo(player));
    }
}
