using Godot;

public class Block : StaticBody2D, IInitializable
{
    public new BlockPos Position { get => BlockPos.FromVector(base.Position);
                                   set => base.Position = value.ToVector(); }
    public Color Color { get => Sprite.SelfModulate; set => Sprite.SelfModulate = value; }
    public bool Unbreakable { get; set; } = false;

    public Sprite Sprite { get; private set; }
    public void Initialize() => Sprite = GetNode<Sprite>("Sprite");
}
