using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;

public class World : Node
{
    internal Node PlayerContainer { get; }
    internal Node ChunkContainer { get; }

    public IWorldGenerator Generator { get; set; } = WorldGeneratorRegistry.GetOrNull("Simple");
    public int Seed { get; set; } = unchecked((int)GD.Randi());

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
        => ChunkContainer.GetNodeOrNull<Chunk>($"Chunk ({chunkPos})");
    public Chunk GetOrCreateChunk((int X, int Y) chunkPos, bool generate = false)
        => ChunkContainer.GetOrCreateChild($"Chunk ({chunkPos})", () => {
            var chunk = new Chunk(chunkPos);
            if (generate) Generator.Generate(chunk);
            return chunk;
        });
    [PuppetSync] public void ClearChunks()
        { foreach (var chunk in Chunks) chunk.RemoveFromParent(); }


    public BlockData GetBlockDataAt(BlockPos position)
        => GetChunkOrNull(position.ToChunkPos())
            ?.GetLayerOrNull<BlockLayer>()?[position.GlobalToChunkRel()] ?? default;

    [PuppetSync]
    public void SetBlockData(int x, int y, int color)
    {
        var blockPos = new BlockPos(x, y);
        GetOrCreateChunk(blockPos.ToChunkPos())
            .GetOrCreateLayer<BlockLayer>()[blockPos.GlobalToChunkRel()]
                = (color != 0) ? new BlockData(Block.DEFAULT, color) : default;
    }

    [PuppetSync]
    public void SpawnChunk(int chunkX, int chunkY, byte[] data)
    {
        var chunk = GetOrCreateChunk((chunkX, chunkY));
        using (var stream = new MemoryStream(data)) {
            using (var reader = new BinaryReader(stream)) {
                var numLayers = reader.ReadByte();
                for (var i = 0; i < numLayers; i++) {
                    var name  = reader.ReadString();
                    var layer = chunk.GetOrCreateLayer(name);
                    layer.Read(reader);
                }
            }
        }
    }

    private static byte[] ChunkToBytes(Chunk chunk)
    {
        using (var stream = new MemoryStream()) {
            using (var writer = new BinaryWriter(stream)) {
                var layers = chunk.GetChildren()
                    .OfType<IChunkLayer>()
                    .Where(layer => !layer.IsDefault)
                    .ToArray();

                writer.Write((byte)layers.Length);
                foreach (var layer in layers) {
                    writer.Write(layer.GetType().Name);
                    layer.Write(writer);
                }
            }
            return stream.ToArray();
        }
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
                var chunk  = GetOrCreateChunk(chunkPos, generate: true);
                RPC.Reliable(player.NetworkID, SpawnChunk,
                    chunk.ChunkPosition.X, chunk.ChunkPosition.Y, ChunkToBytes(chunk));
            };
            player.VisibilityTracker.ChunkUntracked += (chunkPos) => {
                var chunk = GetChunkOrNull(chunkPos);
                if (chunk == null) return;
                RPC.Reliable(player.NetworkID, Despawn, GetPathTo(chunk), false);
            };
        }
    }

    [Puppet]
    public void SpawnHit(NodePath spritePath, Vector2 hitPosition, Color color)
    {
        var hit    = SceneCache<HitDecal>.Instance();
        var sprite = this.GetWorld().GetNode<Sprite>(spritePath);
        hit.Add(sprite, hitPosition, color);
    }

    [PuppetSync]
    public void Despawn(NodePath path, bool errorIfMissing)
    {
        var node = GetNode(path);
        if ((node == null) && !errorIfMissing) return;
        node.GetParent().RemoveChild(node);
        node.QueueFree();
    }
}
