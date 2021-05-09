using System;
using Godot;
using static Godot.NetworkedMultiplayerPeer;

public class Client : Game
{
    [Export] public NodePath CursorPath { get; set; }
    public Cursor Cursor { get; private set; }

    public NetworkedMultiplayerENet Peer => (NetworkedMultiplayerENet)Multiplayer.NetworkPeer;
    public ConnectionStatus Status => Peer?.GetConnectionStatus() ?? ConnectionStatus.Disconnected;
    public LocalPlayer LocalPlayer => (LocalPlayer)this.GetWorld().GetPlayer(GetTree().GetNetworkUniqueId());

    public event Action Connected;
    public event Action Disconnected;
    public event Action<ConnectionStatus> StatusChanged;


    public override void _Ready()
    {
        base._Ready();
        Cursor = GetNode<Cursor>(CursorPath);

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

        Disconnected?.Invoke();
        StatusChanged?.Invoke(Status);
    }

    private void OnConnectedToServer()
    {
        Connected?.Invoke();
        StatusChanged?.Invoke(Status);
    }
}
