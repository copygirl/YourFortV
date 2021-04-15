using Godot;

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
        set {
            _networkId = value;
            Name = (_networkId > 0) ? value.ToString() : "LocalPlayer";
        }
    }

    public Color Color {
        get => Sprite.Modulate;
        set {
            Sprite.Modulate = value;
            Sprite.Rset(NetworkId, "modulate", nameof(OnColorChanged), value);
        }
    }

    public string DisplayName {
        get => DisplayNameLabel.Text;
        set {
            DisplayNameLabel.Text = value;
            DisplayNameLabel.Rset(NetworkId, "text", nameof(OnDisplayNameChanged), value);
        }
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
        => this.RsetUnreliable(NetworkId, "position", nameof(OnPositionChanged), Position);

    [Master]
    private void OnPositionChanged(Vector2 value)
        { if (GetTree().GetRpcSenderId() == NetworkId) Position = value; }

    [Master]
    private void OnColorChanged(Color value)
        { if (GetTree().GetRpcSenderId() == NetworkId) Color = value; }

    [Master]
    private void OnDisplayNameChanged(string value)
        { if (GetTree().GetRpcSenderId() == NetworkId) DisplayName = value; }
}
