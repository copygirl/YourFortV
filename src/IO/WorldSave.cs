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


    public DateTime LastSaved { get; private set; }
    public int Version { get; private set; } = LATEST_VERSION;
    public TimeSpan Playtime { get; set; } = TimeSpan.Zero;

    public Dictionary<(int, int), Dictionary<BlockPos, (Color, bool)>> Chunks { get; private set; }


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
                    save.Chunks   = new Dictionary<(int, int), Dictionary<BlockPos, (Color, bool)>>();
                    var numBlocks = reader.ReadInt32();
                    for (var i = 0; i < numBlocks; i++) {
                        var blockPos  = new BlockPos(reader.ReadInt32(), reader.ReadInt32());
                        var blockData = (new Color(reader.ReadInt32()), reader.ReadBoolean());
                        var chunkPos  = blockPos.ToChunkPos();
                        if (!save.Chunks.TryGetValue(chunkPos, out var blocks))
                            save.Chunks.Add(chunkPos, blocks = new Dictionary<BlockPos, (Color, bool)>());
                        blocks.Add(blockPos.GlobalToChunkRel(), blockData);
                    }
                } else if (save.Version == 1) {
                    var numChunks = reader.ReadInt32();
                    save.Chunks   = new Dictionary<(int, int), Dictionary<BlockPos, (Color, bool)>>(numChunks);
                    for (var i = 0; i < numChunks; i++) {
                        var chunkPos  = (reader.ReadInt32(), reader.ReadInt32());
                        var numBlocks = (int)reader.ReadUInt16();
                        var blocks    = new Dictionary<BlockPos, (Color, bool)>(numBlocks);
                        for (var j = 0; j < numBlocks; j++)
                            blocks.Add(new BlockPos(reader.ReadByte(), reader.ReadByte()),
                                       (new Color(reader.ReadInt32()), reader.ReadBoolean()));
                        save.Chunks.Add(chunkPos, blocks);
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

                writer.Write(Chunks.Count);
                foreach (var ((chunkX, chunkY), blocks) in Chunks) {
                    writer.Write(chunkX);
                    writer.Write(chunkY);
                    writer.Write((ushort)blocks.Count);
                    foreach (var ((blockX, blockY), (color, unbreakable)) in blocks) {
                        writer.Write((byte)blockX);
                        writer.Write((byte)blockY);
                        writer.Write(color.ToRgba32());
                        writer.Write(unbreakable);
                    }
                }
            }
        }
        new Godot.Directory().Rename(path + ".tmp", path);
        LastSaved = File.GetLastWriteTime(path);
    }


    public void WriteDataFromWorld(World world)
        => Chunks = world.Chunks.ToDictionary(
            chunk => chunk.ChunkPosition,
            chunk => chunk.GetLayerOrNull<Block>()
                .GetChildren<Block>().ToDictionary(
                    block => block.ChunkLocalBlockPos,
                    block => (block.Color, block.Unbreakable)));

    public void ReadDataIntoWorld(World world)
    {
        RPC.Reliable(world.ClearChunks);
        foreach (var (chunkPos, blocks) in Chunks) {
            foreach (var (blockPos, (color, unbreakable)) in blocks) {
                var (x, y) = blockPos.ChunkRelToGlobal(chunkPos);
                world.SpawnBlock(x, y, color, unbreakable);
            }
        }
    }
}
