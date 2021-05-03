using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

// TODO: Allow for initially private integrated server to open itself up to the public.
public class Server : Game
{
    public new SyncServer Sync => (SyncServer)base.Sync;

    private readonly Dictionary<NetworkID, Player> _playersByNetworkID = new Dictionary<NetworkID, Player>();
    private readonly Dictionary<Player, NetworkID> _networkIDByPlayer = new Dictionary<Player, NetworkID>();

    private Player _localPlayer = null;
    private bool _isLocalPlayerConnected = false;

    public NetworkedMultiplayerENet Peer => (NetworkedMultiplayerENet)CustomMultiplayer.NetworkPeer;
    public bool IsRunning => Peer != null;
    public bool IsSingleplayer { get; private set; }

    public Server()
    {
        base.Sync = new SyncServer(this);
        CustomMultiplayer = new MultiplayerAPI { RootNode = this };
        CustomMultiplayer.Connect("network_peer_connected", this, nameof(OnPeerConnected));
        CustomMultiplayer.Connect("network_peer_disconnected", this, nameof(OnPeerDisconnected));
        CustomMultiplayer.Connect("network_peer_packet", this, nameof(OnPacketReceived));
    }

    public override void _Process(float delta)
    {
        CustomMultiplayer.Poll();
        Sync.ProcessDirty(this);
        NetworkRPC.ProcessPacketBuffer(this);
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
                // 3 retries should be well enough to find a random unused port.
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

        CustomMultiplayer.NetworkPeer = peer;
    }

    public void Stop()
    {
        if (!IsRunning) throw new InvalidOperationException("Server is not running");

        Peer.CloseConnection();
        CustomMultiplayer.NetworkPeer = null;

        IsSingleplayer = false;
        _isLocalPlayerConnected = false;
    }


    public IEnumerable<(NetworkID, Player)> Players
        => _playersByNetworkID.Select(entry => (entry.Key, entry.Value));
    public Player GetPlayer(NetworkID networkID)
        => _playersByNetworkID[networkID];
    public NetworkID GetNetworkID(Player player)
        => _networkIDByPlayer[player];


    private void OnPeerConnected(int id)
    {
        var networkID = new NetworkID(id);

        if (IsSingleplayer) {
            if (Peer.GetPeerAddress(id) != "127.0.0.1")
                { Peer.DisconnectPeer(id, true); return; }
            CustomMultiplayer.RefuseNewNetworkConnections = true;
        }

        Player player;
        if ((_localPlayer != null) && !_isLocalPlayerConnected &&
            (Peer.GetPeerAddress(id) == "127.0.0.1")) {
            player = _localPlayer;
            _isLocalPlayerConnected = true;

            var oldNetworkID = GetNetworkID(player);
            _playersByNetworkID.Remove(oldNetworkID);
            _playersByNetworkID.Add(networkID, player);
            _networkIDByPlayer[player] = networkID;
        } else {
            Sync.SendAllObjects(this, networkID);
            player = this.Spawn<Player>();
            player.Position = Vector2.Zero;
            player.Color    = Colors.Red;

            _playersByNetworkID.Add(networkID, player);
            _networkIDByPlayer.Add(player, networkID);
        }

        if (IsSingleplayer) _localPlayer = player;
        player.RPC(new []{ networkID }, player.SetLocal);
    }

    private void OnPeerDisconnected(int id)
    {
        var networkID = new NetworkID(id);
        var player    = GetPlayer(networkID);

        // Local player stays around for reconnecting.
        if (_localPlayer == player) return;

        player.RemoveFromParent();
        _playersByNetworkID.Remove(networkID);
        _networkIDByPlayer.Remove(player);
    }

    private void OnPacketReceived(int networkID, byte[] bytes)
        => NetworkPackets.Process(this, new NetworkID(networkID), bytes);


    public void RPC(IEnumerable<NetworkID> targets, Action<Client> action) => NetworkRPC.Call(this, targets, action.Method, false);
    public void RPC<T>(IEnumerable<NetworkID> targets, Action<Client, T> action, T arg) => NetworkRPC.Call(this, targets, action.Method, false, arg);
    public void RPC<T0, T1>(IEnumerable<NetworkID> targets, Action<Client, T0, T1> action, T0 arg0, T1 arg1) => NetworkRPC.Call(this, targets, action.Method, false, arg0, arg1);
    public void RPC<T0, T1, T2>(IEnumerable<NetworkID> targets, Action<Client, T0, T1, T2> action, T0 arg0, T1 arg1, T2 arg2) => NetworkRPC.Call(this, targets, action.Method, false, arg0, arg1, arg2);
    public void RPC<T0, T1, T2, T3>(IEnumerable<NetworkID> targets, Action<Client, T0, T1, T2, T3> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3) => NetworkRPC.Call(this, targets, action.Method, false, arg0, arg1, arg2, arg3);
    public void RPC<T0, T1, T2, T3, T4>(IEnumerable<NetworkID> targets, Action<Client, T0, T1, T2, T3, T4> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => NetworkRPC.Call(this, targets, action.Method, false, arg0, arg1, arg2, arg3, arg4);
    public void RPC<T0, T1, T2, T3, T4, T5>(IEnumerable<NetworkID> targets, Action<Client, T0, T1, T2, T3, T4, T5> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => NetworkRPC.Call(this, targets, action.Method, false, arg0, arg1, arg2, arg3, arg4, arg5);
    public void RPC<T0, T1, T2, T3, T4, T5, T6>(IEnumerable<NetworkID> targets, Action<Client, T0, T1, T2, T3, T4, T5, T6> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => NetworkRPC.Call(this, targets, action.Method, false, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
    public void RPC<T0, T1, T2, T3, T4, T5, T6, T7>(IEnumerable<NetworkID> targets, Action<Client, T0, T1, T2, T3, T4, T5, T6, T7> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) => NetworkRPC.Call(this, targets, action.Method, false, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
}

public readonly struct NetworkID : IEquatable<NetworkID>
{
    public static readonly NetworkID Server = new NetworkID(1);
    public int Value { get; }
    public NetworkID(int value) => Value = value;
    public override bool Equals(object obj) => (obj is NetworkID other) && Equals(other);
    public bool Equals(NetworkID other) => Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => $"{nameof(NetworkID)}({Value})";
    public static bool operator ==(NetworkID left, NetworkID right) => left.Equals(right);
    public static bool operator !=(NetworkID left, NetworkID right) => !left.Equals(right);
}
