using Godot;

public class Camera : Camera2D
{
    public Cursor Cursor { get; private set; }

    public override void _EnterTree()
    {
        Cursor = GetViewport().GetNode<Cursor>("Cursor");
    }

    public override void _Process(float delta)
    {
        // TODO: Implement some kind of "zoom" mechanic?
        // var mousePos  = GetTree().Root.GetMousePosition();
        // var centerPos = OS.WindowSize / 2;
        // var scale     = ((Viewport)GetViewport()).Scale;
        // Position      = !Cursor.Visible ? Vector2.Zero
        //     : ((mousePos - centerPos) / scale).Clamped(MaxDistance) / 2;
    }
}
