using System;
using System.Collections.Generic;
using Godot;

// TODO: Allow for initially private integrated server to open itself up to the public.
public class Server : Game
{
    private readonly Dictionary<NetworkID, Player> _playersByNetworkID = new Dictionary<NetworkID, Player>();
    private readonly Dictionary<Player, NetworkID> _networkIDByPlayer = new Dictionary<Player, NetworkID>();

    public Server()
    {
        CustomMultiplayer = new MultiplayerAPI { RootNode = this };
        CustomMultiplayer.Connect("network_peer_connected", this, nameof(OnPeerConnected));
        CustomMultiplayer.Connect("network_peer_disconnected", this, nameof(OnPeerDisconnected));
        CustomMultiplayer.Connect("network_peer_packet", this, nameof(OnPacketReceived));
    }

    public override void _Process(float delta)
    {
        CustomMultiplayer.Poll();
        NetworkSync.ProcessDirty(this);
        NetworkRPC.ProcessPacketBuffer(this);
    }


    public void Start(ushort port)
    {
        if (CustomMultiplayer.NetworkPeer != null)
            throw new InvalidOperationException("Server is already running");
        var peer  = new NetworkedMultiplayerENet();
        var error = peer.CreateServer(port);
        if (error != Error.Ok) throw new Exception($"Error when starting the server: {error}");
        CustomMultiplayer.NetworkPeer = peer;

        // Spawn default blocks.
        for (var x = -6; x <= 6; x++) {
            var block = this.Spawn<Block>();
            block.Position    = new BlockPos(x, 3);
            block.Color       = Color.FromHsv(GD.Randf(), 0.1F, 1.0F);
            block.Unbreakable = true;
        }
    }

    public void Stop()
    {
        if (CustomMultiplayer.NetworkPeer != null)
            throw new InvalidOperationException("Server is not running");
        ((NetworkedMultiplayerENet)CustomMultiplayer.NetworkPeer).CloseConnection();
        CustomMultiplayer.NetworkPeer = null;
    }


    public Player GetPlayer(NetworkID networkID)
        => _playersByNetworkID[networkID];
    public NetworkID GetNetworkID(Player player)
        => _networkIDByPlayer[player];


    private void OnPeerConnected(int id)
    {
        var networkID = new NetworkID(id);
        NetworkSync.SendAllObjects(this, networkID);

        var player = this.Spawn<Player>();
        player.Position = Vector2.Zero;
        player.Color    = Colors.Red;

        _playersByNetworkID.Add(networkID, player);
        _networkIDByPlayer.Add(player, networkID);

        player.RPC(new []{ networkID }, player.SetLocal);
    }

    private void OnPeerDisconnected(int id)
    {
        var networkID = new NetworkID(id);
        var player    = GetPlayer(networkID);
        player.Destroy();
        _playersByNetworkID.Remove(networkID);
        _networkIDByPlayer.Remove(player);
    }

    private void OnPacketReceived(int networkID, byte[] bytes)
        => NetworkPackets.Process(this, new NetworkID(networkID), bytes);


    public void RPC(IEnumerable<NetworkID> targets, Action<Client> action) => NetworkRPC.Call(this, targets, action.Method, false);
    public void RPC<T>(IEnumerable<NetworkID> targets, Action<Client, T> action, T arg) => NetworkRPC.Call(this, targets, action.Method, false, arg);
    public void RPC<T0, T1>(IEnumerable<NetworkID> targets, Action<Client, T0, T1> action, T0 arg0, T1 arg1) => NetworkRPC.Call(this, targets, action.Method, false, arg0, arg1);
    public void RPC<T0, T1, T2>(IEnumerable<NetworkID> targets, Action<Client, T0, T1, T2> action, T0 arg0, T1 arg1, T2 arg2) => NetworkRPC.Call(this, targets, action.Method, false, arg0, arg1, arg2);
    public void RPC<T0, T1, T2, T3>(IEnumerable<NetworkID> targets, Action<Client, T0, T1, T2, T3> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3) => NetworkRPC.Call(this, targets, action.Method, false, arg0, arg1, arg2, arg3);
    public void RPC<T0, T1, T2, T3, T4>(IEnumerable<NetworkID> targets, Action<Client, T0, T1, T2, T3, T4> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => NetworkRPC.Call(this, targets, action.Method, false, arg0, arg1, arg2, arg3, arg4);
    public void RPC<T0, T1, T2, T3, T4, T5>(IEnumerable<NetworkID> targets, Action<Client, T0, T1, T2, T3, T4, T5> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => NetworkRPC.Call(this, targets, action.Method, false, arg0, arg1, arg2, arg3, arg4, arg5);
    public void RPC<T0, T1, T2, T3, T4, T5, T6>(IEnumerable<NetworkID> targets, Action<Client, T0, T1, T2, T3, T4, T5, T6> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => NetworkRPC.Call(this, targets, action.Method, false, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
    public void RPC<T0, T1, T2, T3, T4, T5, T6, T7>(IEnumerable<NetworkID> targets, Action<Client, T0, T1, T2, T3, T4, T5, T6, T7> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) => NetworkRPC.Call(this, targets, action.Method, false, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
}

public readonly struct NetworkID : IEquatable<NetworkID>
{
    public static readonly NetworkID Server = new NetworkID(1);
    public int Value { get; }
    public NetworkID(int value) => Value = value;
    public override bool Equals(object obj) => (obj is NetworkID other) && Equals(other);
    public bool Equals(NetworkID other) => Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => $"NetworkID({Value})";
    public static bool operator ==(NetworkID left, NetworkID right) => left.Equals(right);
    public static bool operator !=(NetworkID left, NetworkID right) => !left.Equals(right);
}
