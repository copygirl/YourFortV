using Godot;

public class Cursor : Node2D
{
    public override void _Ready()
    {
        Input.SetMouseMode(Input.MouseMode.Hidden);
    }

    public override void _Notification(int what)
    {
        switch (what) {
            case MainLoop.NotificationWmMouseEnter: Visible = true; break;
            case MainLoop.NotificationWmMouseExit: Visible = false; break;
        }
    }

    public override void _Process(float delta)
    {
        Position = GetGlobalMousePosition();
    }
}
