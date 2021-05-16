using System;
using Godot;

// TODO: When spawned, add to multiple nearby sprites.
public class HitDecal : Sprite
{
    private static readonly TimeSpan LIFE_TIME = TimeSpan.FromSeconds(5.0);
    private static readonly TimeSpan FADE_TIME = TimeSpan.FromSeconds(5.0);

    private TimeSpan _age = TimeSpan.Zero;
    private float _fadeFactor;

    public void Add(Sprite sprite, Vector2 hitVec, Color color)
    {
        Position   = (hitVec - Texture.GetSize() / 2).Round();
        var offset = Position + sprite.Texture.GetSize() / 2;

        ShaderMaterial material;
        Material = material = (ShaderMaterial)Material.Duplicate();
        material.SetShaderParam("offset", new Vector3(offset.x, offset.y, 0));
        material.SetShaderParam("mask", sprite.Texture);

        Modulate    = color;
        _fadeFactor = color.a;

        sprite.AddChild(this);
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
