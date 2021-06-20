using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Godot;
using File = System.IO.File;

public class WorldSave
{
    public const string FILE_EXT    = ".yf5";
    public const int MAGIC_NUMBER   = 0x59463573; // "YF5s"
    public const int LATEST_VERSION = 1;

    public static readonly string WORLDS_DIR = OS.GetUserDataDir() + "/worlds/";


    public int Version { get; private set; } = LATEST_VERSION;
    public TimeSpan Playtime { get; set; } = TimeSpan.Zero;
    public DateTime LastSaved { get; private set; }

    public string Generator { get; private set; }
    public int Seed { get; private set; }

    public Dictionary<(int X, int Y), Dictionary<string, byte[]>> ChunkData { get; private set; }


    public static WorldSave ReadFromFile(string path)
    {
        var save = new WorldSave { LastSaved = File.GetLastAccessTime(path) };
        using (var stream = File.OpenRead(path)) {
            using (var reader = new BinaryReader(stream)) {
                var magic = reader.ReadInt32();
                if (magic != MAGIC_NUMBER) throw new IOException(
                    $"Magic number does not match ({magic:X8} != {MAGIC_NUMBER:X8})");

                // TODO: See how to better support multiple versions, improve saving/loading.
                save.Version  = reader.ReadUInt16();
                save.Playtime = TimeSpan.FromSeconds(reader.ReadUInt32());

                if (save.Version == 0) {
                    save.Seed      = unchecked((int)GD.Randi());
                    save.Generator = "Void";

                    var tempBlockLayers = new Dictionary<(int X, int Y), BlockLayer>();
                    var numBlocks = reader.ReadInt32();
                    for (var i = 0; i < numBlocks; i++) {
                        var blockPos    = new BlockPos(reader.ReadInt32(), reader.ReadInt32());
                        var rawColor    = reader.ReadInt32();
                        var unbreakable = reader.ReadBoolean(); // TODO

                        var chunkPos = blockPos.ToChunkPos();
                        if (!tempBlockLayers.TryGetValue(chunkPos, out var blocks))
                            tempBlockLayers.Add(chunkPos, blocks = new BlockLayer());
                        blocks[blockPos.GlobalToChunkRel()] = new BlockData(Block.DEFAULT, rawColor);
                    }
                    save.ChunkData = tempBlockLayers.ToDictionary(kvp => kvp.Key,
                        kvp => new Dictionary<string, byte[]> { [nameof(BlockLayer)] = kvp.Value.ToBytes() });
                } else if (save.Version == 1) {
                    save.Generator = reader.ReadString();
                    save.Seed      = reader.ReadInt32();

                    var numChunks  = reader.ReadInt32();
                    save.ChunkData = new Dictionary<(int X, int Y), Dictionary<string, byte[]>>();
                    for (var i = 0; i < numChunks; i++) {
                        var chunkPos = (reader.ReadInt32(), reader.ReadInt32());
                        var chunk    = new Dictionary<string, byte[]>();
                        save.ChunkData.Add(chunkPos, chunk);

                        var numLayers = reader.ReadByte();
                        for (var j = 0; j < numLayers; j++) {
                            var name  = reader.ReadString();
                            var count = reader.ReadInt32();
                            var data  = reader.ReadBytes(count);
                            chunk.Add(name, data);
                        }
                    }
                } else throw new IOException($"Version {save.Version} not supported (latest version: {LATEST_VERSION})");
            }
        }
        return save;
    }

    public void WriteToFile(string path)
    {
        using (var stream = File.OpenWrite(path + ".tmp")) {
            using (var writer = new BinaryWriter(stream)) {
                writer.Write(MAGIC_NUMBER);
                writer.Write((ushort)LATEST_VERSION);
                writer.Write((uint)Playtime.TotalSeconds);

                writer.Write(Generator);
                writer.Write(Seed);

                writer.Write(ChunkData.Count);
                foreach (var ((chunkX, chunkY), layers) in ChunkData) {
                    writer.Write(chunkX);
                    writer.Write(chunkY);
                    writer.Write((byte)layers.Count);
                    foreach (var (name, data) in layers) {
                        writer.Write(name);
                        writer.Write(data.Length);
                        writer.Write(data);
                    }
                }
            }
        }
        new Godot.Directory().Rename(path + ".tmp", path);
        LastSaved = File.GetLastWriteTime(path);
    }


    public void WriteDataFromWorld(World world)
    {
        Generator = world.Generator.Name;
        Seed      = world.Seed;

        ChunkData = world.Chunks.ToDictionary(
            chunk => chunk.ChunkPosition,
            chunk => chunk.Layers
                .Where(layer => !layer.IsDefault)
                .ToDictionary(layer => layer.GetType().Name, layer => layer.ToBytes()));
    }

    public void ReadDataIntoWorld(World world)
    {
        world.Generator = WorldGeneratorRegistry.GetOrNull(Generator);
        world.Seed      = Seed;

        RPC.Reliable(world.ClearChunks);
        foreach (var (chunkPos, layers) in ChunkData) {
            var chunk = world.GetOrCreateChunk(chunkPos);
            foreach (var (name, data) in layers)
                chunk.GetOrCreateLayer(name).FromBytes(data);
        }
    }
}
