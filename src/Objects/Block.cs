using Godot;

[Sync]
public class Block : StaticBody2D
{
    [Sync]
    public new BlockPos Position {
        get => BlockPos.FromVector(base.Position);
        set => base.Position = this.SetSync(value).ToVector();
    }

    [Sync]
    public Color Color {
        get => Modulate;
        set => Modulate = this.SetSync(value);
    }

    public bool Unbreakable { get; set; } = false;
}
