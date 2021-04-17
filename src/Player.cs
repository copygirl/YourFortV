using Godot;

public class Player : KinematicBody2D, IInitializer
{
    [Export] public NodePath DisplayNamePath { get; set; }
    [Export] public NodePath SpritePath { get; set; }

    public Label DisplayNameLabel { get; private set; }
    public Sprite Sprite { get; private set; }

    public bool IsLocal => this is LocalPlayer;

    private int _networkId = -1;
    public int NetworkID {
        get => _networkId;
        set { Name = ((_networkId = value) > 0) ? value.ToString() : "LocalPlayer"; }
    }

    public Color Color {
        get => Sprite.Modulate;
        set { Sprite.Modulate = value; }
    }

    public string DisplayName {
        get => DisplayNameLabel.Text;
        set { DisplayNameLabel.Text = value; }
    }


    public void Initialize()
    {
        DisplayNameLabel = GetNode<Label>(DisplayNamePath);
        Sprite           = GetNode<Sprite>(SpritePath);
    }

    public override void _Ready()
    {
        Initialize();
    }

    public override void _Process(float delta)
    {
        if (Network.IsAuthoratative && (Position.y > 9000)) {
            Position = Vector2.Zero;
            if (this is LocalPlayer localPlayer) localPlayer.Velocity = Vector2.Zero;
            else Network.API.SendTo(this, new PositionChangedPacket(this), TransferMode.Reliable);
        }

        if (Network.IsMultiplayerReady) {
            if (Network.IsServer) Network.API.SendToEveryoneExcept(this, new PositionChangedPacket(this));
            else if (IsLocal) Network.API.SendToServer(new MovePacket(Position));
        }
    }


    public static void RegisterPackets()
    {
        Network.API.RegisterS2CPacket<PositionChangedPacket>(packet => {
            var player = Network.GetPlayerOrThrow(packet.ID);
            player.Position = packet.Position;
            if (player is LocalPlayer localPlayer)
                localPlayer.Velocity = Vector2.Zero;
        }, TransferMode.UnreliableOrdered);

        Network.API.RegisterS2CPacket<ColorChangedPacket>(packet =>
            Network.GetPlayerOrThrow(packet.ID).Color = packet.Color);
        Network.API.RegisterS2CPacket<DisplayNameChangedPacket>(packet =>
            Network.GetPlayerOrThrow(packet.ID).DisplayName = packet.DisplayName);

        Network.API.RegisterC2SPacket<MovePacket>((player, packet) => {
            // TODO: Somewhat verify the movement of players.
            player.Position = packet.Position;
        }, TransferMode.UnreliableOrdered);

        Network.API.RegisterC2SPacket<ChangeAppearancePacket>((player, packet) =>
            ChangeAppearance(player, packet.DisplayName, packet.Color, false));
    }

    public static void ChangeAppearance(Player player,
        string displayName, Color color, bool sendPacket)
    {
        if (!sendPacket) {
            player.DisplayName = displayName;
            player.Color       = color;
            if (Network.IsServer) {
                Network.API.SendToEveryone(new DisplayNameChangedPacket(player));
                Network.API.SendToEveryone(new ColorChangedPacket(player));
            }
        } else Network.API.SendToServer(new ChangeAppearancePacket(displayName, color));
    }

    private class PositionChangedPacket
    {
        public int ID { get; }
        public Vector2 Position { get; }
        public PositionChangedPacket(Player player)
            { ID = player.NetworkID; Position = player.Position; }
    }
    private class DisplayNameChangedPacket
    {
        public int ID { get; }
        public string DisplayName { get; }
        public DisplayNameChangedPacket(Player player)
            { ID = player.NetworkID; DisplayName = player.DisplayName; }
    }
    private class ColorChangedPacket
    {
        public int ID { get; }
        public Color Color { get; }
        public ColorChangedPacket(Player player)
            { ID = player.NetworkID; Color = player.Color; }
    }

    private class MovePacket
    {
        public Vector2 Position { get; }
        public MovePacket(Vector2 position) => Position = position;
    }
    private class ChangeAppearancePacket
    {
        public string DisplayName { get; }
        public Color Color { get; }
        public ChangeAppearancePacket(string displayName, Color color)
            { DisplayName = displayName; Color = color; }
    }
}
