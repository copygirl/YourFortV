using Godot;

public class IntegratedServer : Node
{
    private SceneTree _sceneTree;
    public Server Server { get; private set; }

    public IntegratedServer()
        => Name = "IntegratedServer";

    public override void _Ready()
    {
        _sceneTree = new SceneTree();
        _sceneTree.Init();
        _sceneTree.Root.RenderTargetUpdateMode = Godot.Viewport.UpdateMode.Disabled;
        // VisualServer.ViewportSetActive(_sceneTree.Root.GetViewportRid(), false);

        var scene = GD.Load<PackedScene>("res://scene/ServerScene.tscn").Init<Server>();
        _sceneTree.Root.AddChild(scene, true);
        _sceneTree.CurrentScene = scene;
        Server = _sceneTree.Root.GetChild<Server>(0);

        // Spawn default blocks.
        var world = Server.GetWorld();
        for (var x = -6; x <= 6; x++) {
            var color = Color.FromHsv(GD.Randf(), 0.1F, 1.0F);
            world.SpawnBlock(x, 3, color, true);
        }
    }

    public override void _Process(float delta) => _sceneTree.Idle(delta);
    public override void _PhysicsProcess(float delta) => _sceneTree.Iteration(delta);
    public override void _ExitTree() => _sceneTree.Finish();
}
