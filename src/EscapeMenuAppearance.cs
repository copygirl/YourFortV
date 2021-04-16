using System.Text.RegularExpressions;
using Godot;

public class EscapeMenuAppearance : CenterContainer
{
    [Export] public NodePath DisplayNamePath { get; set; }
    [Export] public NodePath ColorPreviewPath { get; set; }
    [Export] public NodePath ColorSliderPath { get; set; }

    public LineEdit DisplayName { get; private set; }
    public TextureRect ColorPreview { get; private set; }
    public Slider ColorSlider { get; private set; }

    public override void _Ready()
    {
        DisplayName  = GetNode<LineEdit>(DisplayNamePath);
        ColorPreview = GetNode<TextureRect>(ColorPreviewPath);
        ColorSlider  = GetNode<Slider>(ColorSliderPath);

        ColorSlider.Value = GD.Randf();
        var color = Color.FromHsv((float)ColorSlider.Value, 1.0F, 1.0F);
        LocalPlayer.Instance.Color = ColorPreview.Modulate = color;
    }


    #pragma warning disable IDE0051
    #pragma warning disable IDE1006

    private static readonly Regex INVALID_CHARS = new Regex(@"\s");
    private void _on_DisplayName_text_changed(string text)
    {
        var validText = INVALID_CHARS.Replace(text, "");
        if (validText != text) {
            var previousCaretPos = DisplayName.CaretPosition;
            DisplayName.Text = validText;
            DisplayName.CaretPosition = previousCaretPos - (text.Length - validText.Length);
        }
    }

    private void _on_Hue_value_changed(float value)
    {
        var color = Color.FromHsv(value, 1.0F, 1.0F);
        ColorPreview.Modulate = color;
    }

    private void _on_Appearance_visibility_changed()
    {
        if (!IsVisibleInTree())
            Player.ChangeAppearance(LocalPlayer.Instance,
                DisplayName.Text, ColorPreview.Modulate,
                Network.IsClient);
    }
}
