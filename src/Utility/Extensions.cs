using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

public static class Extensions
{
    public static Game GetGame(this Node node)
        => node.GetTree().Root.GetChild<Game>(0);
    public static Client GetClient(this Node node)
        => node.GetGame() as Client;
    public static Server GetServer(this Node node)
        => node.GetGame() as Server;

    public static TValue SetSync<TObject, TValue>(
        this TObject obj, TValue value,
        [CallerMemberName] string property = null)
            where TObject : Node
        { obj.GetServer()?.Sync.MarkDirty(obj, property); return value; }

    public static T Init<T>(this PackedScene @this)
        where T : Node
    {
        var instance = (T)@this.Instance();
        (instance as IInitializer)?.Initialize();
        return instance;
    }

    public static void Deconstruct<TKey, TValue>(
            this KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue value)
        { key = kvp.Key; value = kvp.Value; }
}

public interface IInitializer
{
    void Initialize();
}
