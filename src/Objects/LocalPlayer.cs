using System;
using Godot;

public class LocalPlayer : Player
{
    // TODO: Implement "low jumps" activated by releasing the jump button early.
    public TimeSpan JumpEarlyTime { get; } = TimeSpan.FromSeconds(0.2F);
    public TimeSpan JumpCoyoteTime { get; } = TimeSpan.FromSeconds(0.2F);

    public float MovementSpeed { get; set; } = 160;
    public float JumpVelocity { get; set; } = 240;
    public float Gravity { get; set; } = 480;

    public float Acceleration { get; set; } = 0.25F;
    public float GroundFriction { get; set; } = 0.2F;
    public float AirFriction { get; set; } = 0.05F;


    public Vector2 Velocity = Vector2.Zero;
    private DateTime? _jumpPressed = null;
    private DateTime? _lastOnFloor = null;

    public override void _Process(float delta)
    {
        base._Process(delta);
        RPC.Unreliable(1, Move, Position);
    }

    public override void _PhysicsProcess(float delta)
    {
        var moveDir     = 0.0F;
        var jumpPressed = false;
        if (!EscapeMenu.Instance.Visible && IsAlive) {
            moveDir     = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
            jumpPressed = Input.IsActionJustPressed("move_jump");
        }

        var friction = IsOnFloor() ? GroundFriction : AirFriction;
        Velocity.x = (moveDir != 0) ? Mathf.Lerp(Velocity.x, moveDir * MovementSpeed, Acceleration)
                                    : Mathf.Lerp(Velocity.x, 0, friction);
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

    [Puppet]
    public void ResetPosition(Vector2 position)
    {
        Position = position;
        Velocity = Vector2.Zero;
    }
}
