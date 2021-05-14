using System;
using System.Text.RegularExpressions;
using Godot;
using static Godot.NetworkedMultiplayerPeer;

public class EscapeMenuMultiplayer : Container
{
    private const ushort DEFAULT_PORT = 42005;

    [Export] public NodePath StatusPath { get; set; }
    [Export] public NodePath ServerOpenClosePath { get; set; }
    [Export] public NodePath ServerPortPath { get; set; }
    [Export] public NodePath ClientDisConnectPath { get; set; }
    [Export] public NodePath ClientAddressPath { get; set; }

    public Label Status { get; private set; }
    public Button ServerOpenClose { get; private set; }
    public LineEdit ServerPort { get; private set; }
    public Button ClientDisConnect { get; private set; }
    public LineEdit ClientAddress { get; private set; }

    public override void _Ready()
    {
        Status           = GetNode<Label>(StatusPath);
        ServerOpenClose  = GetNode<Button>(ServerOpenClosePath);
        ServerPort       = GetNode<LineEdit>(ServerPortPath);
        ClientDisConnect = GetNode<Button>(ClientDisConnectPath);
        ClientAddress    = GetNode<LineEdit>(ClientAddressPath);

        ServerPort.PlaceholderText    = DEFAULT_PORT.ToString();
        ClientAddress.PlaceholderText = $"localhost:{DEFAULT_PORT}";

        this.GetClient().StatusChanged += OnStatusChanged;
    }

    private void OnStatusChanged(ConnectionStatus status)
    {
        var server = this.GetClient().IntegratedServer.Server;
        switch (status) {
            case ConnectionStatus.Disconnected:
                Status.Text     = "Disconnected";
                Status.Modulate = Colors.Red;
                break;
            case ConnectionStatus.Connecting:
                Status.Text     = "Connecting ...";
                Status.Modulate = Colors.Yellow;
                break;
            case ConnectionStatus.Connected:
                if (!server.IsRunning) {
                    Status.Text     = "Connected!";
                    Status.Modulate = Colors.Green;
                } else if (server.IsSingleplayer) {
                    Status.Text     = "Singleplayer";
                    Status.Modulate = Colors.White;
                } else {
                    Status.Text     = "Server Running";
                    Status.Modulate = Colors.Green;
                }
                break;
        }

        ServerPort.Editable      =  server.IsRunning;
        ServerOpenClose.Disabled = !server.IsRunning;
        ServerOpenClose.Text     = (server.IsRunning && !server.IsSingleplayer) ? "Close Server" : "Open Server";
        ClientDisConnect.Text     = (server.IsSingleplayer || (status == ConnectionStatus.Disconnected)) ? "Connect" : "Disconnect";
        ClientDisConnect.Disabled = server.IsRunning && !server.IsSingleplayer;

        var pauseMode = server.IsSingleplayer ? PauseModeEnum.Stop : PauseModeEnum.Process;
        this.GetWorld().PauseMode   = pauseMode;
        server.GetWorld().PauseMode = pauseMode;

        // TODO: Allow starting up the integrated server again when disconnected.
    }


    #pragma warning disable IDE0051
    #pragma warning disable IDE1006

    private static readonly Regex INVALID_CHARS = new Regex(@"[^0-9]");
    private void _on_ServerPort_text_changed(string text)
    {
        var validText = INVALID_CHARS.Replace(text, "");
        validText = validText.TrimStart('0');
        if (validText != text) {
            var previousCaretPos = ServerPort.CaretPosition;
            ServerPort.Text = validText;
            ServerPort.CaretPosition = previousCaretPos - (text.Length - validText.Length);
        }
    }

    private void _on_HideAddress_toggled(bool pressed)
        => ClientAddress.Secret = pressed;


    private void _on_ServerOpenClose_pressed()
    {
        var client = this.GetClient();
        var server = client.IntegratedServer.Server;
        if (server?.IsRunning != true) throw new InvalidOperationException();

        if (server.IsSingleplayer) {
            var port = (ServerPort.Text.Length > 0) ? ushort.Parse(ServerPort.Text) : DEFAULT_PORT;
            client.Disconnect();
            server.Stop();
            server.Start(port);
            client.Connect("127.0.0.1", port);
        } else {
            client.Disconnect();
            server.Stop();
            var port = server.StartSingleplayer();
            client.Connect("127.0.0.1", port);
        }

        ServerOpenClose.Text = server.IsSingleplayer ? "Open Server" : "Close Server";

    }

    private void _on_ClientDisConnect_pressed()
    {
        var client = this.GetClient();
        var server = client.IntegratedServer.Server;

        if (server.IsRunning) {
            server.Stop();
            server.GetWorld().ClearPlayers();
            server.GetWorld().ClearBlocks();
            client.Disconnect();
        }

        if (client.Status == ConnectionStatus.Disconnected) {
            var address = "localhost";
            var port    = DEFAULT_PORT;
            if (ClientAddress.Text.Length > 0) {
                // TODO: Verify input some more, support IPv6?
                var split = ClientAddress.Text.Split(':');
                address = (split.Length > 1) ? split[0] : ClientAddress.Text;
                port    = (split.Length > 1) ? ushort.Parse(split[1]) : port;
            }
            client.Connect(address, port);
        } else {
            client.Disconnect();
        }
    }
}
