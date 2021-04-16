using System;
using System.Collections.Generic;
using System.Linq;
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
    public static NetworkAPI API { get; private set; }
    public static NetworkStatus Status { get; private set; } = NetworkStatus.NoConnection;

    public static bool IsMultiplayerReady => (Status == NetworkStatus.ServerRunning) || (Status == NetworkStatus.ConnectedToServer);
    public static bool IsAuthoratative => Status <= NetworkStatus.ServerRunning;
    public static bool IsServer => Status == NetworkStatus.ServerRunning;
    public static bool IsClient => Status > NetworkStatus.Connecting;
    public static int LocalNetworkID => Instance.GetTree().GetNetworkUniqueId();
    public static IEnumerable<Player> Players => Instance._playersById.Values;

    public static event Action<NetworkStatus> StatusChanged;

    public static Player GetPlayer(int id)
        => Instance._playersById.TryGetValue(id, out var value) ? value : null;
    public static Player GetPlayerOrThrow(int id)
        => GetPlayer(id) ?? throw new ArgumentException(
            $"No player instance found for ID {id}", nameof(id));


    private readonly Dictionary<int, Player> _playersById = new Dictionary<int, Player>();

    [Export] public NodePath PlayerContainerPath { get; set; }
    [Export] public PackedScene OtherPlayerScene { get; set; }

    public Node PlayerContainer { get; private set; }


    public Network() =>Instance = this;

    public override void _Ready()
    {
        PlayerContainer = GetNode(PlayerContainerPath);

        GetTree().Connect("connected_to_server", this, nameof(OnClientConnected));
        GetTree().Connect("connection_failed", this, nameof(DisconnectFromServer));
        GetTree().Connect("server_disconnected", this, nameof(DisconnectFromServer));

        GetTree().Connect("network_peer_connected", this, nameof(OnPeerConnected));
        GetTree().Connect("network_peer_disconnected", this, nameof(OnPeerDisconnected));

        var multiplayerApi = GetTree().Multiplayer;
        API = new NetworkAPI(multiplayerApi);
        multiplayerApi.Connect("network_peer_packet", this, nameof(OnPacketReceived));

        API.RegisterC2SPacket<ClientAuthPacket>(OnClientAuthPacket);
        API.RegisterS2CPacket<SpawnPlayerPacket>(OnSpawnPlayerPacket);
        API.RegisterS2CPacket<SpawnBlockPacket>(OnSpawnBlockPacket);
        API.RegisterS2CPacket<SpawnBlocksPacket>(OnSpawnBlocksPacket);
        Player.RegisterPackets();
    }

    // Let NetworkAPI handle receiving of custom packages.
    private void OnPacketReceived(int id, byte[] bytes)
        => API.OnPacketReceived(id, bytes);


    public void ResetGame()
    {
        LocalPlayer.Instance.NetworkID = -1;

        // Clear other players.
        foreach (var player in _playersById.Values)
            if (!player.IsLocal) player.QueueFree();
        _playersById.Clear();

        // Game.Instance.ClearBlocks();
        // Game.Instance.SpawnDefaultBlocks();
    }

    private void ChangeStatus(NetworkStatus status)
    {
        if (Status == status) return;
        Status = status;
        StatusChanged?.Invoke(status);

        PlayerContainer.PauseMode = IsMultiplayerReady
            ? PauseModeEnum.Process : PauseModeEnum.Stop;
    }


    public Error StartServer(ushort port)
    {
        if (GetTree().NetworkPeer != null) throw new InvalidOperationException();

        var peer = new NetworkedMultiplayerENet();
        var error = peer.CreateServer(port);
        if (error != Error.Ok) return error;
        GetTree().NetworkPeer = peer;

        LocalPlayer.Instance.NetworkID = 1;
        _playersById.Add(1, LocalPlayer.Instance);

        ChangeStatus(NetworkStatus.ServerRunning);
        return Error.Ok;
    }

    public void StopServer()
    {
        if ((GetTree().NetworkPeer == null) || !GetTree().IsNetworkServer()) throw new InvalidOperationException();

        ((NetworkedMultiplayerENet)GetTree().NetworkPeer).CloseConnection();
        GetTree().NetworkPeer = null;

        ResetGame();
        ChangeStatus(NetworkStatus.NoConnection);
    }


    public Error ConnectToServer(string address, ushort port)
    {
        if (GetTree().NetworkPeer != null) throw new InvalidOperationException();

        var peer = new NetworkedMultiplayerENet();
        var error = peer.CreateClient(address, port);
        if (error != Error.Ok) return error;
        GetTree().NetworkPeer = peer;

        ChangeStatus(NetworkStatus.Connecting);
        return Error.Ok;
    }

    private void OnClientConnected()
    {
        ChangeStatus(NetworkStatus.Authenticating);

        var id = GetTree().GetNetworkUniqueId();
        LocalPlayer.Instance.NetworkID = id;
        _playersById.Add(id, LocalPlayer.Instance);

        API.SendToServer(new ClientAuthPacket(LocalPlayer.Instance));
    }

    public void DisconnectFromServer()
    {
        if ((GetTree().NetworkPeer == null) || GetTree().IsNetworkServer()) throw new InvalidOperationException();

        ((NetworkedMultiplayerENet)GetTree().NetworkPeer).CloseConnection();
        GetTree().NetworkPeer = null;

        ChangeStatus(NetworkStatus.NoConnection);
        ResetGame();
    }


    private Player SpawnOtherPlayer(int networkID, Vector2 position, string displayName, Color color)
    {
        var player = OtherPlayerScene.Init<Player>();
        player.NetworkID   = networkID;
        // TODO: We need to find a way to sync these property automatically.
        player.Position    = position;
        player.DisplayName = displayName;
        player.Color       = color;
        _playersById.Add(networkID, player);
        PlayerContainer.AddChild(player);
        return player;
    }


    private class ClientAuthPacket
    {
        public string DisplayName { get; }
        public Color Color { get; }
        public ClientAuthPacket(Player player)
            { DisplayName = player.DisplayName; Color = player.Color; }
    }
    private void OnClientAuthPacket(int networkID, ClientAuthPacket packet)
    {
        // Authentication message is only sent once, so once the Player object exists, ignore this message.
        if (GetPlayer(networkID) != null) return;

        API.SendTo(networkID, new SpawnBlocksPacket());

        foreach (var player in _playersById.Values)
            API.SendTo(networkID, new SpawnPlayerPacket(player));

        var newPlayer = SpawnOtherPlayer(networkID, Vector2.Zero, packet.DisplayName, packet.Color);
        API.SendToEveryone(new SpawnPlayerPacket(newPlayer));
    }

    private class SpawnPlayerPacket
    {
        public int NetworkID { get; }
        public Vector2 Position { get; }
        public string DisplayName { get; }
        public Color Color { get; }

        public SpawnPlayerPacket(Player player)
        {
            NetworkID   = player.NetworkID;
            Position    = player.Position;
            DisplayName = player.DisplayName;
            Color       = player.Color;
        }
    }
    private void OnSpawnPlayerPacket(SpawnPlayerPacket packet)
    {
        if (packet.NetworkID == LocalNetworkID) {
            var player = LocalPlayer.Instance;
            player.Position = packet.Position;
            player.Velocity = Vector2.Zero;
            ChangeStatus(NetworkStatus.ConnectedToServer);
        } else SpawnOtherPlayer(packet.NetworkID, packet.Position, packet.DisplayName, packet.Color);
    }

    private struct SpawnBlockPacket
    {
        public Vector2 Position { get; }
        public Color Color { get; }
        public SpawnBlockPacket(Node2D block)
            { Position = block.Position; Color = block.Modulate; }
    }
    private void OnSpawnBlockPacket(SpawnBlockPacket packet)
    {
        var block = Game.Instance.BlockScene.Init<Node2D>();
        block.Position = packet.Position;
        block.Modulate = packet.Color;
        Game.Instance.BlockContainer.AddChild(block);
    }

    private class SpawnBlocksPacket
    {
        public List<SpawnBlockPacket> Blocks { get; }
        public SpawnBlocksPacket()
            => Blocks = Game.Instance.BlockContainer.GetChildren().OfType<Node2D>()
                .Select(block => new SpawnBlockPacket(block)).ToList();
    }
    private void OnSpawnBlocksPacket(SpawnBlocksPacket packet)
    {
        Game.Instance.ClearBlocks();
        foreach (var block in packet.Blocks)
            OnSpawnBlockPacket(block);
    }


    private void OnPeerConnected(int id)
    {
        // Currently unused.
    }

    private void OnPeerDisconnected(int id)
    {
        GetPlayer(id)?.QueueFree();
        _playersById.Remove(id);
    }
}
