using Godot;

public static class Extensions
{
    public static Game GetGame(this Node node)
        => node.GetTree().Root.GetChild<Game>(0);
    public static Client GetClient(this Node node)
        => node.GetGame() as Client;
    public static Server GetServer(this Node node)
        => node.GetGame() as Server;

    public static T Init<T>(this PackedScene @this)
        where T : Node
    {
        var instance = (T)@this.Instance();
        (instance as IInitializer)?.Initialize();
        return instance;
    }
}

public interface IInitializer
{
    void Initialize();
}
