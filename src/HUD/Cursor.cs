using Godot;

public class Cursor : Node2D
{
    public Vector2 ScreenPosition { get; private set; }

    public override void _Ready()
    {
        Input.SetMouseMode(Input.MouseMode.Hidden);
    }

    public override void _Notification(int what)
    {
        // TODO: Keep mouse visible when it was pressed down in the game window.
        //       Meaning the game will continue to receive mouse move updates.
        switch (what) {
            case MainLoop.NotificationWmMouseEnter: Visible = true; break;
            case MainLoop.NotificationWmMouseExit: Visible = false; break;
        }
    }

    public override void _Process(float delta)
    {
        ScreenPosition = GetGlobalMousePosition();
        Position       = ScreenPosition - GetViewport().CanvasTransform.origin;
    }
}
