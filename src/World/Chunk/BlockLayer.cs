using System.IO;
using Godot;

public readonly struct BlockData
{
    public Block Block { get; }  // TODO: Replace with 2-byte or smaller integer identifier?
    public int RawColor { get; } // TODO: Replace with smaller representation?
                                 // Perhaps we can fit this into 4 bytes total?
    public Color Color => new Color(RawColor);
    public BlockData(Block block, int rawColor) { Block = block; RawColor = rawColor; }
    public BlockData(Block block, Color color) { Block = block; RawColor = color.ToRgba32(); }
}

public class BlockLayer : BasicChunkLayer<BlockData>
{
    private MeshInstance2D _render = null;
    private StaticBody2D _collider = null;

    public override void _Process(float delta)
    {
        if (!Dirty) return;
        Dirty = false;

        var st = (SurfaceTool)null;
        if (this.GetGame() is Client) {
            if (_render == null) AddChild(_render = new MeshInstance2D
                { Texture = GD.Load<Texture>("res://gfx/block.png") });
            st = new SurfaceTool();
            st.Begin(Mesh.PrimitiveType.Triangles);
        }

        _collider?.RemoveFromParent();
        if (IsDefault) _collider = null;
        else AddChild(_collider = new StaticBody2D());

        var size  = Block.LENGTH;
        var index = 0;
        for (var i = 0; i < Chunk.LENGTH * Chunk.LENGTH; i++) {
            var data = Data[i];
            if (data.Block == null) continue;

            var x = (float)(i  & Chunk.BIT_MASK);
            var y = (float)(i >> Chunk.BIT_SHIFT);

            if (_render != null) {
                st.AddColor(data.Color);
                st.AddUv(new Vector2(0, 0)); st.AddVertex(new Vector3(x - 0.5F, y - 0.5F, 0) * size);
                st.AddUv(new Vector2(1, 0)); st.AddVertex(new Vector3(x + 0.5F, y - 0.5F, 0) * size);
                st.AddUv(new Vector2(1, 1)); st.AddVertex(new Vector3(x + 0.5F, y + 0.5F, 0) * size);
                st.AddUv(new Vector2(0, 1)); st.AddVertex(new Vector3(x - 0.5F, y + 0.5F, 0) * size);

                st.AddIndex(index); st.AddIndex(index + 1); st.AddIndex(index + 2);
                st.AddIndex(index); st.AddIndex(index + 3); st.AddIndex(index + 2);
                index += 4;
            }

            var ownerID = _collider.CreateShapeOwner(null);
            _collider.ShapeOwnerAddShape(ownerID, data.Block.Shape);
            _collider.ShapeOwnerSetTransform(ownerID, Transform2D.Identity.Translated(new Vector2(x, y) * size));
        }

        if (_render != null)
            _render.Mesh = st.Commit();
    }

    public override void Read(BinaryReader reader)
    {
        NonDefaultCount = 0;
        for (var i = 0; i < Chunk.LENGTH * Chunk.LENGTH; i++) {
            var color = reader.ReadInt32();
            if (color == 0) continue;
            Data[i] = new BlockData(Block.DEFAULT, color);
            NonDefaultCount++;
        }
        Dirty = true;
    }

    public override void Write(BinaryWriter writer)
    {
        for (var i = 0; i < Chunk.LENGTH * Chunk.LENGTH; i++)
            writer.Write(Data[i].RawColor); // Is 0 if block is not set.
    }
}
