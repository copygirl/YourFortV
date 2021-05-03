using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

public static class Extensions
{
    public static void RemoveFromParent(this Node node)
    {
        node.GetParent().RemoveChild(node);
        node.QueueFree();
    }

    public static T Init<T>(this PackedScene @this)
        where T : Node
    {
        var instance = (T)@this.Instance();
        (instance as IInitializer)?.Initialize();
        return instance;
    }


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


    public static void Deconstruct<TKey, TValue>(
            this KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue value)
        { key = kvp.Key; value = kvp.Value; }

    public static int GetDeterministicHashCode(this string str)
    { unchecked {
        int hash1 = (5381 << 16) + 5381;
        int hash2 = hash1;
        for (int i = 0; i < str.Length; i += 2) {
            hash1 = ((hash1 << 5) + hash1) ^ str[i];
            if (i == str.Length - 1) break;
            hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
        }
        return hash1 + (hash2 * 1566083941);
    } }

}

public interface IInitializer
{
    void Initialize();
}
