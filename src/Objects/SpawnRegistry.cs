using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;

public static class SpawnRegistry
{
    private static readonly Dictionary<int, SpawnInfo> _byID = new Dictionary<int, SpawnInfo>();
    private static readonly Dictionary<Type, SpawnInfo> _byType = new Dictionary<Type, SpawnInfo>();

    static SpawnRegistry()
    {
        foreach (var type in typeof(SpawnRegistry).Assembly.GetTypes()) {
            var objAttr = type.GetCustomAttribute<SpawnAttribute>();
            if (objAttr == null) continue;

            if (!typeof(Node).IsAssignableFrom(type)) throw new Exception(
                $"Type {type} with {nameof(SpawnAttribute)} must be a subclass of {nameof(Node)}");

            var objInfo = new SpawnInfo(type);
            _byID.Add(objInfo.HashID, objInfo);
            _byType.Add(objInfo.Type, objInfo);
        }
    }

    public static T Spawn<T>(this Server server)
        where T : Node
    {
        var info = Get<T>();
        var obj  = info.Scene.Init<T>();
        server.GetNode("World").AddChild(obj, true);
        server.Objects.Add(null, obj);
        return obj;
    }

    public static SpawnInfo Get(int id)
        => _byID.TryGetValue(id, out var value) ? value : throw new Exception(
            $"No {nameof(SpawnInfo)} found with ID {id}");

    public static SpawnInfo Get<T>()
        => Get(typeof(T));
    public static SpawnInfo Get(Type type)
        => _byType.TryGetValue(type, out var value) ? value : throw new Exception(
            $"No {nameof(SpawnInfo)} found for type {type} (missing {nameof(SpawnAttribute)}?)");
}

public class SpawnInfo
{
    public Type Type { get; }
    public int HashID { get; }
    public PackedScene Scene { get; }

    public SpawnInfo(Type type)
    {
        Type   = type;
        HashID = type.FullName.GetDeterministicHashCode();

        var sceneStr = Type.GetCustomAttribute<SpawnAttribute>().Scene;
        if (sceneStr == null) sceneStr = $"res://scene/{Type.Name}.tscn";
        Scene = GD.Load<PackedScene>(sceneStr);
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class SpawnAttribute : Attribute
{
    public string Scene { get; }
    public SpawnAttribute(string scene = null)
        => Scene = scene;
}
