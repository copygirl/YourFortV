using System;
using Godot;

public class SyncClient : Sync
{
    protected Client Client => (Client)Game;

    // FIXME: This works for now, but will break with dedicated servers. We need to register packet types and their handlers separately.
    //        Fortunately, at this time, there is only two packet types: RPC and Sync. We could even reduce that to just a single one?
    public SyncClient(Client client) : base(client)
        => NetworkPackets.Register<SyncPacket>(PacketDirection.ServerToClient, OnSyncPacket);

    private void OnSyncPacket(Game _, NetworkID networkID, SyncPacket packet)
    {
        foreach (var packetObj in packet.Changes) {
            var info   = SyncRegistry.Get(packetObj.InfoID);
            var status = GetStatusOrNull(packetObj.SyncID);

            if (status == null) {
                if (packetObj.Mode != SyncMode.Spawn) throw new Exception(
                    $"Unknown synced object {info.Name} (ID {packetObj.SyncID})");

                var obj = info.Scene.Init<Node>();
                status  = new SyncStatus(packetObj.SyncID, obj, info);
                StatusBySyncID.Add(status.SyncID, status);
                StatusByObject.Add(status.Object, status);
                Client.GetNode("World").AddChild(obj);
            } else {
                if (packetObj.Mode == SyncMode.Spawn) throw new Exception(
                    $"Spawning object {info.Name} with ID {packetObj.SyncID}, but it already exists");
                if (info != status.Info) throw new Exception(
                    $"Info of synced object being modified doesn't match ({info.Name} != {status.Info.Name})");

                if (packetObj.Mode == SyncMode.Destroy) {
                    StatusBySyncID.Remove(status.SyncID);
                    StatusByObject.Remove(status.Object);

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
}
