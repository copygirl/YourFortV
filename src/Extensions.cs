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


    public static void RsetProperty(this Node @this, Node propertyOwner, string property, string method, object value)
    {
        if (!@this.IsInsideTree()) return;
        if (Network.IsServer) propertyOwner.RsetExcept(@this as Player, property, value);
        else if (Network.IsMultiplayerReady) @this.RpcId(1, method, value);
    }

    public static void RsetPropertyUnreliable(this Node @this, Node propertyOwner, string property, string method, object value)
    {
        if (!@this.IsInsideTree()) return;
        if (Network.IsServer) propertyOwner.RsetUnreliableExcept(@this as Player, property, value);
        else if (Network.IsMultiplayerReady) @this.RpcUnreliableId(1, method, value);
    }


    public static void RpcExcept(this Node @this, Player except, string method, params object[] args)
    {
        foreach (var player in Network.Players)
            if (player != except)
                @this.RpcId(player.NetworkId, method, args);
    }

    public static void RsetExcept(this Node @this, Player except, string property, object value)
    {
        foreach (var player in Network.Players)
            if (player != except)
                @this.RsetId(player.NetworkId, property, value);
    }

    public static void RsetUnreliableExcept(this Node @this, Player except, string property, object value)
    {
        foreach (var player in Network.Players)
            if (player != except)
                @this.RsetUnreliableId(player.NetworkId, property, value);
    }
}

public interface IInitializer
{
    void Initialize();
}
