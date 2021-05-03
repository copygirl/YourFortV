using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using File = System.IO.File;

public class Save
{
    public const string FILE_EXT     = ".yf5";
    public const int MAGIC_NUMBER    = 0x59463573; // "YF5s"
    public const int CURRENT_VERSION = 0;

    public int Version { get; private set; }
    public DateTime LastSaved { get; private set; }
    public TimeSpan Playtime { get; set; }

    public List<(SaveObjectInfo, List<object>)> Objects { get; private set; }


    public static Save ReadFromFile(string path)
    {
        var save = new Save { LastSaved = File.GetLastAccessTime(path) };
        using (var stream = File.OpenRead(path)) {
            using (var reader = new BinaryReader(stream)) {
                var magic = reader.ReadInt32();
                if (magic != MAGIC_NUMBER) throw new IOException(
                    $"Magic number does not match ({magic:X8} != {MAGIC_NUMBER:X8})");

                // TODO: See how to support multiple versions.
                save.Version = reader.ReadUInt16();
                if (save.Version != CURRENT_VERSION) throw new IOException(
                    $"Version does not match ({save.Version} != {CURRENT_VERSION})");

                save.Playtime = TimeSpan.FromSeconds(reader.ReadUInt32());

                var numObjects = reader.ReadInt32();
                save.Objects = new List<(SaveObjectInfo, List<object>)>(numObjects);
                for (var i = 0; i < numObjects; i++) {
                    var hashID  = reader.ReadInt32();
                    var objInfo = SaveRegistry.GetOrThrow(hashID);
                    var props = objInfo.PropertiesByID.Select(x => x.DeSerializer.Deserialize(null, reader)).ToList();
                    save.Objects.Add((objInfo, props));
                }
            }
        }
        return save;
    }

    public void WriteToFile(string path)
    {
        using (var stream = File.OpenWrite(path)) {
            using (var writer = new BinaryWriter(stream)) {
                writer.Write(MAGIC_NUMBER);
                writer.Write((ushort)CURRENT_VERSION);
                writer.Write((uint)Playtime.TotalSeconds);

                writer.Write(Objects.Count);
                foreach (var (objInfo, props) in Objects) {
                    writer.Write(objInfo.HashID);
                    for (var i = 0; i < objInfo.PropertiesByID.Count; i++) {
                        var propInfo  = objInfo.PropertiesByID[i];
                        var propValue = props[i];
                        propInfo.DeSerializer.Serialize(null, writer, propValue);
                    }
                }
            }
        }
        LastSaved = File.GetLastAccessTime(path);
    }


    public static Save CreateFromWorld(Game game, TimeSpan playtime)
    {
        var save = new Save {
            Playtime = playtime,
            Objects  = new List<(SaveObjectInfo, List<object>)>(),
        };
        foreach (var (id, obj) in game.Objects) {
            var objInfo = SaveRegistry.GetOrNull(obj.GetType());
            if (objInfo == null) continue;

            var props = objInfo.PropertiesByID.Select(x => x.Get(obj)).ToList();
            save.Objects.Add((objInfo, props));
        }
        return save;
    }

    public void AddToWorld(Server server)
    {
        foreach (var (objInfo, props) in Objects) {
            var obj = objInfo.SpawnInfo.Scene.Init<Node>();
            server.GetNode("World").AddChild(obj, true);
            server.Objects.Add(null, obj);

            for (var i = 0; i < objInfo.PropertiesByID.Count; i++) {
                var propInfo  = objInfo.PropertiesByID[i];
                var propValue = props[i];
                propInfo.Set(obj, propValue);
            }
        }
    }
}
