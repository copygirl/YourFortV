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
        // VisualServer.ViewportSetActive(_sceneTree.Root.GetViewportRid(), false);

        var scene = GD.Load<PackedScene>("res://scene/ServerScene.tscn").Instance<Server>();
        _sceneTree.Root.AddChild(scene, true);
        _sceneTree.CurrentScene = scene;
        Server = _sceneTree.Root.GetChild<Server>(0);

        var port = Server.StartSingleplayer();
        this.GetClient().Connect("127.0.0.1", port);
    }

    public override void _Process(float delta) => _sceneTree.Idle(delta);
    public override void _PhysicsProcess(float delta) => _sceneTree.Iteration(delta);
    public override void _ExitTree() => _sceneTree.Finish();
}
