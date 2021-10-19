using System;
using System.Collections.Generic;
using Godot;
using MessagePack;

[MessagePackFormatter(typeof(ChunkFormatter))]
public partial class Chunk : Node2D
{
    public const int LENGTH    = 32;
    public const int BIT_SHIFT = 5;
    public const int BIT_MASK  = ~(~0 << BIT_SHIFT);


    private readonly List<IChunkLayer> _layers = new List<IChunkLayer>();

    public (int X, int Y) ChunkPos { get; }
    public IEnumerable<IChunkLayer> Layers {
        get => _layers.AsReadOnly();
        internal set {
            foreach (var layer in _layers)
                layer.Changed -= OnLayerChanged;
            _layers.Clear();

            foreach (var layer in value) {
                _layers.Add(layer);
                layer.Changed += OnLayerChanged;
            }
        }
    }

    public Chunk((int X, int Y) chunkPos)
    {
        Name = $"Chunk ({chunkPos})";
        ChunkPos = chunkPos;
        Position = new Vector2(chunkPos.X << (BIT_SHIFT + Block.BIT_SHIFT),
                               chunkPos.Y << (BIT_SHIFT + Block.BIT_SHIFT));
    }


    public IChunkLayer<T> GetLayer<T>(bool create)
        => (IChunkLayer<T>)GetLayer(typeof(T), create);
    public IChunkLayer GetLayer(Type type, bool create)
    {
        var layer = _layers.Find(l => l.AccessType == type);
        if ((layer == null) && create) {
            layer = ChunkLayerRegistry.Create(type);
            layer.Changed += OnLayerChanged;
            _layers.Add(layer);
        }
        return layer;
    }
    public void OnLayerChanged(IChunkLayer layer)
        => _dirty = true;


    public BlockEntity GetBlockEntity(BlockPos pos, bool create)
    {
        EnsureWithinBounds(pos);
        return create ? this.GetOrCreateChild(pos.ToString(), () => new BlockEntity())
                      : GetNode<BlockEntity>(pos.ToString());
    }


    public static void EnsureWithinBounds(BlockPos pos)
    {
        if ((pos.X < 0) || (pos.X >= LENGTH) || (pos.Y < 0) || (pos.Y >= LENGTH)) throw new ArgumentException(
            $"{pos} must be within chunk boundaries - (0,0) inclusive to ({LENGTH},{LENGTH}) exclusive");
    }

    // TODO: How should we handle chunk extends? Blocks can go "outside" of the current extends, since they're centered.
    // public override void _Draw()
    //     => DrawRect(new Rect2(Vector2.Zero, Vector2.One * (LENGTH * Block.LENGTH)), Colors.Blue, false);

    public static int GetIndex(BlockPos pos) => pos.X | pos.Y << BIT_SHIFT;
    public static int GetIndex(int x, int y) => x | y << BIT_SHIFT;
}
