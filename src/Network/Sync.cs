using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Godot;

// TODO: Allow syncronization of child objects spawned with their parent objects.
// TODO: Specify who properties are syncronized with. (Owner, Friends, Team, Everyone)
public class Sync
{
    protected Game Game { get; }
    protected Dictionary<uint, SyncStatus> StatusBySyncID { get; } = new Dictionary<uint, SyncStatus>();
    protected Dictionary<Node, SyncStatus> StatusByObject { get; } = new Dictionary<Node, SyncStatus>();

    static Sync() => DeSerializerRegistry.Register(new SyncPacketObjectDeSerializer());
    public Sync(Game game) => Game = game;

    public SyncStatus GetStatusOrNull(uint syncID)
        => StatusBySyncID.TryGetValue(syncID, out var value) ? value : null;
    public SyncStatus GetStatusOrThrow(uint syncID)
        => GetStatusOrNull(syncID) ?? throw new Exception(
            $"No {nameof(SyncStatus)} found for ID {syncID}");

    public SyncStatus GetStatusOrNull(Node obj)
    {
        if (obj.GetType().GetCustomAttribute<SyncObjectAttribute>() == null)
            throw new ArgumentException($"Type {obj.GetType()} is missing {nameof(SyncObjectAttribute)}");
        return StatusByObject.TryGetValue(obj, out var value) ? value : null;
    }
    public SyncStatus GetStatusOrThrow(Node obj)
        => GetStatusOrNull(obj) ?? throw new Exception(
            $"No {nameof(SyncStatus)} found for '{obj.Name}' ({obj.GetType()})");

    public virtual void Clear()
    {
        foreach (var (node, _) in StatusByObject) {
            if (!Godot.Object.IsInstanceValid(node)) continue;
            node.GetParent().RemoveChild(node);
            node.QueueFree();
        }

        StatusByObject.Clear();
        StatusBySyncID.Clear();
    }
}


public class SyncStatus
{
    public uint SyncID { get; }
    public Node Object { get; }
    public SyncObjectInfo Info { get; }

    public int DirtyProperties { get; set; }
    public SyncMode Mode { get; set; }

    public SyncStatus(uint syncID, Node obj, SyncObjectInfo info)
        { SyncID = syncID; Object = obj; Info = info; }
}

public enum SyncMode
{
    Default,
    Spawn,
    Destroy,
}


public class SyncPacket
{
    public List<Object> Changes { get; } = new List<Object>();

    public class Object
    {
        public ushort InfoID { get; }
        public uint SyncID { get; }
        public SyncMode Mode { get; }
        public List<(byte, object)> Values { get; }
        public Object(ushort infoID, uint syncID, SyncMode mode, List<(byte, object)> values)
            { InfoID = infoID; SyncID = syncID; Mode = mode; Values = values; }
    }
}

internal class SyncPacketObjectDeSerializer
    : DeSerializer<SyncPacket.Object>
{
    public override void Serialize(Game game, BinaryWriter writer, SyncPacket.Object value)
    {
        writer.Write(value.InfoID);
        writer.Write(value.SyncID);
        writer.Write((byte)value.Mode);
        writer.Write((byte)value.Values.Count);

        var objInfo = SyncRegistry.Get(value.InfoID);
        foreach (var (propID, val) in value.Values) {
            writer.Write(propID);
            var propInfo = objInfo.PropertiesByID[propID];
            var deSerializer = DeSerializerRegistry.Get(propInfo.Type, false);
            deSerializer.Serialize(game, writer, val);
        }
    }

    public override SyncPacket.Object Deserialize(Game game, BinaryReader reader)
    {
        var infoID = reader.ReadUInt16();
        var syncID = reader.ReadUInt32();
        var mode   = (SyncMode)reader.ReadByte();
        var count  = reader.ReadByte();

        var objInfo = SyncRegistry.Get(infoID);
        if (count > objInfo.PropertiesByID.Count) throw new Exception(
            $"Count is higher than possible number of changes");

        var values = new List<(byte, object)>(count);
        var duplicateCheck = new HashSet<byte>();
        for (var i = 0; i < count; i++) {
            var propID = reader.ReadByte();
            if (propID >= objInfo.PropertiesByID.Count) throw new Exception(
                $"No sync property with ID {propID} on {objInfo.Name}");
            var propInfo = objInfo.PropertiesByID[propID];
            if (!duplicateCheck.Add(propID)) throw new Exception(
                $"Duplicate entry for property {propInfo.Name}");
            var deSerializer = DeSerializerRegistry.Get(propInfo.Type, false);
            values.Add((propID, deSerializer.Deserialize(game, reader)));
        }

        return new SyncPacket.Object(infoID, syncID, mode, values);
    }
}
