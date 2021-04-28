using Godot;

[SyncObject("Block", "World/Blocks")]
public class Block : StaticBody2D
{
    [SyncProperty]
    public new BlockPos Position {
        get => BlockPos.FromVector(base.Position);
        set => base.Position = this.SetSync(value).ToVector();
    }

    [SyncProperty]
    public Color Color {
        get => Modulate;
        set => Modulate = this.SetSync(value);
    }

    public bool Unbreakable { get; set; } = false;
}
