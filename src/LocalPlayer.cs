using System;
using Godot;

// TODO: Implement "low jumps" activated by releasing the jump button early.
public class LocalPlayer : Player
{
    public TimeSpan JumpEarlyTime { get; } = TimeSpan.FromSeconds(0.2F);
    public TimeSpan JumpCoyoteTime { get; } = TimeSpan.FromSeconds(0.2F);

    [Export] public float MovementSpeed { get; set; } = 160;
    [Export] public float JumpVelocity { get; set; } = 240;
    [Export] public float Gravity { get; set; } = 480;

    [Export(PropertyHint.Range, "0,1")]
    public float Friction { get; set; } = 0.1F;
    [Export(PropertyHint.Range, "0,1")]
    public float Acceleration { get; set; } = 0.25F;

    public Vector2 Velocity = Vector2.Zero;
    private DateTime? _jumpPressed = null;
    private DateTime? _lastOnFloor = null;

    public override void _EnterTree() => Game.LocalPlayer = this;
    public override void _ExitTree() => Game.LocalPlayer = null;

    public override void _PhysicsProcess(float delta)
    {
        var moveDir     = 0.0F;
        var jumpPressed = false;
        if (!EscapeMenu.Instance.Visible) {
            moveDir     = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
            jumpPressed = Input.IsActionJustPressed("move_jump");
        }

        Velocity.x = (moveDir != 0) ? Mathf.Lerp(Velocity.x, moveDir * MovementSpeed, Acceleration)
                                     : Mathf.Lerp(Velocity.x, 0, Friction);
        Velocity.y += Gravity * delta;
        Velocity = MoveAndSlide(Velocity, Vector2.Up);

        if (jumpPressed) _jumpPressed = DateTime.Now;
        if (IsOnFloor()) _lastOnFloor = DateTime.Now;

        if (((DateTime.Now - _jumpPressed) <= JumpEarlyTime) &&
            ((DateTime.Now - _lastOnFloor) <= JumpCoyoteTime)) {
            Velocity.y  = -JumpVelocity;
            _jumpPressed = null;
            _lastOnFloor = null;
        }
    }
}
