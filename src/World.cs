using System.Collections.Generic;
using System.Linq;
using Godot;

public class World : Node
{
    [Export] public NodePath PlayerContainerPath { get; set; }
    [Export] public NodePath BlockContainerPath { get; set; }

    public Node PlayerContainer { get; private set; }
    public Node BlockContainer { get; private set; }

    public PackedScene BlockScene { get; private set; }
    public PackedScene PlayerScene { get; private set; }
    public PackedScene LocalPlayerScene { get; private set; }
    private static readonly PackedScene HIT_DECAL = GD.Load<PackedScene>("res://scene/HitDecal.tscn");
    // TODO: Make all of these static and readonly, hardcode the values..?

    public override void _Ready()
    {
        PlayerContainer = GetNode(PlayerContainerPath);
        BlockContainer  = GetNode(BlockContainerPath);

        BlockScene       = GD.Load<PackedScene>("res://scene/Block.tscn");
        PlayerScene      = GD.Load<PackedScene>("res://scene/Player.tscn");
        LocalPlayerScene = GD.Load<PackedScene>("res://scene/LocalPlayer.tscn");
    }

    public IEnumerable<Player> Players
        => PlayerContainer.GetChildren().Cast<Player>();
    public Player GetPlayer(int networkID)
        => PlayerContainer.GetNodeOrNull<Player>(networkID.ToString());
    public void ClearPlayers()
        { foreach (var player in Players) player.RemoveFromParent(); }

    public IEnumerable<Block> Blocks
        => BlockContainer.GetChildren().Cast<Block>();
    public Block GetBlockAt(BlockPos position)
        => BlockContainer.GetNodeOrNull<Block>(position.ToString());
    [PuppetSync] public void ClearBlocks()
        { foreach (var block in Blocks) block.RemoveFromParent(); }


    [PuppetSync]
    public void SpawnBlock(int x, int y, Color color, bool unbreakable)
    {
        var position = new BlockPos(x, y);
        var block    = BlockScene.Init<Block>();
        block.Name        = position.ToString();
        block.Position    = position;
        block.Color       = color;
        block.Unbreakable = unbreakable;
        BlockContainer.AddChild(block);
    }

    [PuppetSync]
    public void SpawnPlayer(int networkID, Vector2 position)
    {
        var isLocal = networkID == GetTree().GetNetworkUniqueId();
        var player  = (isLocal ? LocalPlayerScene : PlayerScene).Init<Player>();
        player.NetworkID = networkID;
        player.Position  = position;
        PlayerContainer.AddChild(player);

        if (player is LocalPlayer localPlayer)
            this.GetClient().FireLocalPlayerSpawned(localPlayer);
    }

    [Puppet]
    public void SpawnHit(NodePath spritePath, Vector2 hitPosition, Color color)
    {
        var hit    = HIT_DECAL.Init<HitDecal>();
        var sprite = this.GetWorld().GetNode<Sprite>(spritePath);
        hit.Add(sprite, hitPosition, color);
    }

    [PuppetSync]
    public void Despawn(NodePath path)
    {
        var node = GetNode(path);
        node.GetParent().RemoveChild(node);
        node.QueueFree();
    }
}
