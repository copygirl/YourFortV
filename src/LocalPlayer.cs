using System;
using Godot;

public class LocalPlayer : Player
{
    public static LocalPlayer Instance { get; private set; }

    public TimeSpan JumpEarlyTime { get; } = TimeSpan.FromSeconds(0.2F);
    public TimeSpan JumpCoyoteTime { get; } = TimeSpan.FromSeconds(0.2F);

    [Export] public float Speed { get; set; } = 120;
    [Export] public float JumpSpeed { get; set; } = 180;
    [Export] public float Gravity { get; set; } = 400;

    [Export(PropertyHint.Range, "0,1")]
    public float Friction { get; set; } = 0.1F;
    [Export(PropertyHint.Range, "0,1")]
    public float Acceleration { get; set; } = 0.25F;

    public Vector2 Velocity = Vector2.Zero;
    private DateTime? _jumpPressed = null;
    private DateTime? _lastOnFloor = null;

    public override void _EnterTree() => Instance = this;
    public override void _ExitTree() => Instance = null;

    public override void _PhysicsProcess(float delta)
    {
        var moveDir = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
        Velocity.x = (moveDir != 0) ? Mathf.Lerp(Velocity.x, moveDir * Speed, Acceleration)
                                     : Mathf.Lerp(Velocity.x, 0, Friction);
        Velocity.y += Gravity * delta;
        Velocity = MoveAndSlide(Velocity, Vector2.Up);

        if (Input.IsActionJustPressed("move_jump"))
            _jumpPressed = DateTime.Now;
        if (IsOnFloor())
            _lastOnFloor = DateTime.Now;

        if (((DateTime.Now - _jumpPressed) <= JumpEarlyTime) &&
            ((DateTime.Now - _lastOnFloor) <= JumpCoyoteTime)) {
            Velocity.y  = -JumpSpeed;
            _jumpPressed = null;
            _lastOnFloor = null;
        }
    }

    internal void ResetPositionInternal(Vector2 position)
    {
        Position = position;
        Velocity = Vector2.Zero;
    }
    [Puppet]
    internal void ResetPosition(Vector2 position)
        => ResetPositionInternal(position);
}
