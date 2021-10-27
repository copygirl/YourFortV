using Godot;

// TODO: Add saving of block entities.
public class BlockEntity : Node2D, INotifyChildRemoved
{
    public BlockRef Block { get; }

    public BlockEntity(BlockRef block)
    {
        Block    = block;
        Position = block.Position.GlobalToChunkRel().ToVector();
    }

    public void OnChildRemoved(Node child)
    {
        if (GetChildCount() == 0)
            this.RemoveFromParent();
    }
}
