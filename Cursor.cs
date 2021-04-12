using Godot;

public class Cursor : Node2D
{
    public override void _Ready()
    {
        Visible = false;
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
        var viewport = (Viewport)GetViewport();
        var origin   = viewport.CanvasTransform.origin;
        Position = viewport.GetMousePosition() / viewport.Scale - origin;
    }
}
