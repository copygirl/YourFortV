using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;

[Flags]
public enum PacketDirection
{
    ServerToClient = 0b01,
    ClientToServer = 0b10,
    Both = ServerToClient | ClientToServer,
}

public enum TransferMode
{
    Unreliable        = NetworkedMultiplayerPeer.TransferModeEnum.Unreliable,
    UnreliableOrdered = NetworkedMultiplayerPeer.TransferModeEnum.UnreliableOrdered,
    Reliable          = NetworkedMultiplayerPeer.TransferModeEnum.Reliable,
}

// TODO: Improve performance and type safety of de/serialization.
// TODO: Support easily spawning and syncronizing objects and their properties.
public partial class NetworkAPI
{
    private readonly MultiplayerAPI _multiplayerAPI;
    private readonly List<PacketInfo> _packetsById = new List<PacketInfo>();
    private readonly Dictionary<Type, PacketInfo> _packetsByType = new Dictionary<Type, PacketInfo>();
    private readonly Dictionary<Type, INetworkDeSerializer> _deSerializers = new Dictionary<Type, INetworkDeSerializer>();
    private readonly List<INetworkDeSerializerGenerator> _deSerializerGenerators = new List<INetworkDeSerializerGenerator>();

    private class PacketInfo
    {
        public int ID { get; }
        public Type Type { get; }
        public PacketDirection Direction { get; }
        public TransferMode DefaultTransformMode { get; }
        public INetworkDeSerializer DeSerializer { get; }
        public Action<int, object> OnPacketReceived { get; }

        public PacketInfo(int id, Type type, PacketDirection direction,
            TransferMode defaultTransferMode, INetworkDeSerializer deSerializer,
            Action<int, object> onPacketReceived)
        {
            ID = id;
            Type = type;
            Direction = direction;
            DefaultTransformMode = defaultTransferMode;
            DeSerializer = deSerializer;
            OnPacketReceived = onPacketReceived;
        }
    }

    public NetworkAPI(MultiplayerAPI multiplayerAPI)
    {
        _multiplayerAPI = multiplayerAPI;

        RegisterDeSerializer((writer, value) => writer.Write(value), reader => reader.ReadBoolean());
        RegisterDeSerializer((writer, value) => writer.Write(value), reader => reader.ReadByte());
        RegisterDeSerializer((writer, value) => writer.Write(value), reader => reader.ReadSByte());
        RegisterDeSerializer((writer, value) => writer.Write(value), reader => reader.ReadInt16());
        RegisterDeSerializer((writer, value) => writer.Write(value), reader => reader.ReadUInt16());
        RegisterDeSerializer((writer, value) => writer.Write(value), reader => reader.ReadInt32());
        RegisterDeSerializer((writer, value) => writer.Write(value), reader => reader.ReadUInt32());
        RegisterDeSerializer((writer, value) => writer.Write(value), reader => reader.ReadInt64());
        RegisterDeSerializer((writer, value) => writer.Write(value), reader => reader.ReadUInt64());
        RegisterDeSerializer((writer, value) => writer.Write(value), reader => reader.ReadSingle());
        RegisterDeSerializer((writer, value) => writer.Write(value), reader => reader.ReadDouble());
        RegisterDeSerializer((writer, value) => writer.Write(value), reader => reader.ReadString());

        // byte[]
        RegisterDeSerializer((writer, value) => { writer.Write(value.Length); writer.Write(value); },
                             reader => reader.ReadBytes(reader.ReadInt32()));
        // Vector2
        RegisterDeSerializer((writer, value) => { writer.Write(value.x); writer.Write(value.y); },
                             reader => new Vector2(reader.ReadSingle(), reader.ReadSingle()));
        // Color
        RegisterDeSerializer((writer, value) => writer.Write(value.ToRgba32()),
                             reader => new Color(reader.ReadInt32()));

        RegisterDeSerializerGenerator(new DictionaryDeSerializerGenerator());
        RegisterDeSerializerGenerator(new CollectionDeSerializerGenerator());
        RegisterDeSerializerGenerator(new ArrayDeSerializerGenerator());
    }

    public void RegisterDeSerializer<T>(Action<BinaryWriter, T> serialize, Func<BinaryReader, T> deserialize)
        => _deSerializers.Add(typeof(T), new SimpleDeSerializer<T>(serialize, deserialize));
    public void RegisterDeSerializer<T>(INetworkDeSerializer deSerializer)
        => _deSerializers.Add(typeof(T), deSerializer);
    public void RegisterDeSerializerGenerator(INetworkDeSerializerGenerator deSerializerGenerator)
        => _deSerializerGenerators.Add(deSerializerGenerator);

    public void RegisterS2CPacket<T>(Action<T> action, TransferMode defaultTransferMode = TransferMode.Reliable)
        => RegisterPacket((int _id, T packet) => action(packet), defaultTransferMode, PacketDirection.ServerToClient);

    public void RegisterC2SPacket<T>(Action<int, T> action, TransferMode defaultTransferMode = TransferMode.Reliable)
        => RegisterPacket(action, defaultTransferMode, PacketDirection.ClientToServer);
    public void RegisterC2SPacket<T>(Action<Player, T> action, TransferMode defaultTransferMode = TransferMode.Reliable)
        => RegisterPacket(action, defaultTransferMode, PacketDirection.ClientToServer);

    public void RegisterPacket<T>(Action<Player, T> action,
                                  TransferMode defaultTransferMode = TransferMode.Reliable,
                                  PacketDirection direction = PacketDirection.Both)
        => RegisterPacket((int id, T packet) => action(Network.GetPlayerOrThrow(id), packet), defaultTransferMode, direction);
    public void RegisterPacket<T>(Action<int, T> action,
        TransferMode defaultTransferMode = TransferMode.Reliable,
        PacketDirection direction = PacketDirection.Both)
    {
        var deSerializer = GetDeSerializer(typeof(T), true);
        var info = new PacketInfo(_packetsById.Count, typeof(T),
            direction, defaultTransferMode, deSerializer,
            (id, packet) => action(id, (T)packet));
        _packetsByType.Add(typeof(T), info);
        _packetsById.Add(info);
    }

    public void SendToServer<T>(T packet, TransferMode? transferMode = null)
        => SendTo(1, packet, transferMode);

    public void SendTo<T>(Player player, T packet, TransferMode? transferMode = null)
        => SendTo(player.NetworkID, packet, transferMode);
    public void SendTo<T>(int id, T packet, TransferMode? transferMode = null)
        => SendTo(new []{ id }, packet, transferMode);

    public void SendToEveryone<T>(T packet, TransferMode? transferMode = null)
        => SendTo(_multiplayerAPI.GetNetworkConnectedPeers(), packet, transferMode);
    public void SendToEveryoneExcept<T>(Player except, T packet, TransferMode? transferMode = null)
        => SendToEveryoneExcept(except?.NetworkID ?? 0, packet, transferMode);
    public void SendToEveryoneExcept<T>(int except, T packet, TransferMode? transferMode = null)
        => SendTo(_multiplayerAPI.GetNetworkConnectedPeers().Where(id => id != except), packet, transferMode);

    public void SendTo<T>(IEnumerable<Player> players, T packet, TransferMode? transferMode = null)
        => SendTo(players.Select(p => p.NetworkID), packet, transferMode);
    public void SendTo<T>(IEnumerable<int> ids, T packet, TransferMode? transferMode = null)
    {
        var info = GetPacketInfoAndVerifyDirection<T>();
        var mode = ToPeerTransferMode(info, transferMode);
        byte[] bytes = null;
        foreach (var id in ids) {
            // Only serialize the packet if sending to at least 1 player.
            bytes = bytes ?? PacketToBytes(info, packet);
            _multiplayerAPI.SendBytes(bytes, id, mode);
        }
    }

    private PacketInfo GetPacketInfoAndVerifyDirection<T>()
    {
        if (!_packetsByType.TryGetValue(typeof(T), out var info))
            throw new InvalidOperationException($"No packet of type {typeof(T)} has been registered");

        var direction = Network.IsServer ? PacketDirection.ServerToClient : PacketDirection.ClientToServer;
        if ((direction & info.Direction) == 0) throw new InvalidOperationException(
            $"Attempting to send packet {typeof(T)} in invalid direction {direction}");

        return info;
    }
    private byte[] PacketToBytes(PacketInfo info, object packet)
    {
        using (var stream = new MemoryStream()) {
            using (var writer = new BinaryWriter(stream)) {
                writer.Write((ushort)info.ID);
                info.DeSerializer.Serialize(writer, packet);
            }
            return stream.ToArray();
        }
    }
    private NetworkedMultiplayerPeer.TransferModeEnum ToPeerTransferMode(PacketInfo info, TransferMode? transferMode)
        => (NetworkedMultiplayerPeer.TransferModeEnum)(transferMode ?? info.DefaultTransformMode);


    internal void OnPacketReceived(int id, byte[] bytes)
    {
        if (!Network.IsServer && (id != 1))
            throw new Exception($"Received packet from other player (ID {id})");


        using (var stream = new MemoryStream(bytes)) {
            using (var reader = new BinaryReader(stream)) {
                var packetId = reader.ReadUInt16();
                if (packetId >= _packetsById.Count) throw new Exception(
                    $"Received packet with invalid ID {packetId}");
                var info = _packetsById[packetId];

                var direction = Network.IsServer ? PacketDirection.ClientToServer : PacketDirection.ServerToClient;
                if ((direction & info.Direction) == 0) throw new Exception(
                    $"Received packet {info.Type} on invalid side {(_multiplayerAPI.IsNetworkServer() ? "server" : "client")}");

                var packet = info.DeSerializer.Deserialize(reader);
                var playerID = Network.IsServer ? id : Network.LocalNetworkID;
                info.OnPacketReceived(id, packet);
            }
        }
    }


    private INetworkDeSerializer GetDeSerializer(Type type, bool createIfMissing)
    {
        if (!_deSerializers.TryGetValue(type, out var value)) {
            if (!createIfMissing) throw new InvalidOperationException(
                $"No DeSerializer for type {type} found");

            value = _deSerializerGenerators.Select(g => g.GenerateFor(type)).FirstOrDefault(x => x != null);
            if (value == null) value = new ComplexDeSerializer(type);
            _deSerializers.Add(type, value);
        }
        return value;
    }
}
