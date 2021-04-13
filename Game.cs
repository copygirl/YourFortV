using Godot;
using System;

public class Game : Node
{
    public Vector2 PreferredScreenSize { get; } = new Vector2(640, 360);

    [Export] public PackedScene Player { get; set; }
    [Export] public PackedScene Block { get; set; }

    public override void _Ready()
    {
        GetTree().Connect("screen_resized", this, "OnWindowResized");
        OnWindowResized();

        SpawnPlayer();
        SpawnBlocks();
    }

    private void OnWindowResized()
    {
        var viewport = GetViewport();

        var scaleX = OS.WindowSize.x / PreferredScreenSize.x;
        var scaleY = OS.WindowSize.y / PreferredScreenSize.y;
        var scale  = Math.Max(1, Mathf.RoundToInt(Mathf.Min(scaleX, scaleY)));

        viewport.Size = (OS.WindowSize / scale).Ceil();

        // This prevents the viewport from being "squished" to fit the window.
        // The difference is only a few pixels, but it results in distortion
        // around the center horizontal and vertical lines of the screen.
        viewport.SetAttachToScreenRect(new Rect2(0, 0, viewport.Size * scale));
    }

    private void SpawnPlayer()
    {
        var player = (Player)Player.Instance();
        player.Position = PreferredScreenSize / 2;
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
        for (var x = 16; x <= (int)PreferredScreenSize.x - 16; x += 16) {
            SpawnBlockAt(x, 20);
            SpawnBlockAt(x, (int)PreferredScreenSize.y - 20);
        }

        // Left and right.
        for (var y = 36; y <= (int)PreferredScreenSize.y - 36; y += 16) {
            SpawnBlockAt(16, y);
            SpawnBlockAt((int)PreferredScreenSize.x - 16, y);
        }
    }
}
