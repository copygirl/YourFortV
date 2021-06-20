using Godot;

public class Block
{
    public const int LENGTH    = 16;
    public const int BIT_SHIFT = 4;


    public static readonly Block DEFAULT = new Block(
        GD.Load<Texture>("res://gfx/block.png"),
        new RectangleShape2D { Extents = new Vector2(0.5F, 0.5F) * LENGTH });


    public Texture Texture { get; }
    public Shape2D Shape { get; }

    public Block(Texture texture, Shape2D shape)
        { Texture = texture; Shape = shape; }
}
