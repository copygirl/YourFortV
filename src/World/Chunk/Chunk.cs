using System.Collections.Generic;
using System.Linq;
using Godot;

public class Chunk : Node2D
{
    public const int LENGTH    = 32;
    public const int BIT_SHIFT = 5;
    public const int BIT_MASK  = ~(~0 << BIT_SHIFT);

    public (int X, int Y) ChunkPosition { get; }
    public IEnumerable<IChunkLayer> Layers
        => GetChildren().OfType<IChunkLayer>();

    public Chunk((int X, int Y) chunkPos)
    {
        Name = $"Chunk ({chunkPos})";
        ChunkPosition = chunkPos;
        Position = new Vector2(chunkPos.X << (BIT_SHIFT + Block.BIT_SHIFT),
                               chunkPos.Y << (BIT_SHIFT + Block.BIT_SHIFT));
    }

    public T GetLayerOrNull<T>()   => (T)GetLayerOrNull(typeof(T).Name);
    public T GetOrCreateLayer<T>() => (T)GetOrCreateLayer(typeof(T).Name);

    public IChunkLayer GetLayerOrNull(string name)
        => GetNodeOrNull<IChunkLayer>(name);
    public IChunkLayer GetOrCreateLayer(string name)
    {
        var layer = GetLayerOrNull(name);
        if (layer == null) AddChild((Node)(layer = ChunkLayerRegistry.Create(name)));
        return layer;
    }

    // TODO: How should we handle chunk extends? Blocks can go "outside" of the current extends, since they're centered.
    // public override void _Draw()
    //     => DrawRect(new Rect2(Vector2.Zero, Vector2.One * (LENGTH * Block.LENGTH)), Colors.Blue, false);
}
