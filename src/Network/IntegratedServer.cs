using Godot;

public class IntegratedServer : Node
{
    private SceneTree _sceneTree;
    public Server Server { get; private set; }

    public override void _Ready()
    {
        _sceneTree = new SceneTree();
        _sceneTree.Init();
        _sceneTree.Root.RenderTargetUpdateMode = Godot.Viewport.UpdateMode.Disabled;

        var scene = GD.Load<PackedScene>("res://scene/ServerScene.tscn").Init<Server>();
        _sceneTree.Root.AddChild(scene);
        _sceneTree.CurrentScene = scene;

        Server = _sceneTree.Root.GetChild<Server>(0);
        // Spawn default blocks.
        for (var x = -6; x <= 6; x++) {
            var block = Server.Spawn<Block>();
            block.Position    = new BlockPos(x, 3);
            block.Color       = Color.FromHsv(GD.Randf(), 0.1F, 1.0F);
            block.Unbreakable = true;
        }
    }

    public override void _Process(float delta) => _sceneTree.Idle(delta);
    public override void _PhysicsProcess(float delta) => _sceneTree.Iteration(delta);
    public override void _ExitTree() => _sceneTree.Finish();
}
