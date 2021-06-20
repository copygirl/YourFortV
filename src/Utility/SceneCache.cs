using System;
using Godot;

public static class SceneCache<T> where T : Node
{
    private static readonly PackedScene SCENE
        = GD.Load<PackedScene>($"res://scene/{typeof(T).Name}.tscn");

    public static T Instance(Action<T> initFunc = null)
    {
        var node = SCENE.Instance<T>();
        (node as IInitializable)?.Initialize();
        initFunc?.Invoke(node);
        return node;
    }
}

public interface IInitializable
{
    void Initialize();
}
