using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Godot;

// TODO: Allow syncronization of child objects spawned with their parent objects.
// TODO: Specify who properties are syncronized with. (Owner, Friends, Team, Everyone)
public abstract class Sync
{
    protected Game Game { get; }
    protected Dictionary<UniqueID, SyncStatus> StatusByID { get; } = new Dictionary<UniqueID, SyncStatus>();
    protected Dictionary<Node, SyncStatus> StatusByObject { get; } = new Dictionary<Node, SyncStatus>();

    static Sync()
        => DeSerializerRegistry.Register(new SyncPacketObjectDeSerializer());

    public Sync(Game game)
    {
        Game = game;
        Game.Objects.Added   += OnObjectAdded;
        Game.Objects.Removed += OnObjectRemoved;
        Game.Objects.Cleared += OnObjectsCleared;
    }

    private void OnObjectAdded(UniqueID id, Node obj)
    {
        var info = SyncRegistry.GetOrNull(obj.GetType());
        if (info == null) return;

        var status = new SyncStatus(id, obj, info);
        StatusByID.Add(id, status);
        StatusByObject.Add(obj, status);
        OnSyncedAdded(status);
    }

    private void OnObjectRemoved(UniqueID id, Node obj)
    {
        if (!StatusByObject.TryGetValue(obj, out var status)) return;

        StatusByID.Remove(status.ID);
        StatusByObject.Remove(status.Object);
        OnSyncedRemoved(status);
    }

    protected virtual void OnSyncedAdded(SyncStatus status) {  }
    protected virtual void OnSyncedRemoved(SyncStatus status) {  }

    private void OnObjectsCleared()
    {
        StatusByID.Clear();
        StatusByObject.Clear();
    }


    public SyncStatus GetStatusOrNull(UniqueID id)
        => StatusByID.TryGetValue(id, out var value) ? value : null;
    public SyncStatus GetStatusOrThrow(UniqueID id)
        => GetStatusOrNull(id) ?? throw new Exception(
            $"No {nameof(SyncStatus)} found for ID {id}");

    public SyncStatus GetStatusOrNull(Node obj)
    {
        if (obj.GetType().GetCustomAttribute<SyncAttribute>() == null)
            throw new ArgumentException($"Type {obj.GetType()} is missing {nameof(SyncAttribute)}");
        return StatusByObject.TryGetValue(obj, out var value) ? value : null;
    }
    public SyncStatus GetStatusOrThrow(Node obj)
        => GetStatusOrNull(obj) ?? throw new Exception(
            $"No {nameof(SyncStatus)} found for '{obj.Name}' ({obj.GetType()})");
}


public class SyncStatus
{
    public UniqueID ID { get; }
    public Node Object { get; }
    public SyncObjectInfo Info { get; }

    public int DirtyProperties { get; set; }
    public SyncMode Mode { get; set; }

    public SyncStatus(UniqueID id, Node obj, SyncObjectInfo info)
        { ID = id; Object = obj; Info = info; }
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
        public UniqueID ID { get; }
        public SyncMode Mode { get; }
        public List<(byte, object)> Values { get; }
        public Object(ushort infoID, UniqueID id, SyncMode mode, List<(byte, object)> values)
            { InfoID = infoID; ID = id; Mode = mode; Values = values; }
    }
}

internal class SyncPacketObjectDeSerializer
    : DeSerializer<SyncPacket.Object>
{
    public override void Serialize(Game game, BinaryWriter writer, SyncPacket.Object value)
    {
        writer.Write(value.InfoID);
        writer.Write(value.ID.Value);
        writer.Write((byte)value.Mode);
        writer.Write((byte)value.Values.Count);

        var objInfo = SyncRegistry.GetOrThrow(value.InfoID);
        foreach (var (propID, val) in value.Values) {
            writer.Write(propID);
            var propInfo = objInfo.PropertiesByID[propID];
            propInfo.DeSerializer.Serialize(game, writer, val);
        }
    }

    public override SyncPacket.Object Deserialize(Game game, BinaryReader reader)
    {
        var infoID = reader.ReadUInt16();
        var id     = new UniqueID(reader.ReadUInt32());
        var mode   = (SyncMode)reader.ReadByte();
        var count  = reader.ReadByte();

        var objInfo = SyncRegistry.GetOrThrow(infoID);
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
            values.Add((propID, propInfo.DeSerializer.Deserialize(game, reader)));
        }

        return new SyncPacket.Object(infoID, id, mode, values);
    }
}
