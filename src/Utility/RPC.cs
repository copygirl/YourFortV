using System;
using Godot;

public static class RPC
{
    public static void Reliable(Action action) => GetNode(action).Rpc(action.Method.Name);
    public static void Reliable<T>(Action<T> action, T arg) => GetNode(action).Rpc(action.Method.Name, arg);
    public static void Reliable<T0, T1>(Action<T0, T1> action, T0 arg0, T1 arg1) => GetNode(action).Rpc(action.Method.Name, arg0, arg1);
    public static void Reliable<T0, T1, T2>(Action<T0, T1, T2> action, T0 arg0, T1 arg1, T2 arg2) => GetNode(action).Rpc(action.Method.Name, arg0, arg1, arg2);
    public static void Reliable<T0, T1, T2, T3>(Action<T0, T1, T2, T3> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3) => GetNode(action).Rpc(action.Method.Name, arg0, arg1, arg2, arg3);
    public static void Reliable<T0, T1, T2, T3, T4>(Action<T0, T1, T2, T3, T4> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => GetNode(action).Rpc(action.Method.Name, arg0, arg1, arg2, arg3, arg4);
    public static void Reliable<T0, T1, T2, T3, T4, T5>(Action<T0, T1, T2, T3, T4, T5> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => GetNode(action).Rpc(action.Method.Name, arg0, arg1, arg2, arg3, arg4, arg5);
    public static void Reliable<T0, T1, T2, T3, T4, T5, T6>(Action<T0, T1, T2, T3, T4, T5, T6> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => GetNode(action).Rpc(action.Method.Name, arg0, arg1, arg2, arg3, arg4, arg5, arg6);

    public static void Reliable(int networkID, Action action) => GetNode(action).RpcId(networkID, action.Method.Name);
    public static void Reliable<T>(int networkID, Action<T> action, T arg) => GetNode(action).RpcId(networkID, action.Method.Name, arg);
    public static void Reliable<T0, T1>(int networkID, Action<T0, T1> action, T0 arg0, T1 arg1) => GetNode(action).RpcId(networkID, action.Method.Name, arg0, arg1);
    public static void Reliable<T0, T1, T2>(int networkID, Action<T0, T1, T2> action, T0 arg0, T1 arg1, T2 arg2) => GetNode(action).RpcId(networkID, action.Method.Name, arg0, arg1, arg2);
    public static void Reliable<T0, T1, T2, T3>(int networkID, Action<T0, T1, T2, T3> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3) => GetNode(action).RpcId(networkID, action.Method.Name, arg0, arg1, arg2, arg3);
    public static void Reliable<T0, T1, T2, T3, T4>(int networkID, Action<T0, T1, T2, T3, T4> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => GetNode(action).RpcId(networkID, action.Method.Name, arg0, arg1, arg2, arg3, arg4);
    public static void Reliable<T0, T1, T2, T3, T4, T5>(int networkID, Action<T0, T1, T2, T3, T4, T5> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => GetNode(action).RpcId(networkID, action.Method.Name, arg0, arg1, arg2, arg3, arg4, arg5);
    public static void Reliable<T0, T1, T2, T3, T4, T5, T6>(int networkID, Action<T0, T1, T2, T3, T4, T5, T6> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => GetNode(action).RpcId(networkID, action.Method.Name, arg0, arg1, arg2, arg3, arg4, arg5, arg6);

    public static void Unreliable(Action action) => GetNode(action).RpcUnreliable(action.Method.Name);
    public static void Unreliable<T>(Action<T> action, T arg) => GetNode(action).RpcUnreliable(action.Method.Name, arg);
    public static void Unreliable<T0, T1>(Action<T0, T1> action, T0 arg0, T1 arg1) => GetNode(action).RpcUnreliable(action.Method.Name, arg0, arg1);
    public static void Unreliable<T0, T1, T2>(Action<T0, T1, T2> action, T0 arg0, T1 arg1, T2 arg2) => GetNode(action).RpcUnreliable(action.Method.Name, arg0, arg1, arg2);
    public static void Unreliable<T0, T1, T2, T3>(Action<T0, T1, T2, T3> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3) => GetNode(action).RpcUnreliable(action.Method.Name, arg0, arg1, arg2, arg3);
    public static void Unreliable<T0, T1, T2, T3, T4>(Action<T0, T1, T2, T3, T4> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => GetNode(action).RpcUnreliable(action.Method.Name, arg0, arg1, arg2, arg3, arg4);
    public static void Unreliable<T0, T1, T2, T3, T4, T5>(Action<T0, T1, T2, T3, T4, T5> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => GetNode(action).RpcUnreliable(action.Method.Name, arg0, arg1, arg2, arg3, arg4, arg5);
    public static void Unreliable<T0, T1, T2, T3, T4, T5, T6>(Action<T0, T1, T2, T3, T4, T5, T6> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => GetNode(action).RpcUnreliable(action.Method.Name, arg0, arg1, arg2, arg3, arg4, arg5, arg6);

    public static void Unreliable(int networkID, Action action) => GetNode(action).RpcUnreliableId(networkID, action.Method.Name);
    public static void Unreliable<T>(int networkID, Action<T> action, T arg) => GetNode(action).RpcUnreliableId(networkID, action.Method.Name, arg);
    public static void Unreliable<T0, T1>(int networkID, Action<T0, T1> action, T0 arg0, T1 arg1) => GetNode(action).RpcUnreliableId(networkID, action.Method.Name, arg0, arg1);
    public static void Unreliable<T0, T1, T2>(int networkID, Action<T0, T1, T2> action, T0 arg0, T1 arg1, T2 arg2) => GetNode(action).RpcUnreliableId(networkID, action.Method.Name, arg0, arg1, arg2);
    public static void Unreliable<T0, T1, T2, T3>(int networkID, Action<T0, T1, T2, T3> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3) => GetNode(action).RpcUnreliableId(networkID, action.Method.Name, arg0, arg1, arg2, arg3);
    public static void Unreliable<T0, T1, T2, T3, T4>(int networkID, Action<T0, T1, T2, T3, T4> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => GetNode(action).RpcUnreliableId(networkID, action.Method.Name, arg0, arg1, arg2, arg3, arg4);
    public static void Unreliable<T0, T1, T2, T3, T4, T5>(int networkID, Action<T0, T1, T2, T3, T4, T5> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => GetNode(action).RpcUnreliableId(networkID, action.Method.Name, arg0, arg1, arg2, arg3, arg4, arg5);
    public static void Unreliable<T0, T1, T2, T3, T4, T5, T6>(int networkID, Action<T0, T1, T2, T3, T4, T5, T6> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => GetNode(action).RpcUnreliableId(networkID, action.Method.Name, arg0, arg1, arg2, arg3, arg4, arg5, arg6);

    private static Node GetNode(Delegate action) => (action.Target as Node) ?? throw new ArgumentException(
        $"Target ({action.Target?.GetType().ToString() ?? "null"}) must be a Node", nameof(action));
}
