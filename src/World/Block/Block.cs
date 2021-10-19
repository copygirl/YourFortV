using System;
using Godot;

public class Block
{
    public const int LENGTH    = 16;
    public const int BIT_SHIFT = 4;

    public int ID { get; internal set; } = -1;
    public string Name { get; }

    public Texture Texture { get; set; } = null;
    public Shape2D Shape { get; set; } = null;
    public bool IsReplacable { get; set; } = false;

    public Block(string name) => Name = name;
    public override string ToString() => $"Block(\"{Name}\")";
}

public static class Blocks
{
    public static readonly Block AIR = BlockRegistry.Register(0, new Block("air"){ IsReplacable = true });
    public static readonly Block DEFAULT = BlockRegistry.Register(1, new Block("default"){
        Texture = GD.Load<Texture>("res://gfx/block.png"),
        Shape   = new RectangleShape2D { Extents = new Vector2(0.5F, 0.5F) * Block.LENGTH },
    });
}

public static class BlockRegistry
{
    public const int MAX_BLOCK_ID = 255;

    private static readonly Block[] _blocks = new Block[MAX_BLOCK_ID + 1];

    public static T Register<T>(int id, T block) where T : Block
    {
        if ((id < 0) || (id > MAX_BLOCK_ID)) throw new ArgumentOutOfRangeException(nameof(id));
        if (_blocks[id] != null) throw new ArgumentException($"ID {id} is already in use by {_blocks[id]}", nameof(id));
        if (block.ID != -1) throw new ArgumentException($"Block {block} has already been registered", nameof(block));

        _blocks[id] = block;
        block.ID = id;
        return block;
    }

    public static Block Get(int id)
    {
        if ((id < 0) || (id > MAX_BLOCK_ID)) throw new ArgumentOutOfRangeException(nameof(id));
        return _blocks[id];
    }
}
