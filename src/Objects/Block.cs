using Godot;

public class Block : StaticBody2D, IInitializable
{
    public const int LENGTH    = 16;
    public const int BIT_SHIFT = 4;

    public BlockPos GlobalBlockPos { get => BlockPos.FromVector(GlobalPosition); set => GlobalPosition = value.ToVector(); }
    public BlockPos ChunkLocalBlockPos { get => BlockPos.FromVector(Position); set => Position = value.ToVector(); }
    public Color Color { get => Sprite.SelfModulate; set => Sprite.SelfModulate = value; }
    public bool Unbreakable { get; set; } = false;

    public Sprite Sprite { get; private set; }
    public void Initialize() => Sprite = GetNode<Sprite>("Sprite");
}
