using System;
using Godot;
using static Godot.NetworkedMultiplayerPeer;

public class Client : Game
{
    [Export] public NodePath IntegratedServerPath { get; set; }
    [Export] public NodePath CursorPath { get; set; }

    public IntegratedServer IntegratedServer { get; private set; }
    public Cursor Cursor { get; private set; }

    public NetworkedMultiplayerENet Peer => (NetworkedMultiplayerENet)Multiplayer.NetworkPeer;
    public ConnectionStatus Status => Peer?.GetConnectionStatus() ?? ConnectionStatus.Disconnected;
    public Player LocalPlayer => this.GetWorld().GetPlayer(GetTree().GetNetworkUniqueId());
    private Player _storedLocalPlayer;

    public event Action Connected;
    public event Action Disconnected;
    public event Action<Player> LocalPlayerSpawned;
    public event Action<ConnectionStatus> StatusChanged;

    internal void FireLocalPlayerSpawned(Player player)
        => LocalPlayerSpawned?.Invoke(player);


    public override void _Ready()
    {
        base._Ready();
        Cursor           = GetNode<Cursor>(CursorPath);
        IntegratedServer = GetNode<IntegratedServer>(IntegratedServerPath);

        Multiplayer.Connect("connected_to_server", this, nameof(OnConnectedToServer));
        Multiplayer.Connect("connection_failed", this, nameof(Disconnect));
        Multiplayer.Connect("server_disconnected", this, nameof(Disconnect));
    }


    public void Connect(string address, ushort port)
    {
        if (Peer != null) throw new InvalidOperationException("Client connection is already open");

        var peer  = new NetworkedMultiplayerENet { ServerRelay = false };
        var error = peer.CreateClient(address, port);
        if (error != Error.Ok) throw new Exception($"Error when connecting: {error}");
        Multiplayer.NetworkPeer = peer;

        StatusChanged?.Invoke(Status);
    }

    public void Disconnect()
    {
        if (Peer == null) return;
        Peer.CloseConnection();
        Multiplayer.NetworkPeer = null;

        if (IntegratedServer.Server.IsRunning) {
            foreach (var player in this.GetWorld().Players) {
                // Store the local player for later restoration, but remove it from the scene.
                if (player.IsLocal) {
                    _storedLocalPlayer = player;
                    player.GetParent().RemoveChild(player);
                    // Do NOT call QueueFree - like RemoveFromParent does.
                } else player.RemoveFromParent();
            }
        } else {
            this.GetWorld().ClearPlayers();
            this.GetWorld().ClearChunks();
        }

        Disconnected?.Invoke();
        StatusChanged?.Invoke(Status);
    }

    private void OnConnectedToServer()
    {
        if ((IntegratedServer.Server.IsRunning == true) && (_storedLocalPlayer != null)) {
            this.GetWorld().PlayerContainer.AddChild(_storedLocalPlayer);
            _storedLocalPlayer.SetNetworkID(true, GetTree().GetNetworkUniqueId());
            _storedLocalPlayer = null;
        }

        Connected?.Invoke();
        StatusChanged?.Invoke(Status);
    }
}
