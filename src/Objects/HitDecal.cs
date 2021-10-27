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

    public static void Spawn(World world, NodePath path, Vector2 hitPosition, Color color)
    {
        var decal = GD.Load<Texture>("res://gfx/hit_decal.png");
        var node  = world.GetNode(path);
        switch (node) {
            case Sprite sprite:
                node.AddChild(new HitDecal(decal, sprite.Texture, hitPosition, color));
                break;
            case Chunk chunk:
                hitPosition += chunk.Position;
                var start = BlockPos.FromVector((hitPosition - decal.GetSize() / 2).Floor());
                var end   = BlockPos.FromVector((hitPosition + decal.GetSize() / 2).Ceil());
                for (var x = start.X; x <= end.X; x++)
                for (var y = start.Y; y <= end.Y; y++) {
                    var blockPos = new BlockPos(x, y);
                    var texture  = world[blockPos].Get<Block>().Texture;
                    if (texture == null) continue;
                    world[blockPos].GetOrCreate<HitDecals>().AddChild(new HitDecal(
                        decal, texture, hitPosition - blockPos.ToVector(), color));
                }
                break;
        }
    }
}

public class HitDecals : Node2D, INotifyChildRemoved
{
    public void OnChildRemoved(Node child)
    {
        if (GetChildCount() == 0)
            this.RemoveFromParent();
    }
}
