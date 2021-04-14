using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class Network : Node
{
    public enum Status
    {
        NoConnection,
        ServerRunning,
        Connecting,
        ConnectedToServer,
    }

    [Export] public ushort DefaultPort { get; set; } = 42005;
    [Export] public string DefaultAddress { get; set; } = "localhost";

    [Export] public NodePath PlayerContainerPath { get; set; }
    [Export] public PackedScene OtherPlayer { get; set; }

    public Node PlayerContainer { get; private set; }

    public Player LocalPlayer { get; private set; }
    public Status CurrentStatus { get; private set; } = Status.NoConnection;
    [Signal] public delegate void StatusChanged(Status status);

    public override void _Ready()
    {
        PlayerContainer = GetNode(PlayerContainerPath);

        GetTree().Connect("connected_to_server", this, nameof(OnClientConnected));
        GetTree().Connect("connection_failed", this, nameof(DisconnectFromServer));
        GetTree().Connect("server_disconnected", this, nameof(DisconnectFromServer));

        GetTree().Connect("network_peer_connected", this, nameof(OnPeerConnected));
        GetTree().Connect("network_peer_disconnected", this, nameof(OnPeerDisconnected));
    }

    public override void _Process(float delta)
    {
        if (LocalPlayer == null) return;
        RpcUnreliable(nameof(OnPlayerMoved), LocalPlayer.Position);
    }


    public Error StartServer(ushort port)
    {
        if (GetTree().NetworkPeer != null) throw new InvalidOperationException();

        var peer = new NetworkedMultiplayerENet();
        // TODO: Somehow show there was an error.
        var error = peer.CreateServer(port);
        if (error != Error.Ok) return error;
        GetTree().NetworkPeer = peer;
        LocalPlayer = FindLocalPlayer();

        CurrentStatus = Status.ServerRunning;
        EmitSignal(nameof(StatusChanged), CurrentStatus);

        return Error.Ok;
    }

    public void StopServer()
    {
        if ((GetTree().NetworkPeer == null) || !GetTree().IsNetworkServer()) throw new InvalidOperationException();

        // TODO: Disconnect players gracefully.
        ((NetworkedMultiplayerENet)GetTree().NetworkPeer).CloseConnection();
        GetTree().NetworkPeer = null;

        LocalPlayer = null;
        foreach (var player in GetOtherPlayers())
            player.RemoveFromParent();

        CurrentStatus = Status.NoConnection;
        EmitSignal(nameof(StatusChanged), CurrentStatus);
    }

    public Error ConnectToServer(string address, ushort port)
    {
        if (GetTree().NetworkPeer != null) throw new InvalidOperationException();

        var peer = new NetworkedMultiplayerENet();
        // TODO: Somehow show there was an error.
        var error = peer.CreateClient(address, port);
        if (error != Error.Ok) return error;
        GetTree().NetworkPeer = peer;

        CurrentStatus = Status.Connecting;
        EmitSignal(nameof(StatusChanged), CurrentStatus);

        return Error.Ok;
    }

    public void DisconnectFromServer()
    {
        if ((GetTree().NetworkPeer == null) || GetTree().IsNetworkServer()) throw new InvalidOperationException();

        // TODO: Disconnect from server gracefully.
        ((NetworkedMultiplayerENet)GetTree().NetworkPeer).CloseConnection();
        GetTree().NetworkPeer = null;

        LocalPlayer = null;
        foreach (var player in GetOtherPlayers())
            player.RemoveFromParent();

        CurrentStatus = Status.NoConnection;
        EmitSignal(nameof(StatusChanged), CurrentStatus);
    }

    public Player FindLocalPlayer()
        => GetNode<Player>("/root/Game/LocalPlayer");

    public Node2D GetPlayerWithId(int id)
        => PlayerContainer.GetNodeOrNull<Node2D>(id.ToString());
    public Node2D GetOrCreatePlayerWithId(int id)
    {
        var player = GetPlayerWithId(id);
        if (player == null) {
            player = (Node2D)OtherPlayer.Instance();
            // TODO: Use "set_network_master".
            player.Name = id.ToString();
            PlayerContainer.AddChild(player);
        }
        return player;
    }

    // TODO: This assumes that any node whose name starts with a digit is a player.
    public IEnumerable<Node2D> GetOtherPlayers()
        => PlayerContainer.GetChildren().OfType<Node2D>()
            .Where(node => char.IsDigit(node.Name[0]));


    private void OnClientConnected()
    {
        LocalPlayer = FindLocalPlayer();

        CurrentStatus = Status.ConnectedToServer;
        EmitSignal(nameof(StatusChanged), CurrentStatus);
    }


    private void OnPeerConnected(int id)
    {

    }

    private void OnPeerDisconnected(int id)
        => GetPlayerWithId(id)?.RemoveFromParent();


    [Remote]
    private void OnPlayerMoved(Vector2 position)
    {
        var id     = GetTree().GetRpcSenderId();
        var player = GetOrCreatePlayerWithId(id);
        player.Position = position;
    }
}
