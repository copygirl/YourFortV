using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;

// TODO: Allow syncronization of child objects spawned with their parent objects.
// TODO: Specify who properties are syncronized with. (Owner, Friends, Team, Everyone)
public static class NetworkSync
{
    private static readonly List<SyncObjectInfo> _infoByID = new List<SyncObjectInfo>();
    private static readonly Dictionary<Type, SyncObjectInfo> _infoByType = new Dictionary<Type, SyncObjectInfo>();

    // TODO: Rework NetworkSync to be an instance on the Game object.
    private static readonly Dictionary<uint, SyncStatus> _serverStatusBySyncID = new Dictionary<uint, SyncStatus>();
    private static readonly Dictionary<Node, SyncStatus> _serverStatusByObject = new Dictionary<Node, SyncStatus>();
    private static readonly Dictionary<uint, SyncStatus> _clientStatusBySyncID = new Dictionary<uint, SyncStatus>();
    private static readonly Dictionary<Node, SyncStatus> _clientStatusByObject = new Dictionary<Node, SyncStatus>();
    private static readonly HashSet<SyncStatus> _dirtyObjects = new HashSet<SyncStatus>();
    private static uint _syncIDCounter = 1;

    static NetworkSync()
    {
        DiscoverSyncableObjects();
        RegisterPackets();
    }


    public static T Spawn<T>(this Server server)
        where T : Node
    {
        if (!_infoByType.TryGetValue(typeof(T), out var info)) throw new ArgumentException(
            $"No {nameof(SyncObjectInfo)} found for type {typeof(T)} (missing {nameof(SyncObjectAttribute)}?)", nameof(T));

        var obj    = info.InstanceScene.Init<T>();
        var status = new SyncStatus(_syncIDCounter++, obj, info){ Special = Special.Spawn };
        _serverStatusBySyncID.Add(status.SyncID, status);
        _serverStatusByObject.Add(status.Object, status);
        _dirtyObjects.Add(status);
        server.GetNode(info.ContainerNodePath).AddChild(obj);

        return obj;
    }

    public static void Destroy(this Node obj)
    {
        var status = GetSyncStatus(obj);
        if (!(obj.GetGame() is Server)) return;

        status.Special = Special.Destroy;
        _serverStatusBySyncID.Remove(status.SyncID);
        _serverStatusByObject.Remove(status.Object);
        _dirtyObjects.Add(status);

        obj.GetParent().RemoveChild(obj);
        obj.QueueFree();
    }

    public static TValue SetSync<TObject, TValue>(this TObject obj, TValue value,
                                                  [CallerMemberName] string property = null)
            where TObject : Node
        { MarkDirty(obj, property); return value; }
    private static void MarkDirty(Node obj, string property)
    {
        var status = GetSyncStatus(obj);
        if (!status.Info.PropertiesByName.TryGetValue(property, out var propInfo)) throw new ArgumentException(
            $"No {nameof(SyncPropertyInfo)} found for {obj.GetType()}.{property} (missing {nameof(SyncPropertyAttribute)}?)", nameof(property));
        if (!(obj.GetGame() is Server)) return;

        status.DirtyProperties |= 1 << propInfo.ID;
        _dirtyObjects.Add(status);
    }


    internal static void ProcessDirty(Server server)
    {
        if (_dirtyObjects.Count == 0) return;

        var packet = new SyncPacket();
        foreach (var status in _dirtyObjects) {
            var values = new List<(byte, object)>();
            foreach (var prop in status.Info.PropertiesByID)
                if ((status.DirtyProperties & (1 << prop.ID)) != 0)
                    values.Add((prop.ID, prop.Getter(status.Object)));
            packet.Changes.Add(new SyncPacket.Object(status.Info.ID, status.SyncID, status.Special, values));
            // If the object has been newly spawned, now is the time to remove the "Spawn" flag.
            if (status.Special == Special.Spawn) status.Special = Special.None;
        }
        // TODO: Need a different way to send packages to all *properly* connected peers.
        NetworkPackets.Send(server, server.CustomMultiplayer.GetNetworkConnectedPeers().Select(id => new NetworkID(id)), packet);

        _dirtyObjects.Clear();
    }

    internal static void SendAllObjects(Server server, NetworkID networkID)
    {
        var packet = new SyncPacket();
        foreach (var status in _serverStatusByObject.Values) {
            var values = new List<(byte, object)>();
            foreach (var prop in status.Info.PropertiesByID)
                values.Add((prop.ID, prop.Getter(status.Object)));
            packet.Changes.Add(new SyncPacket.Object(status.Info.ID, status.SyncID, Special.Spawn, values));
        }
        NetworkPackets.Send(server, new []{ networkID }, packet);
    }

    internal static void ClearAllObjects(Game game)
    {
        var statusByObject = (game is Server) ? _serverStatusByObject : _clientStatusByObject;
        var statusBySyncID = (game is Server) ? _serverStatusBySyncID : _clientStatusBySyncID;

        foreach (var (node, _) in statusByObject) {
            if (!Godot.Object.IsInstanceValid(node)) continue;
            node.GetParent().RemoveChild(node);
            node.QueueFree();
        }

        statusByObject.Clear();
        statusBySyncID.Clear();
        _dirtyObjects.Clear();
        _syncIDCounter = 1;
    }

    public static uint GetSyncID(this Node obj)
        => GetSyncStatus(obj).SyncID;
    public static Node GetObjectBySyncID(this Game game, uint syncID)
    {
        var statusBySyncID = (game is Server) ? _serverStatusBySyncID : _clientStatusBySyncID;
        return statusBySyncID.TryGetValue(syncID, out var value) ? value.Object : null;
    }

    private static SyncStatus GetSyncStatus(Node obj)
    {
        if (obj.GetType().GetCustomAttribute<SyncObjectAttribute>() == null)
            throw new ArgumentException($"Type {obj.GetType()} is missing {nameof(SyncObjectAttribute)}");
        var statusByObject = (obj.GetGame() is Server) ? _serverStatusByObject : _clientStatusByObject;
        if (!statusByObject.TryGetValue(obj, out var value)) throw new Exception(
            $"No {nameof(SyncStatus)} found for '{obj.Name}' ({obj.GetType()})");
        return value;
    }

    private class SyncStatus
    {
        public uint SyncID { get; }
        public Node Object { get; }
        public SyncObjectInfo Info { get; }

        public int DirtyProperties { get; set; }
        public Special Special { get; set; }

        public SyncStatus(uint syncID, Node obj, SyncObjectInfo info)
            { SyncID = syncID; Object = obj; Info = info; }
    }
    public enum Special
    {
        None,
        Spawn,
        Destroy,
    }


    private static void DiscoverSyncableObjects()
    {
        foreach (var type in typeof(NetworkSync).Assembly.GetTypes()) {
            var objAttr = type.GetCustomAttribute<SyncObjectAttribute>();
            if (objAttr == null) continue;

            if (!typeof(Node).IsAssignableFrom(type)) throw new Exception(
                $"Type {type} with {nameof(SyncObjectAttribute)} must be a subclass of {nameof(Node)}");

            var objInfo = new SyncObjectInfo((ushort)_infoByID.Count, type);
            foreach (var property in type.GetProperties()) {
                if (property.GetCustomAttribute<SyncPropertyAttribute>() == null) continue;
                var propType = typeof(SyncPropertyInfo<,>).MakeGenericType(type, property.PropertyType);
                var propInfo = (SyncPropertyInfo)Activator.CreateInstance(propType, (byte)objInfo.PropertiesByID.Count, property);
                objInfo.PropertiesByID.Add(propInfo);
                objInfo.PropertiesByName.Add(propInfo.Name, propInfo);

                // Ensure that the de/serializer for this type has been generated.
                DeSerializerRegistry.Get(propInfo.Type, true);
            }
            _infoByID.Add(objInfo);
            _infoByType.Add(objInfo.Type, objInfo);
        }
    }


    private class SyncObjectInfo
    {
        public ushort ID { get; }
        public Type Type { get; }
        public string Name => Type.Name;

        public PackedScene InstanceScene { get; }
        public string ContainerNodePath { get; }

        public List<SyncPropertyInfo> PropertiesByID { get; } = new List<SyncPropertyInfo>();
        public Dictionary<string, SyncPropertyInfo> PropertiesByName { get; } = new Dictionary<string, SyncPropertyInfo>();

        public SyncObjectInfo(ushort id, Type type)
        {
            ID   = id;
            Type = type;

            var attr = type.GetCustomAttribute<SyncObjectAttribute>();
            InstanceScene     = GD.Load<PackedScene>($"res://scene/{attr.Scene}.tscn");
            ContainerNodePath = attr.Container;
        }
    }

    private abstract class SyncPropertyInfo
    {
        public byte ID { get; }
        public PropertyInfo Property { get; }
        public string Name => Property.Name;
        public Type Type => Property.PropertyType;

        public Func<object, object> Getter { get; }
        public Action<object, object> Setter { get; }

        public SyncPropertyInfo(byte id, PropertyInfo property,
            Func<object, object> getter, Action<object, object> setter)
        {
            ID = id; Property = property;
            Getter = getter; Setter = setter;
        }
    }

    private class SyncPropertyInfo<TObject, TValue> : SyncPropertyInfo
    {
        public SyncPropertyInfo(byte id, PropertyInfo property) : base(id, property,
            obj => ((Func<TObject, TValue>)property.GetMethod.CreateDelegate(typeof(Func<TObject, TValue>))).Invoke((TObject)obj),
            (obj, value) => ((Action<TObject, TValue>)property.SetMethod.CreateDelegate(typeof(Action<TObject, TValue>))).Invoke((TObject)obj, (TValue)value)
        ) {  }
    }


    private static void RegisterPackets()
    {
        DeSerializerRegistry.Register(new SyncPacketObjectDeSerializer());
        NetworkPackets.Register<SyncPacket>(PacketDirection.ServerToClient, OnSyncPacket);
    }

    private static void OnSyncPacket(Game game, NetworkID networkID, SyncPacket packet)
    {
        foreach (var packetObj in packet.Changes) {
            if (packetObj.InfoID >= _infoByID.Count) throw new Exception(
                $"Unknown {nameof(SyncObjectInfo)} with ID {packetObj.InfoID}");
            var info = _infoByID[packetObj.InfoID];

            if (!_clientStatusBySyncID.TryGetValue(packetObj.SyncID, out var status)) {
                if (packetObj.Special != Special.Spawn) throw new Exception(
                    $"Unknown synced object {info.Name} (ID {packetObj.SyncID})");

                var obj = info.InstanceScene.Init<Node>();
                status  = new SyncStatus(packetObj.SyncID, obj, info);
                _clientStatusBySyncID.Add(status.SyncID, status);
                _clientStatusByObject.Add(status.Object, status);
                game.GetNode(info.ContainerNodePath).AddChild(obj);
            } else {
                if (packetObj.Special == Special.Spawn) throw new Exception(
                    $"Spawning object {info.Name} with ID {packetObj.SyncID}, but it already exists");
                if (info != status.Info) throw new Exception(
                    $"Info of synced object being modified doesn't match ({info.Name} != {status.Info.Name})");

                if (packetObj.Special == Special.Destroy) {
                    _clientStatusBySyncID.Remove(status.SyncID);
                    _clientStatusByObject.Remove(status.Object);

                    status.Object.GetParent().RemoveChild(status.Object);
                    status.Object.QueueFree();
                    continue;
                }
            }

            foreach (var (propID, value) in packetObj.Values) {
                var propInfo = info.PropertiesByID[propID];
                propInfo.Setter(status.Object, value);
            }
        }
    }

    private class SyncPacket
    {
        public List<Object> Changes { get; } = new List<Object>();

        public class Object
        {
            public ushort InfoID { get; }
            public uint SyncID { get; }
            public Special Special { get; }
            public List<(byte, object)> Values { get; }
            public Object(ushort infoID, uint syncID, Special special, List<(byte, object)> values)
                { InfoID = infoID; SyncID = syncID; Special = special; Values = values; }
        }
    }

    private class SyncPacketObjectDeSerializer
        : DeSerializer<SyncPacket.Object>
    {
        public override void Serialize(Game game, BinaryWriter writer, SyncPacket.Object value)
        {
            writer.Write(value.InfoID);
            writer.Write(value.SyncID);
            writer.Write((byte)value.Special);
            writer.Write((byte)value.Values.Count);

            if (value.InfoID >= _infoByID.Count)
                throw new Exception($"No {nameof(SyncObjectInfo)} with ID {value.InfoID}");
            var objInfo = _infoByID[value.InfoID];

            foreach (var (propID, val) in value.Values) {
                writer.Write(propID);
                var propInfo = objInfo.PropertiesByID[propID];
                var deSerializer = DeSerializerRegistry.Get(propInfo.Type, false);
                deSerializer.Serialize(game, writer, val);
            }
        }

        public override SyncPacket.Object Deserialize(Game game, BinaryReader reader)
        {
            var objectID  = reader.ReadUInt16();
            var syncID = reader.ReadUInt32();
            var special   = (Special)reader.ReadByte();
            var count     = reader.ReadByte();

            if (objectID >= _infoByID.Count)
                throw new Exception($"No sync object with ID {objectID}");
            var objInfo = _infoByID[objectID];

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

            return new SyncPacket.Object(objectID, syncID, special, values);
        }
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class SyncObjectAttribute : Attribute
{
    public string Scene { get; }
    public string Container { get; }
    public SyncObjectAttribute(string scene, string container)
        { Scene = scene; Container = container; }
}

[AttributeUsage(AttributeTargets.Property)]
public class SyncPropertyAttribute : Attribute
{
}
