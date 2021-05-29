using System;
using Godot;

public class PlayerMovement : Node
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

    private Player _player;
    private DateTime? _jumpPressed = null;
    private DateTime? _lastOnFloor = null;

    public override void _Ready()
        => _player = GetParent<Player>();

    public override void _Process(float delta)
        => RPC.Unreliable(1, _player.Move, _player.Position);

    public override void _PhysicsProcess(float delta)
    {
        var moveDir     = 0.0F;
        var jumpPressed = false;
        if (!EscapeMenu.Instance.Visible && _player.IsAlive) {
            moveDir     = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
            jumpPressed = Input.IsActionJustPressed("move_jump");
        }

        var velocity = _player.Velocity;
        var friction = _player.IsOnFloor() ? GroundFriction : AirFriction;
        velocity.x = (moveDir != 0) ? Mathf.Lerp(_player.Velocity.x, moveDir * MovementSpeed, Acceleration)
                                    : Mathf.Lerp(_player.Velocity.x, 0, friction);
        velocity.y += Gravity * delta;
        _player.Velocity = _player.MoveAndSlide(velocity, Vector2.Up);

        if (jumpPressed) _jumpPressed = DateTime.Now;
        if (_player.IsOnFloor()) _lastOnFloor = DateTime.Now;

        if (((DateTime.Now - _jumpPressed) <= JumpEarlyTime) &&
            ((DateTime.Now - _lastOnFloor) <= JumpCoyoteTime)) {
            _player.Velocity -= new Vector2(0, JumpVelocity);
            _jumpPressed = null;
            _lastOnFloor = null;
        }
    }
}
