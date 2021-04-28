using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;

public static class DeSerializerRegistry
{
    private static readonly Dictionary<Type, IDeSerializer> _byType = new Dictionary<Type, IDeSerializer>();
    private static readonly List<IDeSerializerGenerator> _generators = new List<IDeSerializerGenerator>();

    static DeSerializerRegistry()
    {
        Register((w, value) => w.Write(value), r => r.ReadBoolean());
        Register((w, value) => w.Write(value), r => r.ReadByte());
        Register((w, value) => w.Write(value), r => r.ReadSByte());
        Register((w, value) => w.Write(value), r => r.ReadInt16());
        Register((w, value) => w.Write(value), r => r.ReadUInt16());
        Register((w, value) => w.Write(value), r => r.ReadInt32());
        Register((w, value) => w.Write(value), r => r.ReadUInt32());
        Register((w, value) => w.Write(value), r => r.ReadInt64());
        Register((w, value) => w.Write(value), r => r.ReadUInt64());
        Register((w, value) => w.Write(value), r => r.ReadSingle());
        Register((w, value) => w.Write(value), r => r.ReadDouble());
        Register((w, value) => w.Write(value), r => r.ReadString());

        // byte[]
        Register((w, value) => { w.Write((ushort)value.Length); w.Write(value); },
                 r => r.ReadBytes(r.ReadUInt16()));
        // Vector2
        Register((w, value) => { w.Write(value.x); w.Write(value.y); },
                 r => new Vector2(r.ReadSingle(), r.ReadSingle()));
        // Color
        Register((w, value) => w.Write(value.ToRgba32()),
                 r => new Color(r.ReadInt32()));

        RegisterGenerator(new EnumDeSerializerGenerator());
        RegisterGenerator(new ArrayDeSerializerGenerator());
        RegisterGenerator(new CollectionDeSerializerGenerator());
        RegisterGenerator(new DictionaryDeSerializerGenerator());
        RegisterGenerator(new SyncedObjectDeSerializerGenerator());
    }

    public static void Register<T>(Action<BinaryWriter, T> serialize, Func<BinaryReader, T> deserialize)
        => Register(new SimpleDeSerializer<T>(serialize, deserialize));
    public static void Register<T>(IDeSerializer<T> deSerializer)
        => _byType.Add(typeof(T), deSerializer);
    public static void RegisterGenerator(IDeSerializerGenerator deSerializerGenerator)
        => _generators.Add(deSerializerGenerator);

    public static IDeSerializer<T> Get<T>(bool createIfMissing)
        => (IDeSerializer<T>)Get(typeof(T), createIfMissing);
    public static IDeSerializer Get(Type type, bool createIfMissing)
    {
        if (!_byType.TryGetValue(type, out var value)) {
            if (!createIfMissing) throw new InvalidOperationException(
                $"No DeSerializer for type {type} found");

            value = _generators.Select(g => g.GenerateFor(type)).FirstOrDefault(x => x != null);
            if (value == null) value = new ComplexDeSerializer(type);
            _byType.Add(type, value);
        }
        return value;
    }
}
