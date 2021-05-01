using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class SyncServer : Sync
{
    private static readonly HashSet<SyncStatus> _dirtyObjects = new HashSet<SyncStatus>();
    private static uint _syncIDCounter = 1;

    protected Server Server => (Server)Game;

    public SyncServer(Server server)
        : base(server) {  }

    public T Spawn<T>()
        where T : Node
    {
        var info   = SyncRegistry.Get<T>();
        var obj    = info.Scene.Init<T>();
        var status = new SyncStatus(_syncIDCounter++, obj, info){ Mode = SyncMode.Spawn };
        StatusBySyncID.Add(status.SyncID, status);
        StatusByObject.Add(status.Object, status);
        _dirtyObjects.Add(status);
        Server.GetNode("World").AddChild(obj);

        return obj;
    }

    // TODO: Do this automatically if the node is removed from the tree?
    public void Destroy(Node obj)
    {
        var status = GetStatusOrThrow(obj);

        status.Mode = SyncMode.Destroy;
        StatusBySyncID.Remove(status.SyncID);
        StatusByObject.Remove(status.Object);
        _dirtyObjects.Add(status);

        obj.GetParent().RemoveChild(obj);
        obj.QueueFree();
    }

    public void MarkDirty(Node obj, string property)
    {
        var status = GetStatusOrThrow(obj);
        if (!status.Info.PropertiesByName.TryGetValue(property, out var propInfo)) throw new ArgumentException(
            $"No {nameof(SyncPropertyInfo)} found for {obj.GetType()}.{property} (missing {nameof(SyncAttribute)}?)", nameof(property));
        if (!(obj.GetGame() is Server)) return;

        status.DirtyProperties |= 1 << propInfo.ID;
        _dirtyObjects.Add(status);
    }


    public void ProcessDirty(Server server)
    {
        if (_dirtyObjects.Count == 0) return;

        var packet = new SyncPacket();
        foreach (var status in _dirtyObjects) {
            var values = new List<(byte, object)>();
            foreach (var prop in status.Info.PropertiesByID)
                if ((status.DirtyProperties & (1 << prop.ID)) != 0)
                    values.Add((prop.ID, prop.Getter(status.Object)));
            packet.Changes.Add(new SyncPacket.Object(status.Info.ID, status.SyncID, status.Mode, values));
            // If the object has been newly spawned, now is the time to remove the "Spawn" flag.
            if (status.Mode == SyncMode.Spawn) status.Mode = SyncMode.Default;
        }
        // TODO: Need a different way to send packages to all *properly* connected peers.
        NetworkPackets.Send(server, server.CustomMultiplayer.GetNetworkConnectedPeers().Select(id => new NetworkID(id)), packet);

        _dirtyObjects.Clear();
    }

    public void SendAllObjects(Server server, NetworkID networkID)
    {
        var packet = new SyncPacket();
        foreach (var status in StatusByObject.Values) {
            var values = new List<(byte, object)>();
            foreach (var prop in status.Info.PropertiesByID)
                values.Add((prop.ID, prop.Getter(status.Object)));
            packet.Changes.Add(new SyncPacket.Object(status.Info.ID, status.SyncID, SyncMode.Spawn, values));
        }
        NetworkPackets.Send(server, new []{ networkID }, packet);
    }

    public override void Clear()
    {
        base.Clear();
        _dirtyObjects.Clear();
        _syncIDCounter = 1;
    }
}
