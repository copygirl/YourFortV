using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;

public static class SyncRegistry
{
    private static readonly List<SyncObjectInfo> _byID = new List<SyncObjectInfo>();
    private static readonly Dictionary<Type, SyncObjectInfo> _byType = new Dictionary<Type, SyncObjectInfo>();

    static SyncRegistry()
    {
        foreach (var type in typeof(SyncRegistry).Assembly.GetTypes()) {
            var objAttr = type.GetCustomAttribute<SyncAttribute>();
            if (objAttr == null) continue;

            if (!typeof(Node).IsAssignableFrom(type)) throw new Exception(
                $"Type {type} with {nameof(SyncAttribute)} must be a subclass of {nameof(Node)}");

            var objInfo = new SyncObjectInfo((ushort)_byID.Count, type);
            foreach (var property in type.GetProperties()) {
                if (property.GetCustomAttribute<SyncAttribute>() == null) continue;
                var propType = typeof(SyncPropertyInfo<,>).MakeGenericType(type, property.PropertyType);
                var propInfo = (SyncPropertyInfo)Activator.CreateInstance(propType, (byte)objInfo.PropertiesByID.Count, property);
                objInfo.PropertiesByID.Add(propInfo);
                objInfo.PropertiesByName.Add(propInfo.Name, propInfo);

                // Ensure that the de/serializer for this type has been generated.
                DeSerializerRegistry.Get(propInfo.Type, true);
            }
            _byID.Add(objInfo);
            _byType.Add(objInfo.Type, objInfo);
        }
    }

    public static SyncObjectInfo Get(ushort id)
        => (id < _byID.Count) ? _byID[id] : throw new Exception(
            $"Unknown {nameof(SyncObjectInfo)} with ID {id}");

    public static SyncObjectInfo Get<T>()
        => Get(typeof(T));
    public static SyncObjectInfo Get(Type type)
        => _byType.TryGetValue(type, out var value) ? value : throw new Exception(
            $"No {nameof(SyncObjectInfo)} found for type {type} (missing {nameof(SyncAttribute)}?)");
}


public class SyncObjectInfo
{
    public ushort ID { get; }
    public Type Type { get; }
    public PackedScene Scene { get; }
    public string Name => Type.Name;

    public List<SyncPropertyInfo> PropertiesByID { get; } = new List<SyncPropertyInfo>();
    public Dictionary<string, SyncPropertyInfo> PropertiesByName { get; } = new Dictionary<string, SyncPropertyInfo>();

    public SyncObjectInfo(ushort id, Type type)
    {
        ID    = id;
        Type  = type;
        Scene = GD.Load<PackedScene>($"res://scene/{type.Name}.tscn");
    }
}

public abstract class SyncPropertyInfo
{
    public byte ID { get; }
    public PropertyInfo Property { get; }
    public string Name => Property.Name;
    public Type Type => Property.PropertyType;

    public Func<object, object> Getter { get; }
    public Action<object, object> Setter { get; }

    public SyncPropertyInfo(byte id, PropertyInfo property,
        Func<object, object> getter, Action<object, object> setter)
    {
        ID = id; Property = property;
        Getter = getter; Setter = setter;
    }
}

public class SyncPropertyInfo<TObject, TValue> : SyncPropertyInfo
{
    public SyncPropertyInfo(byte id, PropertyInfo property) : base(id, property,
        obj => ((Func<TObject, TValue>)property.GetMethod.CreateDelegate(typeof(Func<TObject, TValue>))).Invoke((TObject)obj),
        (obj, value) => ((Action<TObject, TValue>)property.SetMethod.CreateDelegate(typeof(Action<TObject, TValue>))).Invoke((TObject)obj, (TValue)value)
    ) {  }
}


[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public class SyncAttribute : Attribute
{
}
