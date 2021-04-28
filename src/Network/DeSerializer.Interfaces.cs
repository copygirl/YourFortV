using System;
using System.IO;

/// <summary>
/// Basic binary de/serializer interface, capable of de/serializing a particular
/// type that it was made for. Will typically not be implemented directly, as
/// <see cref="IDeSerializer{T}"/> offers a more type-safe interface.
/// </summary>
public interface IDeSerializer
{
    void Serialize(Game game, BinaryWriter writer, object value);
    object Deserialize(Game game, BinaryReader reader);
}

/// <summary>
/// Basic type-safe binary de/serializer interface, capable of de/serializing
/// values of type <c>T</c>. <see cref="DeSerializer{T}"/> offers an abstract
/// implementation that already implements <see cref="IDeSerializer"/> methods
/// so you only have to use the type-safe ones.
/// </summary>
public interface IDeSerializer<T>
    : IDeSerializer
{
    void Serialize(Game game, BinaryWriter writer, T value);
    new T Deserialize(Game game, BinaryReader reader);
}

// TODO: Using C# 8 this could be done with default interface implementations on IDeSerializer<>.
public abstract class DeSerializer<T>
    : IDeSerializer<T>
{
    public abstract void Serialize(Game game, BinaryWriter writer, T value);
    public abstract T Deserialize(Game game, BinaryReader reader);

    void IDeSerializer.Serialize(Game game, BinaryWriter writer, object value)
        => Serialize(game, writer, (T)value);
    object IDeSerializer.Deserialize(Game game, BinaryReader reader)
        => Deserialize(game, reader);
}

/// <summary>
/// This interface allows the dynamic creation of <see cref="IDeSerializer"/>
/// implementations that cannot be covered by simple de/serializer implementations,
/// such as when generics come into play.
/// </summary>
/// <seealso cref="EnumDeSerializerGenerator"/>
/// <seealso cref="ArrayDeSerializerGenerator"/>
/// <seealso cref="CollectionDeSerializerGenerator"/>
/// <seealso cref="DictionaryDeSerializerGenerator"/>
public interface IDeSerializerGenerator
{
    IDeSerializer GenerateFor(Type type);
}
