using Godot;

public static class Extensions
{
    public static void RemoveFromParent(this Node @this)
        => @this.GetParent().RemoveChild(@this);

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
