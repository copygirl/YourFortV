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

    public IntegratedServer IntegratedServer { get; private set; }

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
        CallDeferred(nameof(SetupIntegratedServer));
    }
    private void SetupIntegratedServer()
    {
        IntegratedServer = new IntegratedServer();
        this.GetClient().AddChild(IntegratedServer);
        CallDeferred(nameof(StartIntegratedServerAndConnect));
    }
    private void StartIntegratedServerAndConnect()
    {
        var port = IntegratedServer.Server.StartSingleplayer();
        this.GetClient().Connect("127.0.0.1", port);
    }

    private void OnStatusChanged(ConnectionStatus status)
    {
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
                if (IntegratedServer == null) {
                    Status.Text     = "Connected!";
                    Status.Modulate = Colors.Green;
                } else if (IntegratedServer.Server.IsSingleplayer) {
                    Status.Text     = "Singleplayer";
                    Status.Modulate = Colors.White;
                } else {
                    Status.Text     = "Server Running";
                    Status.Modulate = Colors.Green;
                }
                break;
        }

        ServerPort.Editable      = IntegratedServer != null;
        ServerOpenClose.Disabled = IntegratedServer == null;
        ServerOpenClose.Text     = (IntegratedServer?.Server.IsSingleplayer == false) ? "Close Server" : "Open Server";
        ClientDisConnect.Text = ((IntegratedServer != null) || (status == ConnectionStatus.Disconnected)) ? "Connect" : "Disconnect";

        var pauseMode = (IntegratedServer?.Server.IsSingleplayer == true) ? PauseModeEnum.Stop : PauseModeEnum.Process;
        this.GetClient().GetNode("World").PauseMode = pauseMode;
        if (IntegratedServer != null) IntegratedServer.Server.GetNode("World").PauseMode = pauseMode;

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
        var server = IntegratedServer?.Server;
        var client = this.GetClient();
        if (server?.IsRunning != true) throw new InvalidOperationException();

        if (server.IsSingleplayer) {
            var port = (ServerPort.Text.Length > 0) ? ushort.Parse(ServerPort.Text) : DEFAULT_PORT;
            client.Disconnect();
            server.Stop();
            server.Start(port);
            client.Connect("127.0.0.1", port);
            // TODO: Pause server processing (including packets, RPC, Sync) until client reconnects?
            //       If we're doing that, also make sure to re-map packet and RPC targets to point to new NetworkID.
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

        if (IntegratedServer != null) {
            IntegratedServer.Server.Stop();
            NetworkSync.ClearAllObjects(IntegratedServer.Server);
            IntegratedServer.GetParent().RemoveChild(IntegratedServer);
            IntegratedServer.QueueFree();
            IntegratedServer = null;

            client.Disconnect();
            NetworkSync.ClearAllObjects(client);
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
            NetworkSync.ClearAllObjects(client);
        }
    }
}
