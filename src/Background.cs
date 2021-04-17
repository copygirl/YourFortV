using Godot;

public class Background : TextureRect
{
    public override void _Process(float delta)
    {
        var offset   = new Vector2(8, 8);
        var tileSize = Texture.GetSize();
        RectPosition = (-GetViewportTransform().origin / tileSize).Floor() * tileSize - offset;
        RectSize     = ((GetViewport().Size + offset) / tileSize + Vector2.One).Ceil() * tileSize;
    }
}
