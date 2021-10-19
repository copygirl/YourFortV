using System;
using System.Linq;
using MessagePack;
using MessagePack.Formatters;

public class ChunkFormatter : IMessagePackFormatter<Chunk>
{
    public Chunk Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var numElements = reader.ReadArrayHeader();
        if (numElements != 3) throw new Exception("Expected 3 elements");

        var chunkX = reader.ReadInt32();
        var chunkY = reader.ReadInt32();
        return new Chunk((chunkX, chunkY))
            { Layers = MessagePackSerializer.Deserialize<IChunkLayer[]>(ref reader) };
    }

    public void Serialize(ref MessagePackWriter writer, Chunk chunk, MessagePackSerializerOptions options)
    {
        writer.WriteArrayHeader(3);
        writer.Write(chunk.ChunkPos.X);
        writer.Write(chunk.ChunkPos.Y);
        MessagePackSerializer.Serialize(ref writer,
            chunk.Layers.Where(l => !l.IsDefault).ToArray());
    }
}
