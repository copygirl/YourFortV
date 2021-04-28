using System;
using Godot;

// TODO: Maybe figure out how we can make different classes (LocalPlayer, NPCPlayer) synchronizable.
[SyncObject("Player", "World/Players")]
public class Player : KinematicBody2D, IInitializer
{
    [Export] public NodePath DisplayNamePath { get; set; }
    [Export] public NodePath SpritePath { get; set; }

    public Label DisplayNameLabel { get; private set; }
    public Sprite Sprite { get; private set; }


    public bool IsLocal { get; private set; }

    [SyncProperty]
    public new Vector2 Position {
        get => base.Position;
        set { if (!IsLocal) base.Position = this.SetSync(value); }
    }

    [SyncProperty]
    public Color Color {
        get => Sprite.Modulate;
        set => Sprite.Modulate = this.SetSync(value);
    }

    [SyncProperty]
    public string DisplayName {
        get => DisplayNameLabel.Text;
        set => DisplayNameLabel.Text = this.SetSync(value);
    }


    // TODO: Implement "low jumps" activated by releasing the jump button early.
    public TimeSpan JumpEarlyTime { get; } = TimeSpan.FromSeconds(0.2F);
    public TimeSpan JumpCoyoteTime { get; } = TimeSpan.FromSeconds(0.2F);

    public float MovementSpeed { get; set; } = 160;
    public float JumpVelocity { get; set; } = 240;
    public float Gravity { get; set; } = 480;

    public float Acceleration { get; set; } = 0.25F;
    public float GroundFriction { get; set; } = 0.2F;
    public float AirFriction { get; set; } = 0.05F;

    public Vector2 Velocity = Vector2.Zero;
    private DateTime? _jumpPressed = null;
    private DateTime? _lastOnFloor = null;


    public void Initialize()
    {
        DisplayNameLabel = GetNode<Label>(DisplayNamePath);
        Sprite           = GetNode<Sprite>(SpritePath);
    }

    public override void _Process(float delta)
    {
        if ((Position.y > 9000) && (this.GetGame() is Server server))
            this.RPC(new []{ server.GetNetworkID(this) }, ResetPosition, Vector2.Zero);

        this.GetClient()?.RPC(Move, Position);
    }

    public override void _PhysicsProcess(float delta)
    {
        if (!IsLocal) return;

        var moveDir     = 0.0F;
        var jumpPressed = false;
        if (!EscapeMenu.Instance.Visible) {
            moveDir     = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
            jumpPressed = Input.IsActionJustPressed("move_jump");
        }

        var friction = IsOnFloor() ? GroundFriction : AirFriction;
        Velocity.x = (moveDir != 0) ? Mathf.Lerp(Velocity.x, moveDir * MovementSpeed, Acceleration)
                                    : Mathf.Lerp(Velocity.x, 0, friction);
        Velocity.y += Gravity * delta;
        Velocity = MoveAndSlide(Velocity, Vector2.Up);

        if (jumpPressed) _jumpPressed = DateTime.Now;
        if (IsOnFloor()) _lastOnFloor = DateTime.Now;

        if (((DateTime.Now - _jumpPressed) <= JumpEarlyTime) &&
            ((DateTime.Now - _lastOnFloor) <= JumpCoyoteTime)) {
            Velocity.y  = -JumpVelocity;
            _jumpPressed = null;
            _lastOnFloor = null;
        }
    }


    [RPC(PacketDirection.ServerToClient)]
    public void SetLocal()
    {
        IsLocal = true;
        GetNode<Camera2D>("Camera").Current = true;
    }

    [RPC(PacketDirection.ServerToClient)]
    private void ResetPosition(Vector2 position)
    {
        base.Position = position;
        Velocity      = Vector2.Zero;
    }

    [RPC(PacketDirection.ClientToServer)]
    private static void Move(Server server, NetworkID networkID, Vector2 position)
    {
        // TODO: Somewhat verify the movement of players.
        var player = server.GetPlayer(networkID);
        player.Position = position;
    }

    [RPC(PacketDirection.ClientToServer)]
    public static void ChangeAppearance(Server server, NetworkID networkID, string displayName, Color color)
    {
        // TODO: Validate input.
        var player = server.GetPlayer(networkID);
        player.DisplayName = displayName;
        player.Color       = color;
    }
}
