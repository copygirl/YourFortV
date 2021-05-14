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
    public const int LATEST_VERSION = 0;

    public static readonly string WORLDS_DIR = OS.GetUserDataDir() + "/worlds/";


    public DateTime LastSaved { get; private set; }
    public int Version { get; private set; } = LATEST_VERSION;
    public TimeSpan Playtime { get; set; } = TimeSpan.Zero;

    public List<(BlockPos, Color, bool)> Blocks { get; private set; }


    public static WorldSave ReadFromFile(string path)
    {
        var save = new WorldSave { LastSaved = File.GetLastAccessTime(path) };
        using (var stream = File.OpenRead(path)) {
            using (var reader = new BinaryReader(stream)) {
                var magic = reader.ReadInt32();
                if (magic != MAGIC_NUMBER) throw new IOException(
                    $"Magic number does not match ({magic:X8} != {MAGIC_NUMBER:X8})");

                // TODO: See how to support multiple versions.
                save.Version = reader.ReadUInt16();
                if (save.Version != LATEST_VERSION) throw new IOException(
                    $"Version does not match ({save.Version} != {LATEST_VERSION})");

                save.Playtime = TimeSpan.FromSeconds(reader.ReadUInt32());

                var numBlocks = reader.ReadInt32();
                save.Blocks   = new List<(BlockPos, Color, bool)>();
                for (var i = 0; i < numBlocks; i++)
                    save.Blocks.Add((new BlockPos(reader.ReadInt32(), reader.ReadInt32()),
                                     new Color(reader.ReadInt32()),
                                     reader.ReadBoolean()));
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

                writer.Write(Blocks.Count);
                foreach (var (position, color, unbreakable) in Blocks) {
                    writer.Write(position.X);
                    writer.Write(position.Y);
                    writer.Write(color.ToRgba32());
                    writer.Write(unbreakable);
                }
            }
        }
        new Godot.Directory().Rename(path + ".tmp", path);
        LastSaved = File.GetLastWriteTime(path);
    }


    public void WriteDataFromWorld(World world)
        => Blocks = world.Blocks.Select(block => (block.Position, block.Color, block.Unbreakable)).ToList();

    public void ReadDataIntoWorld(World world)
    {
        RPC.Reliable(world.ClearBlocks);
        foreach (var (position, color, unbreakable) in Blocks)
            RPC.Reliable(world.SpawnBlock, position.X, position.Y, color, unbreakable);
    }
}
