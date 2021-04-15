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

    public override void _Ready()
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


    private void OnNetworkStatusChanged(NetworkStatus status)
    {
        switch (status) {
            case NetworkStatus.NoConnection:
                Status.Text     = "No Connection";
                Status.Modulate = Colors.Red;
                break;
            case NetworkStatus.ServerRunning:
                Status.Text     = "Server Running";
                Status.Modulate = Colors.Green;
                break;
            case NetworkStatus.Connecting:
                Status.Text     = "Connecting ...";
                Status.Modulate = Colors.Yellow;
                break;
            case NetworkStatus.Authenticating:
                Status.Text     = "Authenticating ...";
                Status.Modulate = Colors.YellowGreen;
                break;
            case NetworkStatus.ConnectedToServer:
                Status.Text     = "Connected to Server";
                Status.Modulate = Colors.Green;
                break;
        }

        var noConnection = status == NetworkStatus.NoConnection;
        ServerPort.Editable      = noConnection;
        ServerStartStop.Disabled = noConnection;
        ClientAddress.Editable   = noConnection;
        ServerStartStop.Text      = (status == NetworkStatus.ServerRunning) ? "Stop Server" : "Start Server";
        ClientDisConnect.Text     = (status < NetworkStatus.Connecting) ? "Connect" : "Disconnect";
        ClientDisConnect.Disabled = status == NetworkStatus.ServerRunning;
        if (Visible) GetTree().Paused = noConnection;
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
