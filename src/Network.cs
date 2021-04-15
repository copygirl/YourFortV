using System;
using System.Collections.Generic;
using Godot;

public enum NetworkStatus
{
    NoConnection,
    ServerRunning,
    Connecting,
    Authenticating,
    ConnectedToServer,
}

public class Network : Node
{
    public const ushort DEFAULT_PORT = 42005;

    public static Network Instance { get; private set; }
    public static NetworkStatus Status { get; private set; } = NetworkStatus.NoConnection;

    public static bool IsServer => Instance.GetTree().IsNetworkServer();
    public static int LocalNetworkId => Instance.GetTree().GetNetworkUniqueId();


    private readonly Dictionary<int, Player> _playersById = new Dictionary<int, Player>();

    [Export] public NodePath PlayerContainerPath { get; set; }
    [Export] public PackedScene OtherPlayerScene { get; set; }

    public Node PlayerContainer { get; private set; }

    [Signal] public delegate void StatusChanged(NetworkStatus status);


    public Network() => Instance = this;

    public override void _Ready()
    {
        PlayerContainer = GetNode(PlayerContainerPath);

        GetTree().Connect("connected_to_server", this, nameof(OnClientConnected));
        GetTree().Connect("connection_failed", this, nameof(DisconnectFromServer));
        GetTree().Connect("server_disconnected", this, nameof(DisconnectFromServer));

        GetTree().Connect("network_peer_connected", this, nameof(OnPeerConnected));
        GetTree().Connect("network_peer_disconnected", this, nameof(OnPeerDisconnected));
    }


    public Player GetPlayer(int id)
        => _playersById.TryGetValue(id, out var value) ? value : null;

    public void ClearPlayers()
    {
        LocalPlayer.Instance.NetworkId = -1;
        foreach (var player in _playersById.Values)
            if (!player.IsLocal)
                player.RemoveFromParent();
        _playersById.Clear();
    }


    public Error StartServer(ushort port)
    {
        if (GetTree().NetworkPeer != null) throw new InvalidOperationException();

        var peer = new NetworkedMultiplayerENet();
        var error = peer.CreateServer(port);
        if (error != Error.Ok) return error;
        GetTree().NetworkPeer = peer;

        Status = NetworkStatus.ServerRunning;
        EmitSignal(nameof(StatusChanged), Status);

        LocalPlayer.Instance.NetworkId = 1;
        _playersById.Add(1, LocalPlayer.Instance);

        return Error.Ok;
    }

    public void StopServer()
    {
        if ((GetTree().NetworkPeer == null) || !GetTree().IsNetworkServer()) throw new InvalidOperationException();

        ((NetworkedMultiplayerENet)GetTree().NetworkPeer).CloseConnection();
        GetTree().NetworkPeer = null;

        Status = NetworkStatus.NoConnection;
        EmitSignal(nameof(StatusChanged), Status);

        ClearPlayers();
    }

    public Error ConnectToServer(string address, ushort port)
    {
        if (GetTree().NetworkPeer != null) throw new InvalidOperationException();

        var peer = new NetworkedMultiplayerENet();
        var error = peer.CreateClient(address, port);
        if (error != Error.Ok) return error;
        GetTree().NetworkPeer = peer;

        Status = NetworkStatus.Connecting;
        EmitSignal(nameof(StatusChanged), Status);

        return Error.Ok;
    }

    public void DisconnectFromServer()
    {
        if ((GetTree().NetworkPeer == null) || GetTree().IsNetworkServer()) throw new InvalidOperationException();

        ((NetworkedMultiplayerENet)GetTree().NetworkPeer).CloseConnection();
        GetTree().NetworkPeer = null;

        Status = NetworkStatus.NoConnection;
        EmitSignal(nameof(StatusChanged), Status);

        ClearPlayers();
    }


    private void OnClientConnected()
    {
        Status = NetworkStatus.Authenticating;
        EmitSignal(nameof(StatusChanged), Status);

        var id = GetTree().GetNetworkUniqueId();
        LocalPlayer.Instance.NetworkId = id;
        _playersById.Add(id, LocalPlayer.Instance);

        Rpc(nameof(OnClientAuthenticate), LocalPlayer.Instance.DisplayName, LocalPlayer.Instance.Color);
    }

    [Master]
    private void OnClientAuthenticate(string displayName, Color color)
    {
        var id = GetTree().GetRpcSenderId();

        // Authentication message is only sent once, so once the Player object exists, ignore this message.
        if (GetPlayer(id) != null) return;

        var newPlayer = SpawnOtherPlayerInternal(id, Vector2.Zero, displayName, color);
        RpcId(id, nameof(SpawnLocalPlayer), newPlayer.Position);

        foreach (var player in _playersById.Values) {
            if (player == newPlayer) continue;

            // Spawn existing players for the new player.
            RpcId(id, nameof(SpawnOtherPlayer), player.NetworkId, player.Position, player.DisplayName, player.Color);

            // Spawn new player for existing players.
            if (!player.IsLocal) // Don't spawn the player for the host, it already called SpawnOtherPlayer itself.
                RpcId(player.NetworkId, nameof(SpawnOtherPlayer), newPlayer.NetworkId, newPlayer.Position, newPlayer.DisplayName, newPlayer.Color);
        }
    }

    [Puppet]
    private Player SpawnLocalPlayer(Vector2 position)
    {
        Status = NetworkStatus.ConnectedToServer;
        EmitSignal(nameof(StatusChanged), Status);

        LocalPlayer.Instance.Position = position;
        return LocalPlayer.Instance;
    }

    private Player SpawnOtherPlayerInternal(int id, Vector2 position, string displayName, Color color)
    {
        var player = OtherPlayerScene.Init<Player>();
        player.NetworkId = id;
        // TODO: We need to find a way to sync these property automatically.
        player.Position    = position;
        player.DisplayName = displayName;
        player.Color       = color;
        _playersById.Add(id, player);
        PlayerContainer.AddChild(player);
        return player;
    }
    [Puppet]
    private void SpawnOtherPlayer(int id, Vector2 position, string displayName, Color color)
        => SpawnOtherPlayerInternal(id, position, displayName, color);


    private void OnPeerConnected(int id)
    {
        // Currently unused.
    }

    private void OnPeerDisconnected(int id)
    {
        GetPlayer(id)?.RemoveFromParent();
        _playersById.Remove(id);
    }
}
