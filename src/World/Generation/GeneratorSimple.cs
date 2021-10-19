using System;
using Godot;

public class GeneratorSimple : IWorldGenerator
{
    private int _seed;

    public string Name => "Simple";

    public void SetSeed(int seed) => _seed = seed;

    public void Generate(Chunk chunk)
    {
        if (chunk.ChunkPos.Y != 0) return;

        var rnd    = new Random(_seed ^ chunk.ChunkPos.GetHashCode());
        var blocks = chunk.GetLayer<Block>(true);
        var colors = chunk.GetLayer<Color>(true);

        for (var x = 0; x < Chunk.LENGTH; x++) {
            var grassDepth = rnd.Next(1, 4);
            var dirtDepth  = rnd.Next(6, 9);
            var stoneDepth = rnd.Next(12, 16);
            for (var y = 0; y < stoneDepth; y++) {
                var color = (y < grassDepth) ? Colors.LawnGreen
                          : (y < dirtDepth)  ? Colors.SaddleBrown
                                             : Colors.Gray;
                blocks[x, 3 + y] = Blocks.DEFAULT;
                colors[x, 3 + y] = color.Lightened(GD.Randf() * 0.15F);
            }
        }

        // TODO: Make it easier to create "structures" that cover multiple chunks.
        // TODO: These are supposed to be unbreakable.
        if (chunk.ChunkPos == (-1, 0))
            for (var x = Chunk.LENGTH - 6; x < Chunk.LENGTH; x++) {
                blocks[x, 3] = Blocks.DEFAULT;
                colors[x, 3] = Color.FromHsv(GD.Randf(), 0.1F, 1.0F);
            }
        else if (chunk.ChunkPos == (0, 0))
            for (var x = 0; x <= 6; x++) {
                blocks[x, 3] = Blocks.DEFAULT;
                colors[x, 3] = Color.FromHsv(GD.Randf(), 0.1F, 1.0F);
            }
    }
}
