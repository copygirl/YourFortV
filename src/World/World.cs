using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Godot;
using MessagePack;
using File = System.IO.File;

public partial class World : Node
{
    public const string FILE_EXT     = ".yf5";
    public const string MAGIC_NUMBER = "YF5s"; // 0x59463573

    public static readonly string WORLDS_DIR = OS.GetUserDataDir() + "/worlds/";


    internal Node PlayerContainer { get; }
    internal Node ChunkContainer { get; }

    public DateTime LastSaved { get; set; }
    public TimeSpan Playtime { get; set; } = TimeSpan.Zero;
    public int Seed { get; set; } = unchecked((int)GD.Randi());
    public IWorldGenerator Generator { get; set; } = WorldGeneratorRegistry.GetOrNull("Simple");

    public BlockRef this[BlockPos pos] => new BlockRef(this, pos);
    public BlockRef this[int x, int y] => new BlockRef(this, new BlockPos(x, y));

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
    public Chunk GetChunk((int X, int Y) chunkPos, bool create) => !create
        ? ChunkContainer.GetNodeOrNull<Chunk>($"Chunk ({chunkPos})")
        : ChunkContainer.GetOrCreateChild($"Chunk ({chunkPos})", () => {
            var chunk = new Chunk(chunkPos);
            if (this.GetGame() is Server)
                Generator.Generate(chunk);
            return chunk;
        });
    [PuppetSync] public void ClearChunks()
        { foreach (var chunk in Chunks) chunk.RemoveFromParent(); }


    [PuppetSync]
    public void SetBlock(int x, int y, int color)
    {
        var block = this[x, y];
        block.Set((color != 0) ? Blocks.DEFAULT : Blocks.AIR);
        block.Set(new Color(color));
    }

    [PuppetSync]
    public void SpawnChunk(byte[] data)
    {
        var chunk = MessagePackSerializer.Deserialize<Chunk>(data);
        ChunkContainer.GetNodeOrNull<Chunk>($"Chunk ({chunk.ChunkPos})")?.RemoveFromParent();
        ChunkContainer.AddChild(chunk);
    }

    [PuppetSync]
    public void SpawnPlayer(int networkID, Vector2 position)
    {
        var player  = SceneCache<Player>.Instance();
        var isLocal = networkID == GetTree().GetNetworkUniqueId();
        player.SetNetworkID(isLocal, networkID);
        player.Position  = position;
        PlayerContainer.AddChild(player);

        if (isLocal) {
            player.AddChild(new PlayerMovement { Name = "PlayerMovement" });
            player.AddChild(new Camera2D { Name = "Camera", Current = true });
            this.GetClient().FireLocalPlayerSpawned(player);
        }

        if (this.GetGame() is Server) {
            player.VisibilityTracker.ChunkTracked += (chunkPos) =>
                RPC.Reliable(player.NetworkID, SpawnChunk,
                    MessagePackSerializer.Serialize(GetChunk(chunkPos, true)));

            player.VisibilityTracker.ChunkUntracked += (chunkPos) => {
                var chunk = GetChunk(chunkPos, false);
                if (chunk == null) return;
                RPC.Reliable(player.NetworkID, Despawn, GetPathTo(chunk), false);
            };
        }
    }

    [Puppet]
    public void SpawnHit(NodePath path, Vector2 hitPosition, Color color)
        => HitDecal.Spawn(this.GetWorld(), path, hitPosition, color);

    [PuppetSync]
    public void Despawn(NodePath path, bool errorIfMissing)
    {
        var node = GetNode(path);
        if ((node == null) && !errorIfMissing) return;
        node.GetParent().RemoveChild(node);
        node.QueueFree();
    }


    public void Save(string path)
    {
        using (var stream = File.OpenWrite(path + ".tmp")) {
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true)) {
                writer.Write(MAGIC_NUMBER.ToCharArray());

                // TODO: Eventually, write only "header", not chunks.
                //       Chunks should be stored seperately, in regions or so.
                var bytes = this.SerializeToBytes();
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
        }
        new Godot.Directory().Rename(path + ".tmp", path);
        LastSaved = File.GetLastWriteTime(path);
    }

    public void Load(string path)
    {
        using (var stream = File.OpenRead(path)) {
            using (var reader = new BinaryReader(stream, Encoding.UTF8, true)) {
                var magic = new string(reader.ReadChars(MAGIC_NUMBER.Length));
                if (magic != MAGIC_NUMBER) throw new IOException(
                    $"Magic number does not match ({magic:X8} != {MAGIC_NUMBER:X8})");

                var numBytes = reader.ReadInt32();
                var bytes    = reader.ReadBytes(numBytes);
                this.Deserialize(bytes);
            }
        }
        LastSaved = File.GetLastAccessTime(path);
    }
}
