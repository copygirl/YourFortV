using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

// TODO: Split network and escape menu logic.
public class EscapeMenu : Container
{
    [Export] public ushort DefaultPort { get; set; } = 42005;
    [Export] public string DefaultAddress { get; set; } = "localhost";

    [Export] public NodePath StatusPath { get; set; }
    [Export] public NodePath ServerStartStopPath { get; set; }
    [Export] public NodePath ServerPortPath { get; set; }
    [Export] public NodePath ClientDisConnectPath { get; set; }
    [Export] public NodePath ClientAddressPath { get; set; }
    [Export] public NodePath ReturnPath { get; set; }

    public Label Status { get; private set; }
    public Button ServerStartStop { get; private set; }
    public LineEdit ServerPort { get; private set; }
    public Button ClientDisConnect { get; private set; }
    public LineEdit ClientAddress { get; private set; }
    public Button Return { get; private set; }

    public Node PlayerContainer { get; private set; }
    public Player OwnPlayer { get; private set; }
    public PackedScene OtherPlayer { get; private set; }

    public override void _Ready()
    {
        OtherPlayer = GD.Load<PackedScene>("res://scene/OtherPlayer.tscn");
    }

    public override void _EnterTree()
    {
        Status           = GetNode<Label>(StatusPath);
        ServerStartStop  = GetNode<Button>(ServerStartStopPath);
        ServerPort       = GetNode<LineEdit>(ServerPortPath);
        ClientDisConnect = GetNode<Button>(ClientDisConnectPath);
        ClientAddress    = GetNode<LineEdit>(ClientAddressPath);
        Return           = GetNode<Button>(ReturnPath);
        PlayerContainer  = GetNode("/root/Game");

        ServerPort.PlaceholderText    = DefaultPort.ToString();
        ClientAddress.PlaceholderText = $"{DefaultAddress}:{DefaultPort}";

        GetTree().Connect("connected_to_server", this, "OnClientConnected");
        GetTree().Connect("connection_failed", this, "DisconnectFromServer");
        GetTree().Connect("server_disconnected", this, "DisconnectFromServer");

        GetTree().Connect("network_peer_connected", this, "OnPeerConnected");
        GetTree().Connect("network_peer_disconnected", this, "OnPeerDisconnected");
    }

    public override void _Process(float delta)
    {
        if (OwnPlayer == null) return;
        RpcUnreliable("OnPlayerMoved", OwnPlayer.Position);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_menu")) Toggle();
    }

    public void Toggle()
    {
        if (Visible) Close();
        else Open();
    }

    public void Open()
    {
        if (Visible) return;
        if (GetTree().NetworkPeer == null)
            GetTree().Paused = true;
        Return.GrabFocus();
        Visible = true;
    }

    public void Close()
    {
        if (!Visible) return;
        if (GetTree().NetworkPeer == null)
            GetTree().Paused = false;
        Visible = false;
    }


    public void StartServer(ushort port)
    {
        if (GetTree().NetworkPeer != null) throw new InvalidOperationException();

        var peer = new NetworkedMultiplayerENet();
        // TODO: Somehow show there was an error.
        if (peer.CreateServer(port) != Error.Ok) return;
        GetTree().NetworkPeer = peer;
        OwnPlayer = FindOwnPlayer();

        Status.Text     = "Server Running";
        Status.Modulate = Colors.Green;
        ServerPort.Editable  = false;
        ServerStartStop.Text = "Stop Server";
        ClientAddress.Editable    = false;
        ClientDisConnect.Disabled = true;

        if (Visible) GetTree().Paused = false;
    }

    public void StopServer()
    {
        if ((GetTree().NetworkPeer == null) || !GetTree().IsNetworkServer()) throw new InvalidOperationException();

        // TODO: Disconnect players gracefully.
        ((NetworkedMultiplayerENet)GetTree().NetworkPeer).CloseConnection();
        GetTree().NetworkPeer = null;

        OwnPlayer = null;
        foreach (var player in GetOtherPlayers())
            player.RemoveFromParent();

        Status.Text     = "No Connection";
        Status.Modulate = Colors.Red;
        ServerPort.Editable  = true;
        ServerStartStop.Text = "Start Server";
        ClientAddress.Editable    = true;
        ClientDisConnect.Disabled = false;

        if (Visible) GetTree().Paused = true;
    }

    public void ConnectToServer(string address, ushort port)
    {
        if (GetTree().NetworkPeer != null) throw new InvalidOperationException();

        var peer = new NetworkedMultiplayerENet();
        // TODO: Somehow show there was an error.
        if (peer.CreateClient(address, port) != Error.Ok) return;
        GetTree().NetworkPeer = peer;

        Status.Text     = "Connecting ...";
        Status.Modulate = Colors.Yellow;
        ServerPort.Editable      = false;
        ServerStartStop.Disabled = true;
        ClientAddress.Editable = false;
        ClientDisConnect.Text  = "Disconnect";

        if (Visible) GetTree().Paused = false;
    }

    public void DisconnectFromServer()
    {
        if ((GetTree().NetworkPeer == null) || GetTree().IsNetworkServer()) throw new InvalidOperationException();

        // TODO: Disconnect from server gracefully.
        ((NetworkedMultiplayerENet)GetTree().NetworkPeer).CloseConnection();
        GetTree().NetworkPeer = null;

        OwnPlayer = null;
        foreach (var player in GetOtherPlayers())
            player.RemoveFromParent();

        Status.Text     = "No Connection";
        Status.Modulate = Colors.Red;
        ServerPort.Editable      = true;
        ServerStartStop.Disabled = false;
        ClientAddress.Editable    = true;
        ClientDisConnect.Disabled = false;
        ClientDisConnect.Text     = "Connect";

        if (Visible) GetTree().Paused = true;
    }

    private Player FindOwnPlayer()
        => GetTree().Root.GetChild(0).GetChildren().OfType<Player>().First();

    private Node2D GetPlayerWithId(int id)
        => PlayerContainer.GetNodeOrNull<Node2D>(id.ToString());
    private Node2D GetOrCreatePlayerWithId(int id)
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
    private IEnumerable<Node2D> GetOtherPlayers()
        => PlayerContainer.GetChildren().OfType<Node2D>()
            .Where(node => char.IsDigit(node.Name[0]));


    #pragma warning disable IDE0051

    private void OnClientConnected()
    {
        OwnPlayer = FindOwnPlayer();

        Status.Text     = "Connected to Server";
        Status.Modulate = Colors.Green;
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


    #pragma warning disable IDE1006

    private void _on_ServerStartStop_pressed()
    {
        if (GetTree().NetworkPeer == null)
            StartServer((ServerPort.Text.Length > 0) ? ushort.Parse(ServerPort.Text) : DefaultPort);
        else StopServer();
    }

    private void _on_ClientDisConnect_pressed()
    {
        if (GetTree().NetworkPeer == null) {
            var address = DefaultAddress;
            var port    = DefaultPort;

            if (ClientAddress.Text.Length > 0) {
                // TODO: Verify input some more, support IPv6?
                var split = address.Split(':');
                address = (split.Length > 1) ? split[0] : address;
                port    = (split.Length > 1) ? ushort.Parse(split[1]) : port;
            }

            ConnectToServer(address, port);
        } else DisconnectFromServer();
    }

    private void _on_HideAddress_toggled(bool pressed)
        => ClientAddress.Secret = pressed;
    private void _on_Quit_pressed()
        => GetTree().Quit();
    private void _on_Return_pressed()
        => Close();
}
