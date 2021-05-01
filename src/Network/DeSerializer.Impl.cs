using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Godot;

/// <summary>
/// Implements a simple de/serializer based on a serialize and deserialize
/// function, typically specified using short lambda expressions.
/// A shortcut method for creating an instance of this class can be found at:
/// <see cref="DeSerializerRegistry.Register{T}(Action{BinaryWriter, T}, Func{BinaryReader, T})"/>.
/// </summary>
public class SimpleDeSerializer<T>
    : DeSerializer<T>
{
    private readonly Action<BinaryWriter, T> _serialize;
    private readonly Func<BinaryReader, T> _deserialize;
    public SimpleDeSerializer(Action<BinaryWriter, T> serialize, Func<BinaryReader, T> deserialize)
        { _serialize = serialize; _deserialize = deserialize; }
    public override void Serialize(Game game, BinaryWriter writer, T value) => _serialize(writer, value);
    public override T Deserialize(Game game, BinaryReader reader) => _deserialize(reader);
}

public class EnumDeSerializerGenerator
    : IDeSerializerGenerator
{
    public IDeSerializer GenerateFor(Type type)
    {
        // TODO: Flagged enums are not supported at this time.
        if (!type.IsEnum || (type.GetCustomAttribute<FlagsAttribute>() != null)) return null;
        var deSerializerType = typeof(EnumDeSerializer<,>).MakeGenericType(type, type.GetEnumUnderlyingType());
        return (IDeSerializer)Activator.CreateInstance(deSerializerType);
    }

    private class EnumDeSerializer<TEnum, TUnderlying>
            : DeSerializer<TEnum>
        where TEnum : Enum
    {
        private readonly IDeSerializer<TUnderlying> _underlyingDeSerializer =
            DeSerializerRegistry.Get<TUnderlying>(true);

        public override void Serialize(Game game, BinaryWriter writer, TEnum value)
        {
            if (!Enum.IsDefined(typeof(TEnum), value)) throw new ArgumentException(
                $"Invalid enum value {value} for type {typeof(TEnum)}", nameof(value));
            _underlyingDeSerializer.Serialize(game, writer, (TUnderlying)(object)value);
        }

        public override TEnum Deserialize(Game game, BinaryReader reader)
        {
            var value = (TEnum)(object)_underlyingDeSerializer.Deserialize(game, reader);
            if (!Enum.IsDefined(typeof(TEnum), value)) throw new ArgumentException(
                $"Invalid enum value {value} for type {typeof(TEnum)}", nameof(value));
            return value;
        }
    }
}

public class ArrayDeSerializerGenerator
    : IDeSerializerGenerator
{
    public IDeSerializer GenerateFor(Type type)
    {
        if (!type.IsArray) return null;
        var deSerializerType = typeof(ArrayDeSerializer<>).MakeGenericType(type.GetElementType());
        return (IDeSerializer)Activator.CreateInstance(deSerializerType);
    }

    private class ArrayDeSerializer<T>
        : DeSerializer<T[]>
    {
        private readonly IDeSerializer _elementDeSerializer =
            DeSerializerRegistry.Get<T>(true);

        public override void Serialize(Game game, BinaryWriter writer, T[] value)
        {
            writer.Write(value.Length);
            foreach (var element in value)
                _elementDeSerializer.Serialize(game, writer, element);
        }

        public override T[] Deserialize(Game game, BinaryReader reader)
        {
            var length = reader.ReadInt32();
            var array  = new T[length];
            for (var i = 0; i < length; i++)
                array[i] = (T)_elementDeSerializer.Deserialize(game, reader);
            return array;
        }
    }
}

public class CollectionDeSerializerGenerator
    : IDeSerializerGenerator
{
    public IDeSerializer GenerateFor(Type type)
    {
        Type elementType;
        if (type.IsInterface) {
            // If the type is an interface type, specific interfaces are
            // supported and will be populated with certain concrete types.
            if (!type.IsGenericType) return null;
            elementType = type.GetGenericArguments()[0];
            var typeDef = type.GetGenericTypeDefinition();
            if      (typeDef == typeof(ICollection<>)) type = typeof(List<>).MakeGenericType(elementType);
            else if (typeDef == typeof(IList<>))       type = typeof(List<>).MakeGenericType(elementType);
            else if (typeDef == typeof(ISet<>))        type = typeof(HashSet<>).MakeGenericType(elementType);
            else return null;
        } else {
            // An empty constructor is required.
            if (type.GetConstructor(Type.EmptyTypes) == null) return null;
            // Dictionaries are handled by DictionaryDeSerializerGenerator.
            if (typeof(IDictionary).IsAssignableFrom(type)) return null;

            elementType = type.GetInterfaces()
                .Where(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(ICollection<>)))
                .Select(i => i.GetGenericArguments()[0])
                .FirstOrDefault();
            if (elementType == null) return null;
        }
        var deSerializerType = typeof(CollectionDeSerializer<,>).MakeGenericType(type, elementType);
        return (IDeSerializer)Activator.CreateInstance(deSerializerType);
    }

    private class CollectionDeSerializer<TCollection, TElement>
            : DeSerializer<TCollection>
        where TCollection : ICollection<TElement>, new()
    {
        private readonly IDeSerializer _elementDeSerializer =
            DeSerializerRegistry.Get<TElement>(true);

        public override void Serialize(Game game, BinaryWriter writer, TCollection value)
        {
            writer.Write(value.Count);
            foreach (var element in value)
                _elementDeSerializer.Serialize(game, writer, element);
        }

        public override TCollection Deserialize(Game game, BinaryReader reader)
        {
            var count = reader.ReadInt32();
            var collection = new TCollection();
            for (var i = 0; i < count; i++)
                collection.Add((TElement)_elementDeSerializer.Deserialize(game, reader));
            return collection;
        }
    }
}

public class DictionaryDeSerializerGenerator
    : IDeSerializerGenerator
{
    public IDeSerializer GenerateFor(Type type)
    {
        Type keyType, valueType;
        if (type.IsInterface) {
            if (!type.IsGenericType || (type.GetGenericTypeDefinition() != typeof(IDictionary<,>))) return null;
            keyType   = type.GetGenericArguments()[0];
            valueType = type.GetGenericArguments()[1];
            type = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
        } else {
            // An empty constructor is required.
            if (type.GetConstructor(Type.EmptyTypes) == null) return null;

            (keyType, valueType) = type.GetInterfaces()
                .Where(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
                .Select(i => (i.GetGenericArguments()[0], i.GetGenericArguments()[1]))
                .FirstOrDefault();
            if (keyType == null) return null;
        }
        var deSerializerType = typeof(DictionaryDeSerializer<,,>).MakeGenericType(type, keyType, valueType);
        return (IDeSerializer)Activator.CreateInstance(deSerializerType);
    }

    private class DictionaryDeSerializer<TDictionary, TKey, TValue>
            : DeSerializer<TDictionary>
        where TDictionary : IDictionary<TKey, TValue>, new()
    {
        private readonly IDeSerializer _keyDeSerializer =
            DeSerializerRegistry.Get<TKey>(true);
        private readonly IDeSerializer _valueDeSerializer =
            DeSerializerRegistry.Get<TKey>(true);

        public override void Serialize(Game game, BinaryWriter writer, TDictionary dict)
        {
            writer.Write(dict.Count);
            foreach (var (key, value) in dict) {
                _keyDeSerializer.Serialize(game, writer, key);
                _valueDeSerializer.Serialize(game, writer, value);
            }
        }

        public override TDictionary Deserialize(Game game, BinaryReader reader)
        {
            var count = reader.ReadInt32();
            var dictionary = new TDictionary();
            for (var i = 0; i < count; i++)
                dictionary.Add((TKey)_keyDeSerializer.Deserialize(game, reader),
                                (TValue)_valueDeSerializer.Deserialize(game, reader));
            return dictionary;
        }
    }
}

public class SyncedObjectDeSerializerGenerator
    : IDeSerializerGenerator
{
    public IDeSerializer GenerateFor(Type type)
    {
        if (!typeof(Node).IsAssignableFrom(type) || (type.GetCustomAttribute<SyncObjectAttribute>() == null)) return null;
        var deSerializerType = typeof(SyncedObjectDeSerializer<>).MakeGenericType(type);
        return (IDeSerializer)Activator.CreateInstance(deSerializerType);
    }

    private class SyncedObjectDeSerializer<TObj>
            : DeSerializer<TObj>
        where TObj : Node
    {
        public override void Serialize(Game game, BinaryWriter writer, TObj value)
            => writer.Write(game.Sync.GetStatusOrThrow(value).SyncID);
        public override TObj Deserialize(Game game, BinaryReader reader)
        {
            var id    = reader.ReadUInt32();
            var value = (TObj)game.Sync.GetStatusOrThrow(id).Object;
            if (value == null) throw new Exception($"Could not find synced object of type {typeof(TObj)} with ID {id}");
            return value;
        }
    }
}

// TODO: Replace this with something that will generate code at runtime for improved performance.
public class ComplexDeSerializer
    : IDeSerializer
{
    private readonly Type _type;
    private event Action<Game, BinaryWriter, object> OnSerialize;
    private event Action<Game, BinaryReader, object> OnDeserialize;

    public ComplexDeSerializer(Type type)
    {
        _type = type;
        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
            var deSerializer = DeSerializerRegistry.Get(field.FieldType, true);
            OnSerialize += (game, writer, value) => deSerializer.Serialize(game, writer, field.GetValue(value));
            OnDeserialize += (game, reader, instance) => field.SetValue(instance, deSerializer.Deserialize(game, reader));
        }
        if (OnSerialize == null) throw new InvalidOperationException(
            $"Unable to create serializer for type {type}");
    }

    public void Serialize(Game game, BinaryWriter writer, object value)
        => OnSerialize(game, writer, value);
    public object Deserialize(Game game, BinaryReader reader)
    {
        var instance = FormatterServices.GetUninitializedObject(_type);
        OnDeserialize(game, reader, instance);
        return instance;
    }
}
