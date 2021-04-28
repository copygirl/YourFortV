using System;
using System.Collections.Generic;
using System.IO;

[Flags]
public enum PacketDirection
{
    ServerToClient = 0b01,
    ClientToServer = 0b10,
    Both = ServerToClient | ClientToServer,
}

public static class NetworkPackets
{
    private static readonly List<PacketInfo> _packetsById = new List<PacketInfo>();
    private static readonly Dictionary<Type, PacketInfo> _packetsByType = new Dictionary<Type, PacketInfo>();

    public static void Register<T>(PacketDirection direction, Action<Game, NetworkID, T> onReceived)
    {
        var info = new PacketInfo((byte)_packetsById.Count, typeof(T),
            direction, (game, networkID, packet) => onReceived(game, networkID, (T)packet));
        _packetsByType.Add(typeof(T), info);
        _packetsById.Add(info);
    }

    private static byte[] ToBytes(Game game, PacketInfo info, object packet)
    {
        using (var stream = new MemoryStream()) {
            using (var writer = new BinaryWriter(stream)) {
                writer.Write(info.ID);
                info.DeSerializer.Serialize(game, writer, packet);
            }
            return stream.ToArray();
        }
    }

    public static void Send<T>(Game game, IEnumerable<NetworkID> targets, T packet)
    {
        if (!_packetsByType.TryGetValue(typeof(T), out var info))
            throw new InvalidOperationException($"No packet of type {typeof(T)} has been registered");

        var direction = (game is Server) ? PacketDirection.ServerToClient : PacketDirection.ClientToServer;
        if ((direction & info.Direction) == 0) throw new InvalidOperationException(
            $"Attempting to send packet {typeof(T)} in invalid direction {direction}");

        byte[] bytes = null;
        foreach (var networkID in targets) {
            // Only serialize the packet if sending to at least 1 player.
            bytes = bytes ?? ToBytes(game, info, packet);
            game.CustomMultiplayer.SendBytes(bytes, networkID.Value,
                Godot.NetworkedMultiplayerPeer.TransferModeEnum.Reliable);
            // TODO: Should it be possible to send packets in non-reliable modes?
        }
    }

    public static void Process(Game game, NetworkID networkID, byte[] bytes)
    {
        using (var stream = new MemoryStream(bytes)) {
            using (var reader = new BinaryReader(stream)) {
                var packetId = reader.ReadByte();
                if (packetId >= _packetsById.Count) throw new Exception(
                    $"Received packet with invalid ID {packetId}");
                var info = _packetsById[packetId];

                var validDirection = (game is Server) ? PacketDirection.ClientToServer : PacketDirection.ServerToClient;
                if ((validDirection & info.Direction) == 0) throw new Exception($"Received packet {info.Type} on invalid side {game.Name}");

                var packet = info.DeSerializer.Deserialize(game, reader);
                var bytesLeft = bytes.Length - stream.Position;
                if (bytesLeft > 0) throw new Exception(
                    $"There were {bytesLeft} bytes left after deserializing packet {info.Type}");

                info.OnReceived(game, networkID, packet);
            }
        }
    }

    public class PacketInfo
    {
        public byte ID { get; }
        public Type Type { get; }
        public PacketDirection Direction { get; }
        public Action<Game, NetworkID, object> OnReceived { get; }
        public IDeSerializer DeSerializer { get; }

        public PacketInfo(byte id, Type type,
            PacketDirection direction, Action<Game, NetworkID, object> onReceived)
        {
            ID   = id;
            Type = type;
            Direction  = direction;
            OnReceived = onReceived;
            DeSerializer = DeSerializerRegistry.Get(type, true);
        }
    }
}
