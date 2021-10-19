using System;
using MessagePack;

// [MessagePackFormatter(typeof(DeSerializableFormatter<World>))]
public partial class World : IDeSerializable
{
    public void Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        // Restore defaults.
        Playtime  = TimeSpan.Zero;
        Seed      = 0;
        Generator = WorldGeneratorRegistry.GetOrNull("Simple");
        ChunkContainer.ClearChildren();

        var numKeys = reader.ReadMapHeader();
        for (var keyIndex = 0; keyIndex < numKeys; keyIndex++) {
            var key = reader.ReadString();
            switch (key) {
                case nameof(Playtime):
                    Playtime = TimeSpan.FromMilliseconds(reader.ReadUInt64());
                    break;
                case nameof(Seed):
                    Seed = reader.ReadInt32();
                    break;
                case nameof(Generator):
                    Generator = WorldGeneratorRegistry.GetOrNull(reader.ReadString()) ?? Generator;
                    break;
                case nameof(Chunks):
                    ChunkContainer.AddRange(MessagePackSerializer.Deserialize<Chunk[]>(ref reader));
                    break;
            }
        }
    }

    public void Serialize(ref MessagePackWriter writer, MessagePackSerializerOptions options)
    {
        writer.WriteMapHeader(4);

        writer.Write(nameof(Playtime));
        writer.Write((ulong)Playtime.TotalMilliseconds);

        writer.Write(nameof(Seed));
        writer.Write(Seed);

        writer.Write(nameof(Generator));
        writer.Write(Generator.Name);

        writer.Write(nameof(Chunks));
        writer.WriteArrayHeader(ChunkContainer.GetChildCount());
        foreach (var chunk in Chunks) MessagePackSerializer.Serialize(ref writer, chunk, options);
    }
}
