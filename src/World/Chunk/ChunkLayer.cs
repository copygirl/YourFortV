using System;
using System.Collections.Generic;
using System.IO;
using Godot;

public interface IChunkLayer
{
    bool IsDefault { get; }
    void Read(BinaryReader reader);
    void Write(BinaryWriter writer);
}

public interface IChunkLayer<T> : IChunkLayer
{
    T this[BlockPos pos] { get; set; }
    T this[int x, int y] { get; set; }
}


public static class ChunkLayerRegistry
{
    private static readonly Dictionary<string, Type> _types = new Dictionary<string, Type>();

    static ChunkLayerRegistry()
        => Register<BlockLayer>();

    public static void Register<T>()
        where T : Node2D, IChunkLayer
            => _types.Add(typeof(T).Name, typeof(T));

    public static IChunkLayer Create(string name)
    {
        var layer = (IChunkLayer)Activator.CreateInstance(_types[name]);
        ((Node)layer).Name = name;
        return layer;
    }
}

public static class ChunkLayerExtensions
{
    public static void FromBytes(this IChunkLayer layer, byte[] data)
    {
        using (var stream = new MemoryStream(data))
            layer.Read(stream);
    }
    public static void Read(this IChunkLayer layer, Stream stream)
    {
        using (var reader = new BinaryReader(stream))
            layer.Read(reader);
    }

    public static byte[] ToBytes(this IChunkLayer layer)
    {
        using (var stream = new MemoryStream()) {
            layer.Write(stream);
            return stream.ToArray();
        }
    }
    public static void Write(this IChunkLayer layer, Stream stream)
    {
        using (var writer = new BinaryWriter(stream))
            layer.Write(writer);
    }
}


public abstract class BasicChunkLayer<T> : Node2D, IChunkLayer<T>
{
    private static readonly IEqualityComparer<T> COMPARER = EqualityComparer<T>.Default;

    protected T[] Data { get; } = new T[Chunk.LENGTH * Chunk.LENGTH];
    protected bool Dirty { get; set; } = true;

    public Chunk Chunk => GetParent<Chunk>();
    public int NonDefaultCount { get; protected set; } = 0;
    public bool IsDefault => NonDefaultCount == 0;

    public T this[BlockPos pos] {
        get => this[pos.X, pos.Y];
        set => this[pos.X, pos.Y] = value;
    }
    public T this[int x, int y] {
        get {
            EnsureWithin(x, y);
            return Data[x | y << Chunk.BIT_SHIFT];
        }
        set {
            EnsureWithin(x, y);
            var index = x | y << Chunk.BIT_SHIFT;
            var previous = Data[index];
            if (COMPARER.Equals(value, previous)) return;
            if (!COMPARER.Equals(previous, default)) {
                if (previous is Node node) RemoveChild(node);
                NonDefaultCount--;
            }
            if (!COMPARER.Equals(value, default)) {
                if (value is Node node) AddChild(node);
                NonDefaultCount++;
            }
            Data[index] = value;
            Dirty = true;
        }
    }

    private static void EnsureWithin(int x, int y)
    {
        if ((x < 0) || (x >= Chunk.LENGTH) || (y < 0) || (y >= Chunk.LENGTH)) throw new ArgumentException(
            $"x and y ({x},{y}) must be within chunk boundaries - (0,0) inclusive to ({Chunk.LENGTH},{Chunk.LENGTH}) exclusive");
    }

    public abstract void Read(BinaryReader reader);
    public abstract void Write(BinaryWriter writer);
}
