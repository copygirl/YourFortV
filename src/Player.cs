using Godot;

// FIXME: Player name should not be stored in "Name".
public class Player : KinematicBody2D, IInitializer
{
    [Export] public NodePath DisplayNamePath { get; set; }
    [Export] public NodePath SpritePath { get; set; }

    public Label DisplayNameLabel { get; private set; }
    public Sprite Sprite { get; private set; }
    public Network Network { get; private set; }

    public bool IsLocal => this is LocalPlayer;

    private int _networkId = -1;
    public int NetworkId {
        get => _networkId;
        set => SetNetworkId(value);
    }

    public Color Color {
        get => Sprite.Modulate;
        set => SetColor(value);
    }

    public string DisplayName {
        get => DisplayNameLabel.Text;
        set => SetDisplayName(value);
    }


    public void Initialize()
    {
        DisplayNameLabel = GetNode<Label>(DisplayNamePath);
        Sprite           = GetNode<Sprite>(SpritePath);
    }

    public override void _Ready()
    {
        Initialize();
        Network = GetNode<Network>("/root/Game/Network");

        RsetConfig("position", MultiplayerAPI.RPCMode.Puppetsync);
        Sprite.RsetConfig("modulate", MultiplayerAPI.RPCMode.Puppetsync);
        DisplayNameLabel.RsetConfig("text", MultiplayerAPI.RPCMode.Puppetsync);
    }

    public override void _Process(float delta)
    {
        if (GetTree().NetworkPeer != null) {
            // TODO: Only send position if it changed.
            // Send unreliable messages while moving, and a reliable once the player stopped.
            if (GetTree().IsNetworkServer())
                this.RsetUnreliableExcept(NetworkId, "position", Position);
            else if (Network.Status == NetworkStatus.ConnectedToServer)
                RpcUnreliable(nameof(OnPositionChanged), Position);
        }
    }
    [Master]
    private void OnPositionChanged(Vector2 value)
        { if (GetTree().GetRpcSenderId() == NetworkId) Position = value; }


    private void SetNetworkId(int value)
    {
        _networkId = value;
        Name = (_networkId > 0) ? value.ToString() : "LocalPlayer";
    }

    private void SetColor(Color value)
    {
        Sprite.Modulate = value;
        if (IsInsideTree() && GetTree().NetworkPeer != null) {
            if (GetTree().IsNetworkServer()) Sprite.RsetExcept(NetworkId, "modulate", value);
            else Rpc(nameof(OnColorChanged), value);
        }
    }
    [Master]
    private void OnColorChanged(Color value)
        { if (GetTree().GetRpcSenderId() == NetworkId) Color = value; }

    private void SetDisplayName(string value)
    {
        DisplayNameLabel.Text = value;
        if (IsInsideTree() && GetTree().NetworkPeer != null) {
            if (GetTree().IsNetworkServer()) DisplayNameLabel.RsetExcept(NetworkId, "text", value);
            else Rpc(nameof(OnDisplayNameChanged), value);
        }
    }
    [Master]
    private void OnDisplayNameChanged(string value)
        { if (GetTree().GetRpcSenderId() == NetworkId) DisplayName = value; }
}
