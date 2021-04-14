using Godot;

public class EscapeMenuMultiplayer : Container
{
    [Export] public NodePath StatusPath { get; set; }
    [Export] public NodePath ServerStartStopPath { get; set; }
    [Export] public NodePath ServerPortPath { get; set; }
    [Export] public NodePath ClientDisConnectPath { get; set; }
    [Export] public NodePath ClientAddressPath { get; set; }

    public Label Status { get; private set; }
    public Button ServerStartStop { get; private set; }
    public LineEdit ServerPort { get; private set; }
    public Button ClientDisConnect { get; private set; }
    public LineEdit ClientAddress { get; private set; }

    public Network Network { get; private set; }

    public override void _EnterTree()
    {
        Status           = GetNode<Label>(StatusPath);
        ServerStartStop  = GetNode<Button>(ServerStartStopPath);
        ServerPort       = GetNode<LineEdit>(ServerPortPath);
        ClientDisConnect = GetNode<Button>(ClientDisConnectPath);
        ClientAddress    = GetNode<LineEdit>(ClientAddressPath);

        Network = GetNode<Network>("/root/Game/Network");
        Network.Connect(nameof(Network.StatusChanged), this, nameof(OnNetworkStatusChanged));
        ServerPort.PlaceholderText    = Network.DefaultPort.ToString();
        ClientAddress.PlaceholderText = $"{Network.DefaultAddress}:{Network.DefaultPort}";
    }


    private void OnNetworkStatusChanged(Network.Status status)
    {
        switch (status) {
            case Network.Status.NoConnection:
                Status.Text     = "No Connection";
                Status.Modulate = Colors.Red;
                break;
            case Network.Status.ServerRunning:
                Status.Text     = "Server Running";
                Status.Modulate = Colors.Green;
                break;
            case Network.Status.Connecting:
                Status.Text     = "Connecting ...";
                Status.Modulate = Colors.Yellow;
                break;
            case Network.Status.ConnectedToServer:
                Status.Text     = "Connected to Server";
                Status.Modulate = Colors.Green;
                break;
        }

        ServerPort.Editable = status == Network.Status.NoConnection;
        ServerStartStop.Text = (status == Network.Status.ServerRunning) ? "Stop Server" : "Start Server";
        ClientAddress.Editable = status == Network.Status.NoConnection;
        ClientDisConnect.Text     = (status < Network.Status.Connecting) ? "Connect" : "Disconnect";
        ClientDisConnect.Disabled = status == Network.Status.ServerRunning;
        if (Visible) GetTree().Paused = status == Network.Status.NoConnection;
    }


    #pragma warning disable IDE0051
    #pragma warning disable IDE1006

    private void _on_ServerStartStop_pressed()
    {
        if (GetTree().NetworkPeer == null) {
            var port = Network.DefaultPort;
            if (ServerPort.Text.Length > 0)
                port = ushort.Parse(ServerPort.Text);
            Network.StartServer(port);
        } else Network.StopServer();
    }

    private void _on_ClientDisConnect_pressed()
    {
        if (GetTree().NetworkPeer == null) {
            var address = Network.DefaultAddress;
            var port    = Network.DefaultPort;
            if (ClientAddress.Text.Length > 0) {
                // TODO: Verify input some more, support IPv6?
                var split = address.Split(':');
                address = (split.Length > 1) ? split[0] : address;
                port    = (split.Length > 1) ? ushort.Parse(split[1]) : port;
            }
            Network.ConnectToServer(address, port);
        } else Network.DisconnectFromServer();
    }

    private void _on_HideAddress_toggled(bool pressed)
        => ClientAddress.Secret = pressed;
}
