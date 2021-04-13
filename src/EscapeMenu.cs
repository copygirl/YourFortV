using Godot;

public class EscapeMenu : Container
{
    [Export] public int DefaultPort { get; set; } = 25565;
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

    public override void _EnterTree()
    {
        Status           = GetNode<Label>(StatusPath);
        ServerStartStop  = GetNode<Button>(ServerStartStopPath);
        ServerPort       = GetNode<LineEdit>(ServerPortPath);
        ClientDisConnect = GetNode<Button>(ClientDisConnectPath);
        ClientAddress    = GetNode<LineEdit>(ClientAddressPath);
        Return           = GetNode<Button>(ReturnPath);

        ServerPort.PlaceholderText    = DefaultPort.ToString();
        ClientAddress.PlaceholderText = $"{DefaultAddress}:{DefaultPort}";
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
        GetTree().Paused = true;
        Return.GrabFocus();
        Visible = true;
    }

    public void Close()
    {
        if (!Visible) return;
        GetTree().Paused = false;
        Visible = false;
    }

    #pragma warning disable IDE0051
    #pragma warning disable IDE1006

    private void _on_ServerStartStop_pressed()
    {

    }

    private void _on_ClientDisConnect_pressed()
    {

    }

    private void _on_HideAddress_toggled(bool pressed)
        => ClientAddress.Secret = pressed;
    private void _on_Quit_pressed()
        => GetTree().Quit();
    private void _on_Return_pressed()
        => Close();
}
