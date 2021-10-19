using Godot;
using MessagePack;
using MessagePack.Resolvers;

public abstract class Game : Node
{
	static Game()
		=> MessagePackSerializer.DefaultOptions = StandardResolverAllowPrivate.Options;

	// Using _EnterTree to make sure this code runs before any other.
	public override void _EnterTree()
		=> GD.Randomize();

    public override void _Ready()
		=> Multiplayer.RootNode = this.GetWorld();
}
