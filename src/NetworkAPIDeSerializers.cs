using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

public interface INetworkDeSerializer
{
    void Serialize(BinaryWriter writer, object value);
    object Deserialize(BinaryReader reader);
}

public interface INetworkDeSerializerGenerator
{
    INetworkDeSerializer GenerateFor(Type type);
}

public partial class NetworkAPI
{
    private class SimpleDeSerializer<T>
        : INetworkDeSerializer
    {
        private readonly Action<BinaryWriter, T> _serialize;
        private readonly Func<BinaryReader, T> _deserialize;
        public SimpleDeSerializer(Action<BinaryWriter, T> serialize, Func<BinaryReader, T> deserialize)
            { _serialize = serialize; _deserialize = deserialize; }
        public void Serialize(BinaryWriter writer, object value) => _serialize(writer, (T)value);
        public object Deserialize(BinaryReader reader) => _deserialize(reader);
    }

    private class ArrayDeSerializerGenerator
        : INetworkDeSerializerGenerator
    {
        public INetworkDeSerializer GenerateFor(Type type)
        {
            if (!type.IsArray) return null;
            var deSerializerType = typeof(ArrayDeSerializer<>).MakeGenericType(type.GetElementType());
            return (INetworkDeSerializer)Activator.CreateInstance(deSerializerType);
        }
    }
    private class ArrayDeSerializer<T>
        : INetworkDeSerializer
    {
        private readonly INetworkDeSerializer _elementDeSerializer =
            Network.API.GetDeSerializer(typeof(T), true);

        public void Serialize(BinaryWriter writer, object value)
        {
            var array = (T[])value;
            writer.Write(array.Length);
            foreach (var element in array) _elementDeSerializer.Serialize(writer, element);
        }

        public object Deserialize(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            var array  = new T[length];
            for (var i = 0; i < length; i++)
                array[i] = (T)_elementDeSerializer.Deserialize(reader);
            return array;
        }
    }

    private class CollectionDeSerializerGenerator
        : INetworkDeSerializerGenerator
    {
        public INetworkDeSerializer GenerateFor(Type type)
        {
            Type elementType;
            if (type.IsInterface) {
                if (!type.IsGenericType) return null;
                elementType = type.GetGenericArguments()[0];
                var typeDef = type.GetGenericTypeDefinition();
                if      (typeDef == typeof(ICollection<>)) type = typeof(List<>).MakeGenericType(elementType);
                else if (typeDef == typeof(IList<>))       type = typeof(List<>).MakeGenericType(elementType);
                else if (typeDef == typeof(ISet<>))        type = typeof(HashSet<>).MakeGenericType(elementType);
                else return null;
            } else {
                if (type.GetConstructor(Type.EmptyTypes) == null) return null;
                elementType = type.GetInterfaces()
                    .Where(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(ICollection<>)))
                    .Select(i => i.GetGenericArguments()[0])
                    .FirstOrDefault();
                if (elementType == null) return null;
            }
            var deSerializerType = typeof(CollectionDeSerializer<,>).MakeGenericType(type, elementType);
            return (INetworkDeSerializer)Activator.CreateInstance(deSerializerType);
        }
    }
    private class CollectionDeSerializer<TCollection, TElement>
            : INetworkDeSerializer
        where TCollection : ICollection<TElement>, new()
    {
        private readonly INetworkDeSerializer _elementDeSerializer =
            Network.API.GetDeSerializer(typeof(TElement), true);

        public void Serialize(BinaryWriter writer, object value)
        {
            var collection = (TCollection)value;
            writer.Write(collection.Count);
            foreach (var element in collection)
                _elementDeSerializer.Serialize(writer, element);
        }

        public object Deserialize(BinaryReader reader)
        {
            var count = reader.ReadInt32();
            var collection = new TCollection();
            for (var i = 0; i < count; i++)
                collection.Add((TElement)_elementDeSerializer.Deserialize(reader));
            return collection;
        }
    }

    private class DictionaryDeSerializerGenerator
        : INetworkDeSerializerGenerator
    {
        public INetworkDeSerializer GenerateFor(Type type)
        {
            Type keyType, valueType;
            if (type.IsInterface) {
                if (!type.IsGenericType || (type.GetGenericTypeDefinition() != typeof(IDictionary<,>))) return null;
                keyType   = type.GetGenericArguments()[0];
                valueType = type.GetGenericArguments()[1];
                type = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
            } else {
                if (type.GetConstructor(Type.EmptyTypes) == null) return null;
                (keyType, valueType) = type.GetInterfaces()
                    .Where(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
                    .Select(i => (i.GetGenericArguments()[0], i.GetGenericArguments()[1]))
                    .FirstOrDefault();
                if (keyType == null) return null;
            }
            var deSerializerType = typeof(DictionaryDeSerializer<,,>).MakeGenericType(type, keyType, valueType);
            return (INetworkDeSerializer)Activator.CreateInstance(deSerializerType);
        }
    }
    private class DictionaryDeSerializer<TDictionary, TKey, TValue>
            : INetworkDeSerializer
        where TDictionary : IDictionary<TKey, TValue>, new()
    {
        private readonly INetworkDeSerializer _keyDeSerializer =
            Network.API.GetDeSerializer(typeof(TKey), true);
        private readonly INetworkDeSerializer _valueDeSerializer =
            Network.API.GetDeSerializer(typeof(TKey), true);

        public void Serialize(BinaryWriter writer, object value)
        {
            var dictionary = (TDictionary)value;
            writer.Write(dictionary.Count);
            foreach (var element in dictionary) {
                _keyDeSerializer.Serialize(writer, element.Key);
                _valueDeSerializer.Serialize(writer, element.Value);
            }
        }

        public object Deserialize(BinaryReader reader)
        {
            var count = reader.ReadInt32();
            var dictionary = new TDictionary();
            for (var i = 0; i < count; i++)
                dictionary.Add((TKey)_keyDeSerializer.Deserialize(reader),
                               (TValue)_valueDeSerializer.Deserialize(reader));
            return dictionary;
        }
    }

    // TODO: Replace this with something that will generate code at runtime for improved performance.
    private class ComplexDeSerializer
        : INetworkDeSerializer
    {
        private readonly Type _type;
        private event Action<BinaryWriter, object> OnSerialize;
        private event Action<BinaryReader, object> OnDeserialize;

        public ComplexDeSerializer(Type type)
        {
            _type = type;
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                var deSerializer = Network.API.GetDeSerializer(field.FieldType, true);
                OnSerialize += (writer, value) => deSerializer.Serialize(writer, field.GetValue(value));
                OnDeserialize += (reader, instance) => field.SetValue(instance, deSerializer.Deserialize(reader));
            }
            if (OnSerialize == null) throw new InvalidOperationException(
                $"Unable to create serializer for type {type}");
        }

        public void Serialize(BinaryWriter writer, object value)
            => OnSerialize(writer, value);
        public object Deserialize(BinaryReader reader)
        {
            var instance = FormatterServices.GetUninitializedObject(_type);
            OnDeserialize(reader, instance);
            return instance;
        }
    }
}
