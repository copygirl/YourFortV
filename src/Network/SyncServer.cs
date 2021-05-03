using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class SyncServer : Sync
{
    private static readonly HashSet<SyncStatus> _dirtyObjects = new HashSet<SyncStatus>();

    protected Server Server => (Server)Game;

    public SyncServer(Server server) : base(server)
        => server.Objects.Cleared += _dirtyObjects.Clear;

    protected override void OnSyncedAdded(SyncStatus status)
    {
        status.Mode = SyncMode.Spawn;
        _dirtyObjects.Add(status);
    }

    protected override void OnSyncedRemoved(SyncStatus status)
    {
        status.Mode = SyncMode.Destroy;
        _dirtyObjects.Add(status);
    }


    public void MarkDirty(Node obj, string property)
    {
        var status = GetStatusOrThrow(obj);
        if (!status.Info.PropertiesByName.TryGetValue(property, out var propDeSerializer)) throw new ArgumentException(
            $"No {nameof(IPropertyDeSerializer)} found for {obj.GetType()}.{property} (missing {nameof(SyncAttribute)}?)", nameof(property));
        if (!(obj.GetGame() is Server)) return;

        var index = status.Info.PropertiesByID.IndexOf(propDeSerializer);
        status.DirtyProperties |= 1 << index;
        _dirtyObjects.Add(status);
    }


    public void ProcessDirty(Server server)
    {
        if (_dirtyObjects.Count == 0) return;

        var packet = new SyncPacket();
        foreach (var status in _dirtyObjects) {
            var values = new List<(byte, object)>();
            if (status.Mode != SyncMode.Destroy)
                for (byte i = 0; i < status.Info.PropertiesByID.Count; i++)
                    if ((status.DirtyProperties & (1 << i)) != 0)
                        values.Add((i, status.Info.PropertiesByID[i].Get(status.Object)));
            packet.Changes.Add(new SyncPacket.Object(status.Info.ID, status.ID, status.Mode, values));
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
            for (byte i = 0; i < status.Info.PropertiesByID.Count; i++)
                values.Add((i, status.Info.PropertiesByID[i].Get(status.Object)));
            packet.Changes.Add(new SyncPacket.Object(status.Info.ID, status.ID, SyncMode.Spawn, values));
        }
        NetworkPackets.Send(server, new []{ networkID }, packet);
    }
}
