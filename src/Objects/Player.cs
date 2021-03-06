using System;
using Godot;

public class Player : KinematicBody2D, IInitializable
{
    private static readonly TimeSpan TIME_BEFORE_REGEN = TimeSpan.FromSeconds(1.0);
    private static readonly TimeSpan REGEN_TIMER = TimeSpan.FromSeconds(1.0 / 3);
    private static readonly float REGEN_AMOUNT = 0.025F;
    private static readonly TimeSpan RESPAWN_TIMER = TimeSpan.FromSeconds(5);

    [Export] public NodePath DisplayNamePath { get; set; }
    [Export] public NodePath SpritePath { get; set; }

    public Label DisplayNameLabel { get; private set; }
    public Sprite Sprite { get; private set; }
    public IItems Items { get; private set; }

    public bool IsLocal { get; private set; }
    public int NetworkID { get; private set; }
    public string DisplayName { get => DisplayNameLabel.Text; set => DisplayNameLabel.Text = value; }
    public Color Color { get => Sprite.SelfModulate; set => Sprite.SelfModulate = value; }

    public Vector2 Velocity { get; set; }

    public float Health { get; set; } = 1.0F;
    public bool IsAlive => Health > 0.0F;
    private float _previousHealth;
    private float _regenDelay;
    private float _respawnDelay;

    public PlayerVisibilityTracker VisibilityTracker { get; } = new PlayerVisibilityTracker();

    public void Initialize()
    {
        DisplayNameLabel = GetNode<Label>(DisplayNamePath);
        Sprite = GetNode<Sprite>(SpritePath);
        Items  = GetNode<IItems>("Items");

        RsetConfig("position", MultiplayerAPI.RPCMode.Puppetsync);
        RsetConfig("modulate", MultiplayerAPI.RPCMode.Puppetsync);
        RsetConfig(nameof(DisplayName), MultiplayerAPI.RPCMode.Puppetsync);
        RsetConfig(nameof(Color), MultiplayerAPI.RPCMode.Puppetsync);
        RsetConfig(nameof(Velocity), MultiplayerAPI.RPCMode.Puppet);
        RsetConfig(nameof(Health), MultiplayerAPI.RPCMode.Puppet);
    }

    internal void SetNetworkID(bool isLocal, int networkID)
    {
        IsLocal   = isLocal;
        NetworkID = networkID;
        Name      = networkID.ToString();
    }

    public override void _Ready()
        => Visible = this.GetGame() is Client;

    public override void _Process(float delta)
    {
        if (!(this.GetGame() is Server)) return;

        // Damage player when falling into "the void", so they can respawn.
        if (Position.y > 9000) Health -= 0.01F;

        if (IsAlive && (Health < 1.0F)) {
            if ((_regenDelay += delta) > (TIME_BEFORE_REGEN + REGEN_TIMER).TotalSeconds) {
                _regenDelay -= (float)REGEN_TIMER.TotalSeconds;
                Health = Mathf.Min(1.0F, Health + REGEN_AMOUNT);
            }
        } else _regenDelay = 0.0F;

        if (!IsAlive && ((_respawnDelay += delta) > RESPAWN_TIMER.TotalSeconds)) {
            // TODO: Move respawning related code to its own method.
            RsetId(NetworkID, "position", Vector2.Zero);
            RsetId(NetworkID, nameof(Velocity), Vector2.Zero);
            Rset("modulate", Colors.White);
            Health        = 1.0F;
            _respawnDelay = 0.0F;
            // TODO: Add invulnerability timer? Or some other way to prevent "void" damage
            //       after server considers player respawned, but it hasn't teleported yet.
        }

        if (_previousHealth != Health) {
            RsetId(NetworkID, nameof(Health), Health);
            if (Health < _previousHealth) _regenDelay = 0.0F;
            if ((Health <= 0) && (_previousHealth > 0))
                Rset("modulate", new Color(0.35F, 0.35F, 0.35F, 0.8F));
            _previousHealth = Health;
        }

        VisibilityTracker.Process(this);
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
        if (displayName == null) return;
        // TODO: Verify display name some more.
        if (color.a < 1.0F) return;

        Rset(nameof(DisplayName), displayName);
        Rset(nameof(Color), color);
    }
}
