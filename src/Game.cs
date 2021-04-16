using Godot;

public class Game : Node
{
    public static Game Instance { get; private set; }

    [Export] public NodePath BlockContainerPath { get; set; }
    [Export] public PackedScene BlockScene { get; set; }

    public Node BlockContainer { get; private set; }

    public Game() => Instance = this;

    // Using _EnterTree to make sure this code runs before any other.
    public override void _EnterTree()
        => GD.Randomize();

    public override void _Ready()
    {
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
            var block = BlockScene.Init<Node2D>();
            block.Position = new Vector2(x * 16, 48);
            block.Modulate = Color.FromHsv(GD.Randf(), 0.1F, 1.0F);
            BlockContainer.AddChild(block);
        }
    }
}
