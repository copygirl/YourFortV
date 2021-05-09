using Godot;

public class Block : StaticBody2D
{
    public new BlockPos Position { get => BlockPos.FromVector(base.Position);
                                   set => base.Position = value.ToVector(); }
    public Color Color { get => Modulate; set => Modulate = value; }
    public bool Unbreakable { get; set; } = false;
}
