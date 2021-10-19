using System.Buffers;
using MessagePack;
using MessagePack.Formatters;
using Nerdbank.Streams;

public interface IDeSerializable
{
    void Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options);

    void Serialize(ref MessagePackWriter writer, MessagePackSerializerOptions options);
}

public static class DeSerializableExtensions
{
    public static void Deserialize(this IDeSerializable value, byte[] data, MessagePackSerializerOptions options = null)
    {
        options = options ?? MessagePackSerializerOptions.Standard;
        var reader = new MessagePackReader(data);
        value.Deserialize(ref reader, options);
    }

    public static byte[] SerializeToBytes(this IDeSerializable value, MessagePackSerializerOptions options = null)
    {
        options = options ?? MessagePackSerializerOptions.Standard;
        var sequence = new Sequence<byte>();
        var writer   = new MessagePackWriter(sequence);
        value.Serialize(ref writer, options);
        writer.Flush();
        return sequence.AsReadOnlySequence.ToArray();
    }
}

public class DeSerializableFormatter<T> : IMessagePackFormatter<T>
    where T : IDeSerializable, new()
{
    public T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var value = new T();
        value.Deserialize(ref reader, options);
        return value;
    }

    public void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options)
    {
        value.Serialize(ref writer, options);
    }
}
