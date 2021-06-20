using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public static class Extensions
{
    public static Game GetGame(this Node node)
        => node.GetTree().Root.GetChild<Game>(0);
    public static Client GetClient(this Node node)
        => node.GetGame() as Client;
    public static Server GetServer(this Node node)
        => node.GetGame() as Server;
    public static World GetWorld(this Node node)
        => node.GetGame().GetNode<World>("World");

    public static IEnumerable<T> GetChildren<T>(this Node node)
        => node.GetChildren().Cast<T>();
    public static T GetOrCreateChild<T>(this Node node, string name, Func<T> createFunc)
        where T : Node
    {
        var child = node.GetNodeOrNull<T>(name);
        if (child == null) {
            child = createFunc();
            child.Name = name;
            node.AddChild(child);
        }
        return child;
    }
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

    public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue value)
        { key = kvp.Key; value = kvp.Value; }
}
