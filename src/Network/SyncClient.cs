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
            var info   = SyncRegistry.GetOrThrow(packetObj.InfoID);
            var status = GetStatusOrNull(packetObj.ID);

            if (status == null) {
                if (packetObj.Mode != SyncMode.Spawn) throw new Exception(
                    $"Unknown synced object {info.Name} (ID {packetObj.ID})");

                var obj = info.SpawnInfo.Scene.Init<Node>();
                Client.GetNode("World").AddChild(obj, true);
                Client.Objects.Add(packetObj.ID, obj);
                status = GetStatusOrThrow(packetObj.ID);
            } else {
                if (packetObj.Mode == SyncMode.Spawn) throw new Exception(
                    $"Spawning object {info.Name} with ID {packetObj.ID}, but it already exists");
                if (info != status.Info) throw new Exception(
                    $"Info of synced object being modified doesn't match ({info.Name} != {status.Info.Name})");

                if (packetObj.Mode == SyncMode.Destroy) {
                    status.Object.RemoveFromParent();
                    continue;
                }
            }

            foreach (var (propID, value) in packetObj.Values) {
                var propDeSerializer = info.PropertiesByID[propID];
                propDeSerializer.Set(status.Object, value);
            }
        }
    }
}
