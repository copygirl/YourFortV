using Godot;

public class Camera : Camera2D
{
    public int MaxDistance { get; } = 80;
    public Cursor Cursor { get; private set; }

    private Vector2 _rawPosition = Vector2.Zero;

    public override void _Ready()
    {
        Cursor = GetViewport().GetNode<Cursor>("Cursor");
    }

    public override void _Process(float delta)
    {
        var mousePos  = GetTree().Root.GetMousePosition();
        var centerPos = OS.WindowSize / 2;
        var target = !Cursor.Visible ? Vector2.Zero
            : ((mousePos - centerPos) / 4).Clamped(MaxDistance) * 2;
        _rawPosition = _rawPosition.LinearInterpolate(target, 0.05F).Round();
        Position = _rawPosition.Round();
    }
}
