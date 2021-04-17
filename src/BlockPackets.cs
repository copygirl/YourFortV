using System.Collections.Generic;
using System.Linq;
using Godot;

public static class BlockPackets
{
    public static void Register()
    {
        Network.API.RegisterS2CPacket<SpawnBlockPacket>(OnSpawnBlockPacket);
        Network.API.RegisterS2CPacket<SpawnBlocksPacket>(OnSpawnBlocksPacket);
        Network.API.RegisterS2CPacket<DestroyBlockPacket>(OnDestroyBlockPacket);
    }

    private static void OnSpawnBlockPacket(SpawnBlockPacket packet)
    {
        // Delete any block previously at this position.
        Game.Instance.GetBlockAt(packet.Position)?.QueueFree();

        var block = Game.Instance.BlockScene.Init<Block>();
        block.Position = packet.Position;
        block.Modulate = packet.Color;
        Game.Instance.BlockContainer.AddChild(block);
    }

    private static void OnSpawnBlocksPacket(SpawnBlocksPacket packet)
    {
        Game.Instance.ClearBlocks();
        foreach (var blockInfo in packet.Blocks) {
            var block = Game.Instance.BlockScene.Init<Block>();
            block.Position = blockInfo.Position;
            block.Modulate = blockInfo.Color;
            Game.Instance.BlockContainer.AddChild(block);
        }
    }

    private static void OnDestroyBlockPacket(DestroyBlockPacket packet)
        => Game.Instance.GetBlockAt(packet.Position)?.QueueFree();
}

public class SpawnBlockPacket
{
    public Vector2 Position { get; }
    public Color Color { get; }
    public SpawnBlockPacket(Block block)
        { Position = block.Position; Color = block.Modulate; }
}

public class SpawnBlocksPacket
{
    public List<SpawnBlockPacket> Blocks { get; }
    public SpawnBlocksPacket()
        => Blocks = Game.Instance.BlockContainer.GetChildren().OfType<Block>()
            .Select(block => new SpawnBlockPacket(block)).ToList();
}

public class DestroyBlockPacket
{
    public Vector2 Position { get; }
    public DestroyBlockPacket(Block block)
        { Position = block.Position; }
}
