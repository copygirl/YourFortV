using Godot;
using System;

public class Viewport : Node
{
    [Export(PropertyHint.Range, "0,8")] public int Scale { get; set; } = 0;
    [Export] public Vector2 PreferredScreenSize { get; set; } = new Vector2(640, 360);

    public override void _Ready()
    {
        GetTree().Connect("screen_resized", this, nameof(OnWindowResized));
        OnWindowResized();
    }

    private void OnWindowResized()
    {
        var viewport = GetViewport();

        var scale = Scale;
        if (scale <= 0) {
            var scaleX = OS.WindowSize.x / PreferredScreenSize.x;
            var scaleY = OS.WindowSize.y / PreferredScreenSize.y;
            scale      = Math.Max(1, Mathf.RoundToInt(Mathf.Min(scaleX, scaleY)));
        }

        viewport.Size = (OS.WindowSize / scale).Ceil();

        // This prevents the viewport from being "squished" to fit the window.
        // The difference is only a few pixels, but it results in distortion
        // around the center horizontal and vertical lines of the screen.
        viewport.SetAttachToScreenRect(new Rect2(0, 0, viewport.Size * scale));
    }
}
