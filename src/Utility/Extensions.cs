using Godot;

public static class Extensions
{
    public static T Init<T>(this PackedScene scene)
        where T : Node
    {
        var node = scene.Instance<T>();
        (node as IInitializable)?.Initialize();
        return node;
    }

    public static Game GetGame(this Node node)
        => node.GetTree().Root.GetChild<Game>(0);
    public static Client GetClient(this Node node)
        => node.GetGame() as Client;
    public static Server GetServer(this Node node)
        => node.GetGame() as Server;
    public static World GetWorld(this Node node)
        => node.GetGame().GetNode<World>("World");

    public static void RemoveFromParent(this Node node)
    {
        node.GetParent().RemoveChild(node);
        node.QueueFree();
    }
}

public interface IInitializable
{
    void Initialize();
}
