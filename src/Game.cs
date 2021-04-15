using Godot;

public class Game : Node
{
    public static Game Instance { get; private set; }

    [Export] public Vector2 RoomSize { get; set; } = new Vector2(32, 18) * 16;
    [Export] public PackedScene BlockScene { get; set; }

    public Game() => Instance = this;

    // Using _EnterTree to make sure this code runs before any other.
    public override void _EnterTree()
        => GD.Randomize();

    public override void _Ready()
        => SpawnBlocks();

    private void SpawnBlocks()
    {
        void SpawnBlockAt(int x, int y)
        {
            var block = BlockScene.Init<Node2D>();
            block.Position = new Vector2(x, y);
            AddChild(block);
        }

        // Top and bottom.
        for (var x = (int)RoomSize.x / -2; x <= (int)RoomSize.x / 2; x += 16) {
            SpawnBlockAt(x, (int)RoomSize.y / -2);
            SpawnBlockAt(x, (int)RoomSize.y /  2);
        }

        // Left and right.
        for (var y = (int)RoomSize.y / -2 + 16; y <= (int)RoomSize.y / 2 - 16; y += 16) {
            SpawnBlockAt((int)RoomSize.x / -2, y);
            SpawnBlockAt((int)RoomSize.x /  2, y);
        }
    }
}
