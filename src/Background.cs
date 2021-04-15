using Godot;

public class Background : TextureRect
{
    public override void _Process(float delta)
    {
        var offset       = new Vector2(8, 8);
        var tileSize     = Texture.GetSize();
        var viewportSize = GetViewport().Size;
        var camera       = LocalPlayer.Instance.GetNode<Camera2D>("Camera");
        RectPosition = ((camera.GlobalPosition - viewportSize / 2) / tileSize).Floor() * tileSize - offset;
        RectSize     = ((viewportSize + offset) / tileSize + Vector2.One).Ceil() * tileSize;
    }
}
