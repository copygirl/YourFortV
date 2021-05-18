using System;
using Godot;

public class Health : Node2D
{
    private static readonly TimeSpan VISIBLE_TIME = TimeSpan.FromSeconds(1.0);
    private static readonly TimeSpan FADE_TIME    = TimeSpan.FromSeconds(1.5);

    [Export] public int Segments { get; set; } = 6;
    [Export] public int InnerRadius { get; set; } = 14;
    [Export] public int OuterRadius { get; set; } = 24;
    [Export] public float Separation { get; set; } = 2.0F;

    private float _startAngle;
    private float _health;
    private float _visibilityTimer;

    public override void _Ready()
    {
        _startAngle = (-Mathf.Tau / 4) - (Mathf.Tau / Segments / 2);
        Visible = false;
    }

    public override void _Process(float delta)
    {
        if (!(this.GetClient().LocalPlayer is Player player))
            { Visible = false; return; }

        if (player.Health >= 1.0F) {
            if (!Visible) return;
            _visibilityTimer += delta;
            if (_visibilityTimer > (VISIBLE_TIME + FADE_TIME).TotalSeconds)
                { Visible = false; return; }
            else if (_visibilityTimer > VISIBLE_TIME.TotalSeconds)
                Modulate = new Color(Colors.White, 1.0F - (float)(
                    (_visibilityTimer - VISIBLE_TIME.TotalSeconds) / FADE_TIME.TotalSeconds));
        } else {
            Visible  = true;
            Modulate = Colors.White;
            _visibilityTimer = 0.0F;
        }

        Position = player.GetGlobalTransformWithCanvas().origin;
        _health  = player.Health;

        Update();
    }

    public override void _Draw()
    {
        var vertices = new Vector2[6];

        for (var i = 0; i < Segments; i++) {
            var angle1 = _startAngle + Mathf.Tau * ( i      / (float)Segments);
            var angle3 = _startAngle + Mathf.Tau * ((i + 1) / (float)Segments);
            var angle2 = (angle1 + angle3) / 2;

            var sep1 = Mathf.Polar2Cartesian(Separation, angle1 + Mathf.Tau / 4);
            var sep2 = Mathf.Polar2Cartesian(Separation, angle3 - Mathf.Tau / 4);

            vertices[0] = Mathf.Polar2Cartesian(InnerRadius, angle2);
            vertices[1] = Mathf.Polar2Cartesian(InnerRadius, angle1) + sep1;
            vertices[2] = Mathf.Polar2Cartesian(OuterRadius, angle1) + sep1;
            vertices[3] = Mathf.Polar2Cartesian(OuterRadius, angle2);
            vertices[4] = Mathf.Polar2Cartesian(OuterRadius, angle3) + sep2;
            vertices[5] = Mathf.Polar2Cartesian(InnerRadius, angle3) + sep2;

            DrawColoredPolygon(vertices, new Color(Colors.Black, 0.4F), antialiased: true);
        }

        for (var i = 0; i < Segments; i++) {
            var fullness = Mathf.Clamp((_health * Segments) - i, 0.0F, 1.0F);
            if (fullness <= 0.1) return;

            var angle1 = _startAngle + Mathf.Tau * ( i      / (float)Segments);
            var angle3 = _startAngle + Mathf.Tau * ((i + 1) / (float)Segments);
            var angle2 = (angle1 + angle3) / 2;

            var sep1 = Mathf.Polar2Cartesian(Separation + 1, angle1 + Mathf.Tau / 4);
            var sep2 = Mathf.Polar2Cartesian(Separation + 1, angle3 - Mathf.Tau / 4);

            var outerRadius = Mathf.Lerp(InnerRadius, OuterRadius, fullness);

            vertices[0] = Mathf.Polar2Cartesian(InnerRadius + 1, angle2);
            vertices[1] = Mathf.Polar2Cartesian(InnerRadius + 1, angle1) + sep1;
            vertices[2] = Mathf.Polar2Cartesian(outerRadius - 1, angle1) + sep1;
            vertices[3] = Mathf.Polar2Cartesian(outerRadius - 1, angle2);
            vertices[4] = Mathf.Polar2Cartesian(outerRadius - 1, angle3) + sep2;
            vertices[5] = Mathf.Polar2Cartesian(InnerRadius + 1, angle3) + sep2;

            DrawColoredPolygon(vertices, new Color(Colors.Red, 0.5F), antialiased: true);
        }
    }
}
