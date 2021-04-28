using System.Linq;
using Godot;
using Godot.Collections;

public abstract class Game : Node2D
{
	[Export] public NodePath PlayerContainerPath { get; set; }
	[Export] public NodePath BlockContainerPath { get; set; }

	public Node PlayerContainer { get; private set; }
	public Node BlockContainer { get; private set; }

	// Using _EnterTree to make sure this code runs before any other.
	public override void _EnterTree()
		=> GD.Randomize();

    public override void _Ready()
    {
        PlayerContainer = GetNode(PlayerContainerPath);
        BlockContainer  = GetNode(BlockContainerPath);
    }

	// NOTE: When multithreaded physics are enabled, DirectSpaceState can only be used in _PhysicsProcess.
    public Block GetBlockAt(BlockPos position)
        => GetWorld2d().DirectSpaceState.IntersectPoint(position.ToVector()).Cast<Dictionary>()
            .Select(c => c["collider"]).OfType<Block>().FirstOrDefault();
}
