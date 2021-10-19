using Godot;

public class GeneratorVoid : IWorldGenerator
{
    public string Name => "Void";

    public void SetSeed(int seed) {  }

    public void Generate(Chunk chunk)
    {
        // TODO: Make it easier to create "structures" that cover multiple chunks.
        // TODO: These are supposed to be unbreakable.
        if (chunk.ChunkPos == (-1, 0)) {
            var blocks = chunk.GetLayer<Block>(true);
            var colors = chunk.GetLayer<Color>(false);
            for (var x = Chunk.LENGTH - 6; x < Chunk.LENGTH; x++) {
                blocks[x, 3] = Blocks.DEFAULT;
                colors[x, 3] = Color.FromHsv(GD.Randf(), 0.1F, 1.0F);
            }
        } else if (chunk.ChunkPos == (0, 0)) {
            var blocks = chunk.GetLayer<Block>(true);
            var colors = chunk.GetLayer<Color>(false);
            for (var x = 0; x <= 6; x++) {
                blocks[x, 3] = Blocks.DEFAULT;
                colors[x, 3] = Color.FromHsv(GD.Randf(), 0.1F, 1.0F);
            }
        }
    }
}
