using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class ObjectHolder : IReadOnlyCollection<(UniqueID, Node)>
{
    private readonly Dictionary<UniqueID, Node> _nodeByID = new Dictionary<UniqueID, Node>();
    private readonly Dictionary<Node, UniqueID> _idByNode = new Dictionary<Node, UniqueID>();
    private uint _newIDCounter = 1;

    public event Action<UniqueID, Node> Added;
    public event Action<UniqueID, Node> Removed;
    public event Action Cleared;


    public UniqueID GetSyncID(Node obj)
        => _idByNode.TryGetValue(obj, out var value) ? value : throw new Exception(
            $"The specified object '{obj}' does not have a UniqueID");
    public Node GetObjectByID(UniqueID id)
        => _nodeByID.TryGetValue(id, out var value) ? value : throw new Exception(
            $"No object associated with {id}");


    internal void Add(UniqueID? id, Node obj)
    {
        if (!(id is UniqueID uid)) {
            // If the given UniqueID is null, keep going until we find an unused one.
            while (_nodeByID.ContainsKey(uid = new UniqueID(_newIDCounter++))) {  }
        }

        _nodeByID.Add(uid, obj);
        _idByNode.Add(obj, uid);
        Added?.Invoke(uid, obj);
    }

    internal void OnNodeRemoved(Node obj)
    {
        if (!_idByNode.TryGetValue(obj, out var id)) return;

        _nodeByID.Remove(id);
        _idByNode.Remove(obj);
        Removed?.Invoke(id, obj);
    }

    public void Clear()
    {
        var objects = _nodeByID.Values.ToArray();

        _nodeByID.Clear();
        _idByNode.Clear();
        Cleared?.Invoke();

        foreach (var obj in objects)
            obj.RemoveFromParent();
    }

    // IReadOnlyCollection implementation
    public int Count => _nodeByID.Count;
    public IEnumerator<(UniqueID, Node)> GetEnumerator()
        => _nodeByID.Select(entry => (entry.Key, entry.Value)).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}

public readonly struct UniqueID : IEquatable<UniqueID>
{
    public uint Value { get; }
    public UniqueID(uint value) => Value = value;
    public override bool Equals(object obj) => (obj is UniqueID other) && Equals(other);
    public bool Equals(UniqueID other) => Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => $"{nameof(UniqueID)}({Value})";
    public static bool operator ==(UniqueID left, UniqueID right) => left.Equals(right);
    public static bool operator !=(UniqueID left, UniqueID right) => !left.Equals(right);
}
