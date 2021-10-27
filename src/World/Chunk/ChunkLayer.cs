using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using MessagePack;
using Expression = System.Linq.Expressions.Expression;

[Union(0, typeof(BlockLayer))]
[Union(1, typeof(ColorLayer))]
public interface IChunkLayer : IDeSerializable
{
    Type AccessType { get; }
    bool IsDefault { get; }
    event Action<IChunkLayer, BlockPos> Changed;
}

public interface IChunkLayer<T> : IChunkLayer
{
    T this[BlockPos pos] { get; set; }
    T this[int x, int y] { get; set; }
    T this[int index] { get; }
}

public class ArrayChunkLayer<T> : IChunkLayer<T>
{
    private static readonly IEqualityComparer<T> COMPARER = EqualityComparer<T>.Default;

    private T[] _data = new T[Chunk.LENGTH * Chunk.LENGTH];
    public int NonDefaultCount { get; protected set; } = 0;

    public Type AccessType => typeof(T);
    public bool IsDefault  => NonDefaultCount == 0;

    public event Action<IChunkLayer, BlockPos> Changed;

    public T this[int index] => _data[index];
    public T this[int x, int y] {
        get => this[Chunk.GetIndex(x, y)];
        set => this[new BlockPos(x, y)] = value;
    }
    public T this[BlockPos pos] {
        get => this[Chunk.GetIndex(pos.X, pos.Y)];
        set {
            var index = Chunk.GetIndex(pos.X, pos.Y);
            var previous = _data[index];
            if (COMPARER.Equals(value, previous)) return;
            _data[index] = value;

            if (!COMPARER.Equals(previous, default)) NonDefaultCount--;
            if (!COMPARER.Equals(value, default)) NonDefaultCount++;
            Changed?.Invoke(this, pos);
        }
    }

    public void Serialize(ref MessagePackWriter writer, MessagePackSerializerOptions options)
    {
        writer.Write(NonDefaultCount);
        MessagePackSerializer.Serialize(ref writer, _data);
    }
    public void Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        NonDefaultCount = reader.ReadInt32();
        _data = MessagePackSerializer.Deserialize<T[]>(ref reader);
    }
}

public class TranslationLayer<TData, TAccess> : IChunkLayer<TAccess>
{
    private readonly ArrayChunkLayer<TData> _data = new ArrayChunkLayer<TData>();
    private readonly Func<TData, TAccess> _from;
    private readonly Func<TAccess, TData> _to;

    public TranslationLayer(Func<TData, TAccess> from, Func<TAccess, TData> to)
        { _from = from; _to = to; }

    public Type AccessType => typeof(TAccess);
    public bool IsDefault  => _data.IsDefault;
    public event Action<IChunkLayer, BlockPos> Changed { add => _data.Changed += value; remove => _data.Changed -= value; }
    public TAccess this[BlockPos pos] { get => _from(_data[pos]); set => _data[pos] = _to(value); }
    public TAccess this[int x, int y] { get => _from(_data[x, y]); set => _data[x, y] = _to(value); }
    public TAccess this[int index] => _from(_data[index]);

    public void Serialize(ref MessagePackWriter writer, MessagePackSerializerOptions options)
        => _data.Serialize(ref writer, options);
    public void Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        => _data.Deserialize(ref reader, options);
}

[MessagePackFormatter(typeof(DeSerializableFormatter<BlockLayer>))]
public class BlockLayer : TranslationLayer<byte, Block>
{
    public static readonly Block DEFAULT = Blocks.AIR;
    public BlockLayer() : base(i => BlockRegistry.Get(i), b => (byte)b.ID) {  }
}

[MessagePackFormatter(typeof(DeSerializableFormatter<ColorLayer>))]
public class ColorLayer : TranslationLayer<int, Color>
{
    public static readonly Color DEFAULT = Colors.White;
    public ColorLayer() : base(i => new Color(i), c => c.ToRgba32()) {  }
}


public static class ChunkLayerRegistry
{
    private static readonly Dictionary<Type, Func<IChunkLayer>> _factories
        = new Dictionary<Type, Func<IChunkLayer>>();
    private static readonly Dictionary<Type, object> _defaults
        = new Dictionary<Type, object>();

    static ChunkLayerRegistry()
    {
        foreach (var attr in typeof(IChunkLayer).GetCustomAttributes<UnionAttribute>()) {
            var id   = (byte)attr.Key;
            var type = attr.SubType;
            var ctor = type.GetConstructor(Type.EmptyTypes);
            var fact = Expression.Lambda<Func<IChunkLayer>>(Expression.New(ctor));
            var storedType = type.GetInterfaces()
                .Single(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(IChunkLayer<>)))
                .GenericTypeArguments[0];
            _factories.Add(storedType, fact.Compile());
            _defaults.Add(storedType, type.GetField("DEFAULT").GetValue(null));
        }
    }

    public static bool Has<T>()
        => _factories.ContainsKey(typeof(T));

    public static bool TryGetDefault<T>(out T @default)
    {
        if (_defaults.TryGetValue(typeof(T), out var defaultObj))
            { @default = (T)defaultObj; return true; }
        else { @default = default; return false; }
    }

    public static IChunkLayer<T> Create<T>()
        => (IChunkLayer<T>)Create(typeof(T));
    public static IChunkLayer Create(Type type)
        => _factories[type]();


    // static ChunkLayerRegistry()
    // {
    //     Register(0, Blocks.AIR, () => new BlockLayer());
    //     Register(1, Colors.White, () => new ColorLayer());
    // }

    // public static void Register<T>(byte id, T @default, Func<IChunkLayer<T>> factory)
    // {
    //     var info = new Info<T>(id, @default, factory);
    //     _byType.Add(typeof(T), info);
    //     _byID.Add(id, info);
    // }

    // public interface IInfo
    // {
    //     Type Type { get; }
    //     byte ID { get; }
    //     object Default { get; }
    //     Func<IChunkLayer> Factory { get; }
    // }

    // public class Info<T> : IInfo
    // {
    //     public byte ID { get; }
    //     public T Default { get; }
    //     public Func<IChunkLayer<T>> Factory { get; }

    //     Type IInfo.Type => typeof(T);
    //     object IInfo.Default => Default;
    //     Func<IChunkLayer> IInfo.Factory => Factory;

    //     public Info(byte id, T @default, Func<IChunkLayer<T>> factory)
    //         { ID = id; Default = @default; Factory = factory; }
    // }

    // public static bool TryGet<T>(out Info<T> info)
    // {
    //     if (TryGet(typeof(T), out var infoObj))
    //         { info = (Info<T>)infoObj; return true; }
    //     else { info = null; return false; }
    // }
    // public static bool TryGet(Type type, out IInfo info)
    //     => _byType.TryGetValue(type, out info);
    // public static bool TryGet(byte id, out IInfo info)
    //     => _byID.TryGetValue(id, out info);
}
