using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Godot;
using static Godot.NetworkedMultiplayerPeer;

public static class NetworkRPC
{
    private static readonly Dictionary<int, RPCMethodInfo> _byId = new Dictionary<int, RPCMethodInfo>();
    private static readonly Dictionary<MethodInfo, RPCMethodInfo> _byMethod = new Dictionary<MethodInfo, RPCMethodInfo>();

    private static readonly List<(NetworkID[], RPCPacket)> _serverPacketBuffer = new List<(NetworkID[], RPCPacket)>();
    private static readonly List<RPCPacket> _clientPacketBuffer = new List<RPCPacket>();

    static NetworkRPC()
    {
        DiscoverRPCMethods();
        RegisterPackets();
    }


    // Client to server instance RPC calls.
    public static void RPC(this Node obj, Action<Player> action) => CallToServer(obj, action.Method);
    public static void RPC<T>(this Node obj, Action<Player, T> action, T arg) => CallToServer(obj, action.Method, arg);
    public static void RPC<T0, T1>(this Node obj, Action<Player, T0, T1> action, T0 arg0, T1 arg1) => CallToServer(obj, action.Method, arg0, arg1);
    public static void RPC<T0, T1, T2>(this Node obj, Action<Player, T0, T1, T2> action, T0 arg0, T1 arg1, T2 arg2) => CallToServer(obj, action.Method, arg0, arg1, arg2);
    public static void RPC<T0, T1, T2, T3>(this Node obj, Action<Player, T0, T1, T2, T3> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3) => CallToServer(obj, action.Method, arg0, arg1, arg2, arg3);
    public static void RPC<T0, T1, T2, T3, T4>(this Node obj, Action<Player, T0, T1, T2, T3, T4> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => CallToServer(obj, action.Method, arg0, arg1, arg2, arg3, arg4);
    public static void RPC<T0, T1, T2, T3, T4, T5>(this Node obj, Action<Player, T0, T1, T2, T3, T4, T5> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => CallToServer(obj, action.Method, arg0, arg1, arg2, arg3, arg4, arg5);
    public static void RPC<T0, T1, T2, T3, T4, T5, T6>(this Node obj, Action<Player, T0, T1, T2, T3, T4, T5, T6> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => CallToServer(obj, action.Method, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
    public static void RPC<T0, T1, T2, T3, T4, T5, T6, T7>(this Node obj, Action<Player, T0, T1, T2, T3, T4, T5, T6, T7> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) => CallToServer(obj, action.Method, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);

    // Server to client instance RPC calls.
    public static void RPC(this Node obj, IEnumerable<NetworkID> targets, Action action) => CallToClient(obj, targets, action.Method);
    public static void RPC<T>(this Node obj, IEnumerable<NetworkID> targets, Action<T> action, T arg) => CallToClient(obj, targets, action.Method, arg);
    public static void RPC<T0, T1>(this Node obj, IEnumerable<NetworkID> targets, Action<T0, T1> action, T0 arg0, T1 arg1) => CallToClient(obj, targets, action.Method, arg0, arg1);
    public static void RPC<T0, T1, T2>(this Node obj, IEnumerable<NetworkID> targets, Action<T0, T1, T2> action, T0 arg0, T1 arg1, T2 arg2) => CallToClient(obj, targets, action.Method, arg0, arg1, arg2);
    public static void RPC<T0, T1, T2, T3>(this Node obj, IEnumerable<NetworkID> targets, Action<T0, T1, T2, T3> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3) => CallToClient(obj, targets, action.Method, arg0, arg1, arg2, arg3);
    public static void RPC<T0, T1, T2, T3, T4>(this Node obj, IEnumerable<NetworkID> targets, Action<T0, T1, T2, T3, T4> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => CallToClient(obj, targets, action.Method, arg0, arg1, arg2, arg3, arg4);
    public static void RPC<T0, T1, T2, T3, T4, T5>(this Node obj, IEnumerable<NetworkID> targets, Action<T0, T1, T2, T3, T4, T5> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => CallToClient(obj, targets, action.Method, arg0, arg1, arg2, arg3, arg4, arg5);
    public static void RPC<T0, T1, T2, T3, T4, T5, T6>(this Node obj, IEnumerable<NetworkID> targets, Action<T0, T1, T2, T3, T4, T5, T6> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => CallToClient(obj, targets, action.Method, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
    public static void RPC<T0, T1, T2, T3, T4, T5, T6, T7>(this Node obj, IEnumerable<NetworkID> targets, Action<T0, T1, T2, T3, T4, T5, T6, T7> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) => CallToClient(obj, targets, action.Method, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);

    private static void CallToServer(Node obj, MethodInfo method, params object[] args)
        { if (obj.GetGame() is Client) Call(obj.GetGame(), new []{ NetworkID.Server }, method, true, args.Prepend(obj)); }
    private static void CallToClient(Node obj, IEnumerable<NetworkID> targets, MethodInfo method, params object[] args)
        { if (obj.GetGame() is Server) Call(obj.GetGame(), targets, method, true, args.Prepend(obj)); }


    internal static void Call(Game game, IEnumerable<NetworkID> targets, MethodInfo method, bool isInstance, params object[] args)
        => Call(game, targets, method, isInstance, (IEnumerable<object>)args);
    internal static void Call(Game game, IEnumerable<NetworkID> targets, MethodInfo method, bool isInstance, IEnumerable<object> args)
    {
        if (!_byMethod.TryGetValue(method, out var info)) throw new ArgumentException(
            $"The specified method {method.DeclaringType}.{method.Name} is missing {nameof(RPCAttribute)}", nameof(method));
        if (isInstance == method.IsStatic) throw new ArgumentException(
            $"The specified method {method.DeclaringType}.{method.Name} must be {(isInstance ? "non-static" : "static")} for this RPC call", nameof(method));
        // TODO: Make sure the instance is the right type.

        var direction = (game is Server) ? PacketDirection.ServerToClient : PacketDirection.ClientToServer;
        if (info.Attribute.Direction != direction) throw new Exception(
            $"Sending {info.Attribute.Direction} RPC packet '{info.Name}' from {game.Name}");

        var packet = new RPCPacket(info, new List<object>(args));
        if (game is Server) _serverPacketBuffer.Add((targets.ToArray(), packet));
        else                _clientPacketBuffer.Add(packet);
    }

    internal static void ProcessPacketBuffer(Game game)
    {
        if (game is Server) {
            foreach (var (targets, packet) in _serverPacketBuffer)
                NetworkPackets.Send(game, targets, packet);
            _serverPacketBuffer.Clear();
        } else {
            foreach (var packet in _clientPacketBuffer)
                NetworkPackets.Send(game, new []{ NetworkID.Server }, packet);
            _clientPacketBuffer.Clear();
        }
    }


    private static void DiscoverRPCMethods()
    {
        foreach (var type in typeof(NetworkRPC).Assembly.GetTypes()) {
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
                var rpc = method.GetCustomAttribute<RPCAttribute>();
                if (rpc == null) continue;

                if (!method.IsStatic && (type.GetCustomAttribute<SyncObjectAttribute>() == null)) throw new Exception(
                    $"Type of non-static RPC method '{method.DeclaringType}.{method.Name}' must have {nameof(SyncObjectAttribute)}");

                var deSerializers = new List<IDeSerializer>();
                var paramEnumerable = ((IEnumerable<ParameterInfo>)method.GetParameters()).GetEnumerator();

                var isServer = rpc.Direction == PacketDirection.ClientToServer;
                var gameType = isServer ? typeof(Server) : typeof(Client);
                if (method.IsStatic && (!paramEnumerable.MoveNext() || (paramEnumerable.Current.ParameterType != gameType))) throw new Exception(
                    $"First parameter of {rpc.Direction} RPC method '{method.DeclaringType}.{method.Name}' must be {gameType}");
                if (isServer && (!paramEnumerable.MoveNext() || (paramEnumerable.Current.ParameterType != typeof(NetworkID)))) throw new Exception(
                    $"{(method.IsStatic ? "Second" : "First")} parameter of {rpc.Direction} RPC method '{method.DeclaringType}.{method.Name}' must be {nameof(NetworkID)}");

                if (!method.IsStatic)
                    deSerializers.Add(DeSerializerRegistry.Get(type, true));

                while (paramEnumerable.MoveNext()) {
                    var param        = paramEnumerable.Current;
                    var deSerializer = DeSerializerRegistry.Get(param.ParameterType, true);
                    deSerializers.Add(deSerializer);
                }

                var info = new RPCMethodInfo(method, deSerializers);
                _byId.Add(info.ID, info);
                _byMethod.Add(method, info);
            }
        }
    }

    private class RPCMethodInfo
    {
        public string Name { get; }
        public int ID { get; }
        public MethodInfo Method { get; }
        public RPCAttribute Attribute { get; }
        public List<IDeSerializer> DeSerializers { get; }

        public RPCMethodInfo(MethodInfo method, List<IDeSerializer> deSerializers)
        {
            Name = $"{method.DeclaringType}.{method.Name}";
            ID   = Name.GetHashCode();
            Method        = method;
            Attribute     = method.GetCustomAttribute<RPCAttribute>();
            DeSerializers = deSerializers;
        }
    }


    private static void RegisterPackets()
    {
        DeSerializerRegistry.Register(new RPCPacketDeSerializer());
        NetworkPackets.Register<RPCPacket>(PacketDirection.Both, (game, networkID, packet) => {
            var validDirection = (game is Server) ? PacketDirection.ClientToServer : PacketDirection.ServerToClient;
            if (packet.Info.Attribute.Direction != validDirection) throw new Exception(
                $"Received {packet.Info.Attribute.Direction} RPC packet '{packet.Info.Name}' on side {game.Name}");

            Node obj = null;
            IEnumerable<object> args = packet.Args;

            // If method is instance method, the first argument is the object it is called on.
            if (!packet.Info.Method.IsStatic) { obj = (Node)args.First(); args = args.Skip(1); }
            // If RPC is called on the server, prepend the NetworkID of the client.
            if (game is Server) args = args.Prepend(networkID);
            // If method is static, prepend Client/Server to arguments.
            if (packet.Info.Method.IsStatic) args = args.Prepend(game);

            // TODO: Improve type safety and performance - generate packet for each RPC?
            packet.Info.Method.Invoke(obj, args.ToArray());
        });
    }

    private class RPCPacket
    {
        public RPCMethodInfo Info { get; }
        public List<object> Args { get; }
        public RPCPacket(RPCMethodInfo info, List<object> args)
            { Info = info; Args = args; }
    }

    private class RPCPacketDeSerializer
        : DeSerializer<RPCPacket>
    {
        public override void Serialize(Game game, BinaryWriter writer, RPCPacket value)
        {
            writer.Write(value.Info.ID);
            foreach (var (deSerializer, arg) in value.Info.DeSerializers.Zip(value.Args, Tuple.Create))
                deSerializer.Serialize(game, writer, arg);
        }

        public override RPCPacket Deserialize(Game game, BinaryReader reader)
        {
            var id = reader.ReadInt32();
            if (!_byId.TryGetValue(id, out var info)) throw new Exception($"Unknown RPC ID {id}");
            var args = info.DeSerializers.Select(x => x.Deserialize(game, reader)).ToList();
            return new RPCPacket(info, args);
        }
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class RPCAttribute : Attribute
{
    public PacketDirection Direction { get; }
    public TransferModeEnum TransferMode { get; set; }

    public RPCAttribute(PacketDirection direction) {
        switch (direction) {
            case PacketDirection.ServerToClient:
            case PacketDirection.ClientToServer: Direction = direction; break;
            default: throw new ArgumentException(
                $"Direction must be either ServerToClient or ClientToServer.");
        }
    }
}
