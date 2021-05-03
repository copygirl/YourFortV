using Godot;

[Spawn, Sync, Save]
public class Block : StaticBody2D
{
    [Sync, Save]
    public new BlockPos Position {
        get => BlockPos.FromVector(base.Position);
        set => base.Position = this.SetSync(value).ToVector();
    }

    [Sync, Save]
    public Color Color {
        get => Modulate;
        set => Modulate = this.SetSync(value);
    }

    public bool Unbreakable { get; set; } = false;
}
