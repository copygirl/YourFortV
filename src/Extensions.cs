using Godot;

public static class Extensions
{
    public static void RemoveFromParent(this Node @this)
        => @this.GetParent().RemoveChild(@this);

    public static T Init<T>(this PackedScene @this)
        where T : Node
    {
        var instance = (T)@this.Instance();
        (instance as IInitializer)?.Initialize();
        return instance;
    }


    public static void RpcExcept(this Node @this, int except, string method, params object[] args)
    {
        foreach (var peer in @this.GetTree().GetNetworkConnectedPeers())
            if (peer != except) @this.RpcId(peer, method, args);
    }

    public static void RsetUnreliableExcept(this Node @this, int except, string property, object value)
    {
        foreach (var peer in @this.GetTree().GetNetworkConnectedPeers())
            if (peer != except) @this.RsetUnreliableId(peer, property, value);
    }

    public static void RsetExcept(this Node @this, int except, string property, object value)
    {
        foreach (var peer in @this.GetTree().GetNetworkConnectedPeers())
            if (peer != except) @this.RsetId(peer, property, value);
    }
}

public interface IInitializer
{
    void Initialize();
}
