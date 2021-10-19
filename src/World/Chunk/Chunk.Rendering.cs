using Godot;

public partial class Chunk
{
    private MeshInstance2D _render = null;
    private StaticBody2D _collider = null;
    private bool _dirty = true;

    public override void _Process(float delta)
    {
        if (!_dirty) return;
        _dirty = false;

        var blocks = GetLayer<Block>(false);
        var colors = GetLayer<Color>(false);

        if ((this.GetGame() is Client) && (blocks != null)) {
            if (_render == null) AddChild(_render = new MeshInstance2D
                { Texture = GD.Load<Texture>("res://gfx/block.png") });
            var st = new SurfaceTool();
            st.Begin(Mesh.PrimitiveType.Triangles);

            var index = 0;
            for (var i = 0; i < LENGTH * LENGTH; i++) {
                var texture = blocks[i].Texture; // FIXME: Replace with texture index.
                if (texture == null) continue;

                var x = (float)(i  & BIT_MASK);
                var y = (float)(i >> BIT_SHIFT);

                st.AddColor(colors?[i] ?? Colors.White);
                st.AddUv(new Vector2(0, 0)); st.AddVertex(new Vector3(x - 0.5F, y - 0.5F, 0) * Block.LENGTH);
                st.AddUv(new Vector2(1, 0)); st.AddVertex(new Vector3(x + 0.5F, y - 0.5F, 0) * Block.LENGTH);
                st.AddUv(new Vector2(1, 1)); st.AddVertex(new Vector3(x + 0.5F, y + 0.5F, 0) * Block.LENGTH);
                st.AddUv(new Vector2(0, 1)); st.AddVertex(new Vector3(x - 0.5F, y + 0.5F, 0) * Block.LENGTH);

                st.AddIndex(index); st.AddIndex(index + 1); st.AddIndex(index + 2);
                st.AddIndex(index); st.AddIndex(index + 3); st.AddIndex(index + 2);
                index += 4;
            }
            _render.Mesh = st.Commit();
        }

        _collider?.RemoveFromParent();
        if (blocks?.IsDefault == false) {
            AddChild(_collider = new StaticBody2D());

            for (var i = 0; i < LENGTH * LENGTH; i++) {
                var shape = blocks[i].Shape;
                if (shape == null) continue;

                var x = (float)(i  & BIT_MASK);
                var y = (float)(i >> BIT_SHIFT);

                var ownerID = _collider.CreateShapeOwner(null);
                _collider.ShapeOwnerAddShape(ownerID, shape);
                _collider.ShapeOwnerSetTransform(ownerID, Transform2D.Identity.Translated(new Vector2(x, y) * Block.LENGTH));
            }
        } else _collider = null;
    }
}
