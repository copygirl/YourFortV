using System.Collections.Generic;
using Godot;

public class World : Node
{
    private static readonly PackedScene BLOCK        = GD.Load<PackedScene>("res://scene/Block.tscn");
    private static readonly PackedScene PLAYER       = GD.Load<PackedScene>("res://scene/Player.tscn");
    private static readonly PackedScene LOCAL_PLAYER = GD.Load<PackedScene>("res://scene/LocalPlayer.tscn");
    private static readonly PackedScene HIT_DECAL    = GD.Load<PackedScene>("res://scene/HitDecal.tscn");

    internal Node PlayerContainer { get; }
    internal Node ChunkContainer { get; }

    public World()
    {
        AddChild(PlayerContainer = new Node { Name = "Players" });
        AddChild(ChunkContainer  = new Node { Name = "Chunks" });
    }

    public IEnumerable<Player> Players
        => PlayerContainer.GetChildren<Player>();
    public Player GetPlayer(int networkID)
        => PlayerContainer.GetNodeOrNull<Player>(networkID.ToString());
    public void ClearPlayers()
        { foreach (var player in Players) player.RemoveFromParent(); }

    public IEnumerable<Chunk> Chunks
        => ChunkContainer.GetChildren<Chunk>();
    public Chunk GetChunkOrNull((int X, int Y) chunkPos)
        => ChunkContainer.GetNodeOrNull<Chunk>($"Chunk ({chunkPos.X}, {chunkPos.Y})");
    public Chunk GetOrCreateChunk((int X, int Y) chunkPos)
        => ChunkContainer.GetOrCreateChild($"Chunk ({chunkPos.X}, {chunkPos.Y})", () => new Chunk(chunkPos.X, chunkPos.Y));
    [PuppetSync] public void ClearChunks()
        { foreach (var chunk in Chunks) chunk.RemoveFromParent(); }


    public Block GetBlockAt(BlockPos position)
        => GetChunkOrNull(position.ToChunkPos())
            ?.GetLayerOrNull<Block>()?[position.GlobalToChunkRel()];
    [PuppetSync]
    public void SpawnBlock(int x, int y, Color color, bool unbreakable)
    {
        var blockPos = new BlockPos(x, y);
        var block    = BLOCK.Init<Block>();
        block.Name        = blockPos.ToString();
        block.Color       = color;
        block.Unbreakable = unbreakable;
        block.ChunkLocalBlockPos = blockPos.GlobalToChunkRel();

        GetOrCreateChunk(blockPos.ToChunkPos())
            .GetOrCreateLayer<Block>()[block.ChunkLocalBlockPos] = block;
    }
    [PuppetSync]
    public void DespawnBlock(int x, int y)
    {
        var blockPos   = new BlockPos(x, y);
        var blockLayer = GetChunkOrNull(blockPos.ToChunkPos())?.GetLayerOrNull<Block>();
        if (blockLayer != null) blockLayer[blockPos.GlobalToChunkRel()] = null;
    }

    [PuppetSync]
    public void SpawnPlayer(int networkID, Vector2 position)
    {
        var player = SceneCache<Player>.Instance();
        player.NetworkID = networkID;
        player.Position  = position;
        PlayerContainer.AddChild(player);

        if (player.IsLocal) {
            player.AddChild(new PlayerMovement { Name = "PlayerMovement" });
            player.AddChild(new Camera2D { Name = "Camera", Current = true });
            this.GetClient().FireLocalPlayerSpawned(player);
        }

        if (this.GetGame() is Server) {
            player.VisibilityTracker.ChunkTracked += (chunkPos) => {
                var chunk = GetChunkOrNull(chunkPos);
                if (chunk == null) return;
                foreach (var block in chunk.GetLayerOrNull<Block>().GetChildren<Block>())
                    RPC.Reliable(player.NetworkID, SpawnBlock,
                        block.GlobalBlockPos.X, block.GlobalBlockPos.Y,
                        block.Color, block.Unbreakable);
            };
            player.VisibilityTracker.ChunkUntracked += (chunkPos) => {
                var chunk = GetChunkOrNull(chunkPos);
                if (chunk == null) return;
                RPC.Reliable(player.NetworkID, Despawn, GetPathTo(chunk));
            };
        }
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
