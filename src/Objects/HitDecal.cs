using System;
using Godot;

public class HitDecal : Sprite
{
    private static readonly TimeSpan LIFE_TIME = TimeSpan.FromSeconds(5.0);
    private static readonly TimeSpan FADE_TIME = TimeSpan.FromSeconds(5.0);
    private static readonly Material MATERIAL = GD.Load<Material>("res://decal_material.tres");

    private readonly float _fadeFactor;
    private TimeSpan _age = TimeSpan.Zero;

    public HitDecal(Texture own, Texture target, Vector2 hitVec, Color color)
    {
        Texture  = own;
        Material = (Material)MATERIAL.Duplicate();
        Position = (hitVec - own.GetSize() / 2).Round();
        Centered = false;

        var offset = Position + target.GetSize() / 2;
        ((ShaderMaterial)Material).SetShaderParam("offset", new Vector3(offset.x, offset.y, 0));
        ((ShaderMaterial)Material).SetShaderParam("mask", target);

        Modulate    = color;
        _fadeFactor = color.a;
    }

    public override void _Process(float delta)
    {
        _age += TimeSpan.FromSeconds(delta);
        if (_age > LIFE_TIME) {
            var dec  = delta / (float)FADE_TIME.TotalSeconds * _fadeFactor;
            Modulate = new Color(Modulate, Modulate.a - dec);
            if (Modulate.a <= 0) this.RemoveFromParent();
        }
    }
}
