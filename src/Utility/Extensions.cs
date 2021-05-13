using System;
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

    public static float NextFloat(this Random random)
        => (float)random.NextDouble();
    public static float NextFloat(this Random random, float min, float max)
        => min + NextFloat(random) * (max - min);

    public static float NextGaussian(this Random random, float stdDev = 1.0F, float mean = 0.0F)
    {
        var u1 = 1.0F - random.NextFloat();
        var u2 = 1.0F - random.NextFloat();
        var normal = Mathf.Sqrt(-2.0F * Mathf.Log(u1)) * Mathf.Sin(2.0F * Mathf.Pi * u2);
        return  mean + stdDev * normal;
    }
}

public interface IInitializable
{
    void Initialize();
}
