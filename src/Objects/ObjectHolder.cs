using System;
using System.Collections.Generic;
using Godot;

public class ObjectHolder
{
    private readonly Dictionary<UniqueID, Node> _nodeByID = new Dictionary<UniqueID, Node>();
    private readonly Dictionary<Node, UniqueID> _idByNode = new Dictionary<Node, UniqueID>();
    private uint _newIDCounter = 1;

    public UniqueID Add(Node obj)
    {
        UniqueID id;
        // Keep going until we find an unused UniqueID.
        while (_nodeByID.TryGetValue(id = new UniqueID(_newIDCounter++), out _)) {  }
        Add(id, obj);
        return id;
    }
    public void Add(UniqueID id, Node obj)
    {
        _nodeByID.Add(id, obj);
        _idByNode.Add(obj, id);
    }

    public UniqueID GetSyncID(Node obj)
        => _idByNode.TryGetValue(obj, out var value) ? value : throw new Exception(
            $"The specified object '{obj}' does not have a UniqueID");
    public Node GetNodeByID(UniqueID id)
        => _nodeByID.TryGetValue(id, out var value) ? value : throw new Exception(
            $"No object associated with {id}");

    public void Clear()
    {
        _nodeByID.Clear();
        _idByNode.Clear();
    }
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
