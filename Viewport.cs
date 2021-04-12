using Godot;
using System;

public class Viewport : Godot.Viewport
{
    public TextureRect ViewportTexture { get; private set; }
    public Vector2 DefaultSize { get; private set; }
    public int Scale { get; private set; }

    public override void _Ready()
    {
        ViewportTexture = GetNode<TextureRect>("../ViewportTexture");
        ViewportTexture.Texture = GetTexture();
        GetTree().Root.Connect("size_changed", this, "OnWindowResized");

        RenderDirectToScreen = false;
        DefaultSize = Size;

        OnWindowResized();
        SpawnBlocks();
    }

    private void OnWindowResized()
    {
        var windowSize = GetTree().Root.Size;
        Scale = Math.Max(1, Mathf.RoundToInt(Mathf.Min(
            OS.WindowSize.x / DefaultSize.x,
            OS.WindowSize.y / DefaultSize.y)));
        Size = windowSize / Scale;
        ViewportTexture.RectScale = Vector2.One * Scale;
    }

    private void SpawnBlocks()
    {
        var blockScene = GD.Load<PackedScene>("res://Block.tscn");
        void SpawnBlockAt(int x, int y)
        {
            var block = (Node2D)blockScene.Instance();
            block.Position = new Vector2(x, y);
            AddChild(block);
        }

        // Top and bottom.
        for (var x = 16; x <= (int)DefaultSize.x - 16; x += 16) {
            SpawnBlockAt(x, 20);
            SpawnBlockAt(x, (int)DefaultSize.y - 20);
        }

        // Left and right.
        for (var y = 36; y <= (int)DefaultSize.y - 36; y += 16) {
            SpawnBlockAt(16, y);
            SpawnBlockAt((int)DefaultSize.x - 16, y);
        }
    }
}
