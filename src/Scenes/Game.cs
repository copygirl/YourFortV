using System.Linq;
using Godot;
using Godot.Collections;

public abstract class Game : Node2D
{
    public Sync Sync { get; protected set; }

	// Using _EnterTree to make sure this code runs before any other.
	public override void _EnterTree()
		=> GD.Randomize();

	// NOTE: When multithreaded physics are enabled, DirectSpaceState can only be used in _PhysicsProcess.
    public Block GetBlockAt(BlockPos position)
        => GetWorld2d().DirectSpaceState.IntersectPoint(position.ToVector()).Cast<Dictionary>()
            .Select(c => c["collider"]).OfType<Block>().FirstOrDefault();
}
