using System;
using Godot;

public class BlockRef
{
    public World World { get; }
    public BlockPos Position { get; }

    public BlockRef(World world, BlockPos position)
    {
        World    = world;
        Position = position;
    }


    public Chunk GetChunk(bool create)
        => World.GetChunk(Position.ToChunkPos(), create);

    public IChunkLayer<T> GetChunkLayer<T>(bool create)
        => GetChunk(create)?.GetLayer<T>(create);

    public BlockEntity GetEntity(bool create)
        => GetChunk(create)?.GetBlockEntity(Position.GlobalToChunkRel(), create);


    public T Get<T>()
    {
        if (ChunkLayerRegistry.TryGetDefault<T>(out var @default)) {
            var layer = GetChunkLayer<T>(false);
            return (layer != null) ? layer[Position.GlobalToChunkRel()] : @default;
        } else if (typeof(Node).IsAssignableFrom(typeof(T)))
            return (T)(object)GetEntity(false)?.GetNodeOrNull(typeof(T).Name);
        else throw new ArgumentException($"Unable to access {typeof(T).Name} on a Block", nameof(T));
    }

    public T GetOrCreate<T>() where T : Node, new()
        => GetEntity(true).GetOrCreateChild(typeof(T).Name, () => new T());

    public void Set<T>(T value)
    {
        if (ChunkLayerRegistry.Has<T>())
            GetChunkLayer<T>(true)[Position.GlobalToChunkRel()] = value;
        else if (typeof(Node).IsAssignableFrom(typeof(T))) {
            var entity   = GetEntity(true);
            var existing = entity.GetNodeOrNull(typeof(T).Name);
            existing?.RemoveFromParent();
            entity.AddChild((Node)(object)value);
        } else throw new ArgumentException($"Unable to access {typeof(T).Name} on a Block", nameof(T));
    }
}
