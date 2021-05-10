using Godot;

public class EscapeMenu : Control
{
    public static EscapeMenu Instance { get; private set; }

    [Export] public NodePath ReturnPath { get; set; }
    public Button Return { get; private set; }

    public EscapeMenu() => Instance = this;

    public override void _Ready()
    {
        Return = GetNode<Button>(ReturnPath);
    }

    public override void _UnhandledInput(InputEvent @event)
        { if (@event.IsActionPressed("ui_menu")) Toggle(); }


    public void Toggle()
        { if (Visible) Close(); else Open(); }

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

    private void _on_Quit_pressed()
        => GetTree().Quit();
    private void _on_Return_pressed()
        => Close();
}
