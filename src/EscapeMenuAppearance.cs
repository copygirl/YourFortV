using System.Text.RegularExpressions;
using Godot;

public class EscapeMenuAppearance : CenterContainer
{
    [Export] public NodePath PlayerNamePath { get; set; }
    [Export] public NodePath ColorPreviewPath { get; set; }
    [Export] public NodePath ColorSliderPath { get; set; }

    public LineEdit PlayerName { get; private set; }
    public TextureRect ColorPreview { get; private set; }
    public Slider ColorSlider { get; private set; }

    public Network Network { get; private set; }
    public Player LocalPlayer { get; private set; }

    public override void _EnterTree()
    {
        PlayerName   = GetNode<LineEdit>(PlayerNamePath);
        ColorPreview = GetNode<TextureRect>(ColorPreviewPath);
        ColorSlider  = GetNode<Slider>(ColorSliderPath);

        CallDeferred(nameof(Initialize));
    }

    private void Initialize()
    {
        Network     = GetNode<Network>("/root/Game/Network");
        LocalPlayer = GetNode<Player>("/root/Game/LocalPlayer");

        ColorSlider.Value = GD.RandRange(0.0, 1.0);
        var color = Color.FromHsv((float)ColorSlider.Value, 1.0F, 1.0F);
        LocalPlayer.GetNode<Sprite>("Sprite").Modulate = color;
        ColorPreview.Modulate = color;

        Network.Connect(nameof(Network.StatusChanged), this, nameof(OnNetworkStatusChanged));
        GetTree().Connect("network_peer_connected", this, nameof(OnPeerConnected));
    }


    private void OnNetworkStatusChanged(Network.Status status)
    {
        if (status == Network.Status.ConnectedToServer)
            SendAppearance();
    }

    private void OnPeerConnected(int id)
    {
        // TODO: See if we can do something with syncing these directly?
        var name = LocalPlayer.GetNode<Label>("Name").Text;
        var hue  = LocalPlayer.GetNode<Sprite>("Sprite").Modulate.h;
        RpcId(id, nameof(AppearanceChanged), name, hue);
    }

    private void SendAppearance()
    {
        // TODO: See if we can do something with syncing these directly?
        var name = LocalPlayer.GetNode<Label>("Name").Text;
        var hue  = LocalPlayer.GetNode<Sprite>("Sprite").Modulate.h;
        Rpc(nameof(AppearanceChanged), name, hue);
    }

    [Remote]
    private void AppearanceChanged(string name, float hue)
    {
        // TODO: Clear out invalid characters from name.
        hue = Mathf.Clamp(hue, 0.0F, 1.0F);

        var id     = GetTree().GetRpcSenderId();
        var player = Network.GetOrCreatePlayerWithId(id);
        player.GetNode<Label>("Name").Text = name;
        player.GetNode<Sprite>("Sprite").Modulate = Color.FromHsv(hue, 1.0F, 1.0F);
    }


    #pragma warning disable IDE0051
    #pragma warning disable IDE1006

    private static readonly Regex INVALID_CHARS = new Regex(@"\s");
    private void _on_Name_text_changed(string text)
    {
        var validText = INVALID_CHARS.Replace(text, "");
        if (validText != text) {
            var previousCaretPos = PlayerName.CaretPosition;
            PlayerName.Text = validText;
            PlayerName.CaretPosition = previousCaretPos - (text.Length - validText.Length);
        }
    }

    private void _on_HSlider_value_changed(float value)
    {
        var color = Color.FromHsv(value, 1.0F, 1.0F);
        ColorPreview.Modulate = color;
    }

    private void _on_Appearance_visibility_changed()
    {
        if (IsVisibleInTree()) return;
        LocalPlayer.GetNode<Label>("Name").Text = PlayerName.Text;
        LocalPlayer.GetNode<Sprite>("Sprite").Modulate = ColorPreview.Modulate;
        if (GetTree().NetworkPeer != null) SendAppearance();
    }
}
