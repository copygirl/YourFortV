using Godot;
using System;

public class Player : KinematicBody2D
{
    public TimeSpan JumpEarlyTime { get; } = TimeSpan.FromSeconds(0.2F);
    public TimeSpan JumpCoyoteTime { get; } = TimeSpan.FromSeconds(0.2F);

    [Export] public float Speed { get; set; } = 120;
    [Export] public float JumpSpeed { get; set; } = 180;
    [Export] public float Gravity { get; set; } = 400;

    [Export(PropertyHint.Range, "0,1")]
    public float Friction { get; set; } = 0.1F;
    [Export(PropertyHint.Range, "0,1")]
    public float Acceleration { get; set; } = 0.25F;

    private Vector2 _velocity = Vector2.Zero;
    private DateTime? _jumpPressed = null;
    private DateTime? _lastOnFloor = null;

    public override void _PhysicsProcess(float delta)
    {
        var moveDir = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
        _velocity.x = (moveDir != 0) ? Mathf.Lerp(_velocity.x, moveDir * Speed, Acceleration)
                                     : Mathf.Lerp(_velocity.x, 0, Friction);
        _velocity.y += Gravity * delta;
        _velocity = MoveAndSlide(_velocity, Vector2.Up);

        if (Input.IsActionJustPressed("move_jump"))
            _jumpPressed = DateTime.Now;
        if (IsOnFloor())
            _lastOnFloor = DateTime.Now;

        if (((DateTime.Now - _jumpPressed) <= JumpEarlyTime) &&
            ((DateTime.Now - _lastOnFloor) <= JumpCoyoteTime)) {
            _velocity.y  = -JumpSpeed;
            _jumpPressed = null;
            _lastOnFloor = null;
        }
    }
}
