using System;
using Godot;

public class Bullet : Node2D
{
    private static readonly TimeSpan TRAIL_DURATION = TimeSpan.FromSeconds(0.6);

    public Vector2 Direction { get; }
    public int EffectiveRange { get; }
    public int MaximumRange { get; }
    public int Velocity { get; }
    public Color Color { get; }

    private readonly Vector2 _startPosition;
    private TimeSpan _age;
    private float _distance;

    public Bullet(Vector2 position, Vector2 direction,
        int effectiveRange, int maximumRange, int velocity, Color color)
    {
        _startPosition = Position = position;
        Direction      = direction;
        EffectiveRange = effectiveRange;
        MaximumRange   = maximumRange;
        Velocity = velocity;
        Color    = color;
    }

    public override void _Ready()
        { if (this.GetGame() is Server) Visible = false; }

    public override void _Process(float delta)
    {
        _age += TimeSpan.FromSeconds(delta);

        if (_age > TRAIL_DURATION) {
            Modulate = new Color(Modulate, Modulate.a - delta * 2);
            if (Modulate.a <= 0) this.RemoveFromParent();
        }
    }

    public override void _PhysicsProcess(float delta)
    {
        var previousPosition = Position;
        _distance = Mathf.Min(MaximumRange, Velocity * (float)_age.TotalSeconds);
        Position  = _startPosition + Direction * _distance;

        var collision = GetWorld2d().DirectSpaceState.IntersectRay(
            previousPosition, Position, collisionLayer: 0b11);
        if (collision.Count != 0) {
            Position  = (Vector2)collision["position"];
            _distance = _startPosition.DistanceTo(Position);
            SetPhysicsProcess(false);
        }

        if (_distance > MaximumRange)
            SetPhysicsProcess(false);

        Update();
    }

    public override void _Draw()
    {
        var numPoints = 2
            + ((_distance > 16) ? 1 : 0)
            + ((_distance > EffectiveRange) ? 1 : 0);
        var points = new Vector2[numPoints];
        var colors = new Color[numPoints];

        if (_distance > 16)
            colors[0] = new Color(Color, Color.a * Mathf.Min(1.0F, 1.0F - (_distance - EffectiveRange) / (MaximumRange - EffectiveRange)));

        if (_distance > EffectiveRange) {
            points[1] = Direction * -(_distance - EffectiveRange);
            colors[1] = Color;
        }

        points[points.Length - 2] = Direction * -Mathf.Max(0.0F, _distance - 16);
        points[points.Length - 1] = Direction * -_distance;

        colors[colors.Length - 2] = Color;
        colors[colors.Length - 1] = new Color(Color, 0.0F);

        DrawPolylineColors(points, colors, 1.5F, true);
    }
}
