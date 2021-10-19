using System;
using System.Collections;
using System.Collections.Generic;
using Godot;

// TODO: Add ways to add/move/remove items, including event when changed.
public interface IItems : IReadOnlyCollection<Node2D>
{
    Node2D this[int index] { get; }
    Node2D Current { get; set; }
}

public class Items : Node2D, IItems
{
    [Export] public NodePath DefaultItemPath { get; set; }

    public Player Player { get; private set; }

    private Node2D _current;
    public int Count => GetChildCount();
    public Node2D this[int index] => GetChild<Node2D>(index);
    public Node2D Current { get => _current; set => SetCurrent(value, true); }

    public override void _Ready()
    {
        Player = GetParent<Player>();
        foreach (var item in this) SetActive(item, false);
        if (DefaultItemPath != null) SetCurrent(GetNode<Node2D>(DefaultItemPath), false);
    }

    private void SetCurrent(Node2D node, bool sendRpc)
    {
        if (node == _current) return;
        if ((node != null) && (node.GetParent() != this)) throw new ArgumentException();

        SetActive(_current, false);
        SetActive(node, true);
        _current = node;

        if (sendRpc) {
            if (this.GetGame() is Server) RPC.Reliable(DoSetCurrent, _current?.Name);
            else RPC.Reliable(1, DoSetCurrent, _current?.Name);
        }
    }
    [Remote]
    public void DoSetCurrent(string name)
    {
        if ((this.GetGame() is Server) && (Player.NetworkID != GetTree().GetRpcSenderId())) return;
        var node = (name != null) ? GetNode<Node2D>(name) : null;
        SetCurrent(node, this.GetGame() is Server);
    }

    public IEnumerator<Node2D> GetEnumerator()
        => this.GetChildren<Node2D>().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    private static void SetActive(Node2D node, bool value) {
        if (node == null) return;
        node.SetProcessInput(value);
        node.SetProcessUnhandledInput(value);
        node.Visible = value;
        // Call _Process when an item is equipped so it
        // has a chance to update before it gets drawn.
        if (value) node._Process(0);
    }
}
