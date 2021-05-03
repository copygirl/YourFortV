using System;
using System.Reflection;

public interface IPropertyDeSerializer
{
    PropertyInfo Property { get; }
    Type Type { get; }
    string Name { get; }
    string FullName { get; }
    int HashID { get; }

    IDeSerializer DeSerializer { get; }
    object Get(object obj);
    void Set(object obj, object value);
}

public class PropertyDeSerializer<TObj, TProp>
    : IPropertyDeSerializer
{
    public PropertyInfo Property { get; }
    public Type Type => Property.PropertyType;
    public string Name => Property.Name;
    public string FullName { get; }
    public int HashID { get; }

    public IDeSerializer<TProp> DeSerializer { get; }
    public Func<TObj, TProp> Getter { get; }
    public Action<TObj, TProp> Setter { get; }

    public PropertyDeSerializer(PropertyInfo property)
    {
        if ((property.GetMethod == null) || (property.SetMethod == null)) throw new Exception(
            $"Property {property.DeclaringType}.{property.Name} must have a getter and setter defined");

        Property = property;
        FullName = $"{Property.DeclaringType.FullName}.{Property.Name}";
        HashID   = FullName.GetDeterministicHashCode();

        DeSerializer = DeSerializerRegistry.Get<TProp>(true);
        Getter = (Func<TObj, TProp>)Property.GetMethod.CreateDelegate(typeof(Func<TObj, TProp>));
        Setter = (Action<TObj, TProp>)Property.SetMethod.CreateDelegate(typeof(Action<TObj, TProp>));
    }

    // IPropertyDeSerializer implementation
    IDeSerializer IPropertyDeSerializer.DeSerializer => DeSerializer;
    object IPropertyDeSerializer.Get(object obj) => Getter((TObj)obj);
    void IPropertyDeSerializer.Set(object obj, object value) => Setter((TObj)obj, (TProp)value);
}
