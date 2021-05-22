using System;
using System.Collections.Generic;
using Godot;

public class Chunk : Node2D
{
    public const int LENGTH    = 32;
    public const int BIT_SHIFT = 5;
    public const int BIT_MASK  = ~(~0 << BIT_SHIFT);

    public (int, int) ChunkPosition { get; }

    public Chunk(int x, int y)
    {
        ChunkPosition = (x, y);
        Position      = new Vector2(x << (BIT_SHIFT + Block.BIT_SHIFT), y << (BIT_SHIFT + Block.BIT_SHIFT));
    }

    public ChunkLayer<T> GetLayerOrNull<T>()
        => GetNodeOrNull<ChunkLayer<T>>($"{typeof(T).Name}Layer");
    public ChunkLayer<T> GetOrCreateLayer<T>()
    {
        var layer = GetLayerOrNull<T>();
        if (layer == null) AddChild(layer = new ChunkLayer<T> { Name = $"{typeof(T).Name}Layer" });
        return layer;
    }

    // TODO: How should we handle chunk extends? Blocks can go "outside" of the current extends, since they're centered.
    // public override void _Draw()
    //     => DrawRect(new Rect2(Vector2.Zero, Vector2.One * (LENGTH * Block.LENGTH)), Colors.Blue, false);

}

public class ChunkLayer<T> : Node2D
{
    private static readonly IEqualityComparer<T> COMPARER = EqualityComparer<T>.Default;

    // TODO: Use one-dimensional array?
    private readonly T[,] _data = new T[Chunk.LENGTH, Chunk.LENGTH];
    private int _numNonDefault = 0;

    public T this[BlockPos pos] {
        get => this[pos.X, pos.Y];
        set => this[pos.X, pos.Y] = value;
    }
    public T this[int x, int y] {
        get { EnsureWithin(x, y); return _data[x, y]; }
        set {
            EnsureWithin(x, y);
            var previous = _data[x, y];
            if (COMPARER.Equals(value, previous)) return;

            if (!COMPARER.Equals(previous, default)) {
                if (previous is Node node) RemoveChild(node);
                _numNonDefault--;
            }
            if (!COMPARER.Equals(value, default)) {
                if (value is Node node) AddChild(node);
                _numNonDefault++;
            }
            _data[x, y] = value;
        }
    }

    public bool IsDefault => _numNonDefault == 0;

    private static void EnsureWithin(int x, int y)
    {
        if ((x < 0) || (x >= Chunk.LENGTH) || (y < 0) || (y >= Chunk.LENGTH)) throw new ArgumentException(
            $"x and y ({x},{y}) must be within chunk boundaries - (0,0) inclusive to ({Chunk.LENGTH},{Chunk.LENGTH}) exclusive");
    }
}
