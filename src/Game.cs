using Godot;

public class Game : Node
{
    [Export] public Vector2 RoomSize { get; set; } = new Vector2(32, 18) * 16;

    [Export] public PackedScene Player { get; set; }
    [Export] public PackedScene Block { get; set; }

    public override void _Ready()
    {
        SpawnPlayer();
        SpawnBlocks();
    }

    private void SpawnPlayer()
    {
        var player = (Player)Player.Instance();
        AddChild(player);
    }

    private void SpawnBlocks()
    {
        void SpawnBlockAt(int x, int y)
        {
            var block = (Node2D)Block.Instance();
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
