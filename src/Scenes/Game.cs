using Godot;

public abstract class Game : Node
{
	// Using _EnterTree to make sure this code runs before any other.
	public override void _EnterTree()
		=> GD.Randomize();

    public override void _Ready()
        => Multiplayer.RootNode = this.GetWorld();
}
