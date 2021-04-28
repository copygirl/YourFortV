using System;
using System.Collections.Generic;
using Godot;

public class Client : Game
{
    [Export] public NodePath CursorPath { get; set; }
    public Cursor Cursor { get; private set; }

    public Client()
    {
        CustomMultiplayer = new MultiplayerAPI { RootNode = this };
        CustomMultiplayer.Connect("connected_to_server", this, nameof(OnConnectedToServer));
        CustomMultiplayer.Connect("connection_failed", this, nameof(Disconnect));
        CustomMultiplayer.Connect("server_disconnected", this, nameof(Disconnect));
        CustomMultiplayer.Connect("network_peer_packet", this, nameof(OnPacketReceived));
    }

    public override void _Ready()
    {
        base._Ready();
        Cursor = GetNode<Cursor>(CursorPath);
    }

    public override void _Process(float delta)
    {
        CustomMultiplayer.Poll();
        NetworkRPC.ProcessPacketBuffer(this);
    }


    public void Connect(string address, ushort port)
    {
        if (CustomMultiplayer.NetworkPeer != null)
            throw new InvalidOperationException("Client connection is already open");
        var peer = new NetworkedMultiplayerENet();
        var error = peer.CreateClient(address, port);
        if (error != Error.Ok) throw new Exception($"Error when connecting: {error}");
        CustomMultiplayer.NetworkPeer = peer;
    }

    public void Disconnect()
    {
        if (CustomMultiplayer.NetworkPeer == null) return;
        ((NetworkedMultiplayerENet)CustomMultiplayer.NetworkPeer).CloseConnection();
        CustomMultiplayer.NetworkPeer = null;
    }


    private void OnConnectedToServer()
    {
        // TODO: Send initial appearance.
    }

    private void OnPacketReceived(int id, byte[] bytes)
    {
        var networkID = new NetworkID(id);
        if (networkID != NetworkID.Server) throw new Exception(
            $"Received packet from other player {networkID}");
        NetworkPackets.Process(this, networkID, bytes);
    }


    private static readonly IEnumerable<NetworkID> ToServer = new []{ NetworkID.Server };
    public void RPC(Action<Server, NetworkID> action) => NetworkRPC.Call(this, ToServer, action.Method, false);
    public void RPC<T>(Action<Server, NetworkID, T> action, T arg) => NetworkRPC.Call(this, ToServer, action.Method, false, arg);
    public void RPC<T0, T1>(Action<Server, NetworkID, T0, T1> action, T0 arg0, T1 arg1) => NetworkRPC.Call(this, ToServer, action.Method, false, arg0, arg1);
    public void RPC<T0, T1, T2>(Action<Server, NetworkID, T0, T1, T2> action, T0 arg0, T1 arg1, T2 arg2) => NetworkRPC.Call(this, ToServer, action.Method, false, arg0, arg1, arg2);
    public void RPC<T0, T1, T2, T3>(Action<Server, NetworkID, T0, T1, T2, T3> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3) => NetworkRPC.Call(this, ToServer, action.Method, false, arg0, arg1, arg2, arg3);
    public void RPC<T0, T1, T2, T3, T4>(Action<Server, NetworkID, T0, T1, T2, T3, T4> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => NetworkRPC.Call(this, ToServer, action.Method, false, arg0, arg1, arg2, arg3, arg4);
    public void RPC<T0, T1, T2, T3, T4, T5>(Action<Server, NetworkID, T0, T1, T2, T3, T4, T5> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => NetworkRPC.Call(this, ToServer, action.Method, false, arg0, arg1, arg2, arg3, arg4, arg5);
    public void RPC<T0, T1, T2, T3, T4, T5, T6>(Action<Server, NetworkID, T0, T1, T2, T3, T4, T5, T6> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => NetworkRPC.Call(this, ToServer, action.Method, false, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
    public void RPC<T0, T1, T2, T3, T4, T5, T6, T7>(Action<Server, NetworkID, T0, T1, T2, T3, T4, T5, T6, T7> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) => NetworkRPC.Call(this, ToServer, action.Method, false, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
}
