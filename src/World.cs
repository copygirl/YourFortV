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
        => SpawnBlockInternal(x, y, color, unbreakable);
    [Puppet]
    public void SendBlock(int x, int y, Color color, bool unbreakable)
        => SpawnBlockInternal(x, y, color, unbreakable);

    private void SpawnBlockInternal(int x, int y, Color color, bool unbreakable)
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
    public void SpawnPlayer(int networkID, Vector2 position, string displayName, Color color)
        => SpawnPlayerInternal(networkID, position, displayName, color);
    [Puppet]
    public void SendPlayer(int networkID, Vector2 position, string displayName, Color color)
        => SpawnPlayerInternal(networkID, position, displayName, color);

    private void SpawnPlayerInternal(int networkID, Vector2 position, string displayName, Color color)
    {
        var isLocal = networkID == GetTree().GetNetworkUniqueId();
        var player  = (isLocal ? LocalPlayerScene : PlayerScene).Init<Player>();
        player.NetworkID   = networkID;
        player.Position    = position;
        player.DisplayName = displayName;
        player.Color       = color;
        PlayerContainer.AddChild(player);
    }

    [PuppetSync]
    public void Despawn(NodePath path)
    {
        var node = GetNode(path);
        node.GetParent().RemoveChild(node);
        node.QueueFree();
    }
}
