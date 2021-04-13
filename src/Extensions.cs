using Godot;

public static class Extensions
{
    public static void RemoveFromParent(this Node node)
        => node.GetParent().RemoveChild(node);
}
