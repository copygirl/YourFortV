using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

public static class SaveRegistry
{
    private static readonly Dictionary<int, SaveObjectInfo> _byID = new Dictionary<int, SaveObjectInfo>();
    private static readonly Dictionary<Type, SaveObjectInfo> _byType = new Dictionary<Type, SaveObjectInfo>();

    static SaveRegistry()
    {
        foreach (var type in typeof(SyncRegistry).Assembly.GetTypes()) {
            var syncAttr = type.GetCustomAttribute<SaveAttribute>();
            if (syncAttr == null) continue;

            if (!typeof(Node).IsAssignableFrom(type)) throw new Exception(
                $"Type {type} with {nameof(SyncAttribute)} must be a subclass of {nameof(Node)}");

            var spawnInfo = SpawnRegistry.Get(type);
            var objInfo   = new SaveObjectInfo(type, spawnInfo);

            foreach (var property in type.GetProperties()) {
                if (property.GetCustomAttribute<SaveAttribute>() == null) continue;
                var propType = typeof(PropertyDeSerializer<,>).MakeGenericType(type, property.PropertyType);
                var propDeSerializer = (IPropertyDeSerializer)Activator.CreateInstance(propType, property);
                objInfo.PropertiesByName.Add(propDeSerializer.Name, propDeSerializer);
            }
            objInfo.PropertiesByID.AddRange(objInfo.PropertiesByName.Values.OrderBy(x => x.HashID));

            _byID.Add(objInfo.HashID, objInfo);
            _byType.Add(objInfo.Type, objInfo);
        }
    }

    public static SaveObjectInfo GetOrNull(int hashID)
        => _byID.TryGetValue(hashID, out var value) ? value : null;
    public static SaveObjectInfo GetOrThrow(int hashID) => GetOrNull(hashID)
        ?? throw new Exception($"Unknown {nameof(SaveObjectInfo)} with HashID {hashID}");

    public static SaveObjectInfo GetOrNull<T>() => GetOrNull(typeof(T));
    public static SaveObjectInfo GetOrNull(Type type) => _byType.TryGetValue(type, out var value) ? value : null;

    public static SaveObjectInfo GetOrThrow<T>() => GetOrThrow(typeof(T));
    public static SaveObjectInfo GetOrThrow(Type type) => GetOrNull(type) ?? throw new Exception(
        $"No {nameof(SaveObjectInfo)} found for type {type} (missing {nameof(SyncAttribute)}?)");
}

public class SaveObjectInfo
{
    public Type Type { get; }
    public int HashID { get; }
    public SpawnInfo SpawnInfo { get; }
    public string Name => Type.Name;

    public List<IPropertyDeSerializer> PropertiesByID { get; } = new List<IPropertyDeSerializer>();
    public Dictionary<string, IPropertyDeSerializer> PropertiesByName { get; } = new Dictionary<string, IPropertyDeSerializer>();

    public SaveObjectInfo(Type type, SpawnInfo spawnInfo)
    {
        Type   = type;
        HashID = type.FullName.GetDeterministicHashCode();
        SpawnInfo = spawnInfo;
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public class SaveAttribute : Attribute {  }
