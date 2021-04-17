using System.Linq;
using Godot;
using Godot.Collections;

public class Game : Node2D
{
    public static Game Instance { get; private set; }

    public static LocalPlayer LocalPlayer { get; internal set; }
    public static Cursor Cursor { get; private set; }


    [Export] public NodePath CursorPath { get; set; }
    [Export] public NodePath BlockContainerPath { get; set; }
    [Export] public PackedScene BlockScene { get; set; }

    public Node BlockContainer { get; private set; }

    public Game() => Instance = this;

    // Using _EnterTree to make sure this code runs before any other.
    public override void _EnterTree()
        => GD.Randomize();

    public override void _Ready()
    {
        Cursor         = GetNode<Cursor>(CursorPath);
        BlockContainer = GetNode(BlockContainerPath);
        SpawnDefaultBlocks();
    }

    public void ClearBlocks()
    {
        foreach (var block in BlockContainer.GetChildren())
            ((Node)block).Free();
    }

    public void SpawnDefaultBlocks()
    {
        for (var x = -6; x <= 6; x++) {
            var block = BlockScene.Init<Block>();
            block.Position = new Vector2(x * 16, 48);
            block.Modulate = Color.FromHsv(GD.Randf(), 0.1F, 1.0F);
            BlockContainer.AddChild(block);
        }
    }

    // FIXME: Can only be called during _physics_process?!
    public Block GetBlockAt(Vector2 position)
        => GetWorld2d().DirectSpaceState.IntersectPoint(position).Cast<Dictionary>()
            .Select(c => c["collider"]).OfType<Block>().FirstOrDefault();
}
