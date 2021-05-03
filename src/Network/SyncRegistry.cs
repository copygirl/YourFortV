using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

public static class SyncRegistry
{
    private static readonly List<SyncObjectInfo> _byID = new List<SyncObjectInfo>();
    private static readonly Dictionary<Type, SyncObjectInfo> _byType = new Dictionary<Type, SyncObjectInfo>();

    static SyncRegistry()
    {
        foreach (var type in typeof(SyncRegistry).Assembly.GetTypes()) {
            var syncAttr = type.GetCustomAttribute<SyncAttribute>();
            if (syncAttr == null) continue;

            if (!typeof(Node).IsAssignableFrom(type)) throw new Exception(
                $"Type {type} with {nameof(SyncAttribute)} must be a subclass of {nameof(Node)}");

            var spawnInfo = SpawnRegistry.Get(type);
            var objInfo   = new SyncObjectInfo(type, spawnInfo);

            foreach (var property in type.GetProperties()) {
                if (property.GetCustomAttribute<SyncAttribute>() == null) continue;
                var propType = typeof(PropertyDeSerializer<,>).MakeGenericType(type, property.PropertyType);
                var propDeSerializer = (IPropertyDeSerializer)Activator.CreateInstance(propType, property);
                objInfo.PropertiesByName.Add(propDeSerializer.Name, propDeSerializer);
            }
            objInfo.PropertiesByID.AddRange(objInfo.PropertiesByName.Values.OrderBy(x => x.HashID));

            _byType.Add(objInfo.Type, objInfo);
        }
        _byID.AddRange(_byType.Values.OrderBy(x => x.Name.GetDeterministicHashCode()));
        for (ushort i = 0; i < _byID.Count; i++) _byID[i].ID = i;
    }

    public static SyncObjectInfo GetOrThrow(ushort id)
        => (id < _byID.Count) ? _byID[id] : throw new Exception(
            $"Unknown {nameof(SyncObjectInfo)} with ID {id}");

    public static SyncObjectInfo GetOrNull<T>() => GetOrNull(typeof(T));
    public static SyncObjectInfo GetOrNull(Type type) => _byType.TryGetValue(type, out var value) ? value : null;

    public static SyncObjectInfo GetOrThrow<T>() => GetOrThrow(typeof(T));
    public static SyncObjectInfo GetOrThrow(Type type) => GetOrNull(type) ?? throw new Exception(
        $"No {nameof(SyncObjectInfo)} found for type {type} (missing {nameof(SyncAttribute)}?)");
}

public class SyncObjectInfo
{
    public ushort ID { get; internal set; }
    public Type Type { get; }
    public SpawnInfo SpawnInfo { get; }
    public string Name => Type.Name;

    public List<IPropertyDeSerializer> PropertiesByID { get; } = new List<IPropertyDeSerializer>();
    public Dictionary<string, IPropertyDeSerializer> PropertiesByName { get; } = new Dictionary<string, IPropertyDeSerializer>();

    public SyncObjectInfo(Type type, SpawnInfo spawnInfo)
        { Type = type; SpawnInfo = spawnInfo; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public class SyncAttribute : Attribute {  }
