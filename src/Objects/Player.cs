using Godot;

public class Player : KinematicBody2D, IInitializable
{
    [Export] public NodePath DisplayNamePath { get; set; }
    [Export] public NodePath SpritePath { get; set; }

    public Label DisplayNameLabel { get; private set; }
    public Sprite Sprite { get; private set; }
    public IItems Items { get; private set; }


    public int NetworkID { get => int.Parse(Name); set => Name = value.ToString(); }
    public string DisplayName { get => DisplayNameLabel.Text; set => DisplayNameLabel.Text = value; }
    public Color Color { get => Sprite.Modulate; set => Sprite.Modulate = value; }

    public void Initialize()
    {
        DisplayNameLabel = GetNode<Label>(DisplayNamePath);
        Sprite = GetNode<Sprite>(SpritePath);
        Items  = GetNode<IItems>("Items");

        RsetConfig("position", MultiplayerAPI.RPCMode.Puppetsync);
        RsetConfig(nameof(DisplayName), MultiplayerAPI.RPCMode.Puppetsync);
        RsetConfig(nameof(Color), MultiplayerAPI.RPCMode.Puppetsync);
    }

    public override void _Ready()
        => Visible = this.GetGame() is Client;

    public override void _Process(float delta)
    {
        if ((Position.y > 9000) && (this.GetGame() is Server))
            // Can't use RPC helper method here since player is not a LocalPlayer here.
            RpcId(NetworkID, nameof(LocalPlayer.ResetPosition), Vector2.Zero);
    }


    [Master]
    public void Move(Vector2 position)
    {
        if (GetTree().GetRpcSenderId() != NetworkID) return;
        // TODO: Somewhat verify the movement of players.

        Position = position;
        foreach (var player in this.GetWorld().Players)
            if (player != this)
                RsetId(player.NetworkID, "position", Position);
    }

    [Master]
    public void ChangeAppearance(string displayName, Color color)
    {
        if (GetTree().GetRpcSenderId() != NetworkID) return;
        // TODO: Validate input.

        Rset(nameof(DisplayName), displayName);
        Rset(nameof(Color), color);
    }
}
