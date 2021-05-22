using System;
using Godot;

public readonly struct BlockPos : IEquatable<BlockPos>
{
    public static readonly BlockPos Zero  = new BlockPos( 0,  0);
    public static readonly BlockPos One   = new BlockPos( 1,  1);

    public static readonly BlockPos Left  = new BlockPos(-1,  0);
    public static readonly BlockPos Right = new BlockPos( 1,  0);
    public static readonly BlockPos Up    = new BlockPos( 0, -1);
    public static readonly BlockPos Down  = new BlockPos( 0,  1);

    public int X { get; }
    public int Y { get; }

    public BlockPos(int x, int y) { X = x; Y = y; }
    public static BlockPos FromVector(Vector2 vec) => new BlockPos(
        Mathf.RoundToInt(vec.x / Block.LENGTH), Mathf.RoundToInt(vec.y / Block.LENGTH));

    public BlockPos Add(int x, int y) => new BlockPos(X + x, Y + y);
    public BlockPos Add(Facing facing, int distance = 1)
        { var (x, y) = facing; return Add(x * distance, y * distance); }

    public BlockPos Subtract(int x, int y) => new BlockPos(X - x, Y - y);
    public BlockPos Subtract(Facing facing, int distance = 1)
        { var (x, y) = facing; return Subtract(x * distance, y * distance); }

    public BlockPos GlobalToChunkRel()
        => new BlockPos(X & Chunk.BIT_MASK, Y & Chunk.BIT_MASK);
    public BlockPos ChunkRelToGlobal((int X, int Y) chunkPos)
        => new BlockPos(chunkPos.X << Chunk.BIT_SHIFT | X, chunkPos.Y << Chunk.BIT_SHIFT | Y);

    public void Deconstruct(out int x, out int y) { x = X; y = Y; }
    public Vector2 ToVector() => new Vector2(X << Block.BIT_SHIFT, Y << Block.BIT_SHIFT);
    public (int X, int Y) ToChunkPos() => (X >> Chunk.BIT_SHIFT, Y >> Chunk.BIT_SHIFT);
    public override string ToString() => $"({X}, {Y})";

    public override bool Equals(object obj) => (obj is BlockPos other) && Equals(other);
    public bool Equals(BlockPos other) => (other.X == X) && (other.Y == Y);

    public override int GetHashCode()
    {
        int hashCode = 1861411795;
        hashCode = hashCode * -1521134295 + X.GetHashCode();
        hashCode = hashCode * -1521134295 + Y.GetHashCode();
        return hashCode;
    }

    public static bool operator ==(BlockPos left, BlockPos right) => left.Equals(right);
    public static bool operator !=(BlockPos left, BlockPos right) => !left.Equals(right);

    public static BlockPos operator -(BlockPos value)
        => new BlockPos(-value.X, -value.Y);
    public static BlockPos operator +(BlockPos left, BlockPos right)
        => new BlockPos(left.X + right.X, left.Y + right.Y);
    public static BlockPos operator -(BlockPos left, BlockPos right)
        => new BlockPos(left.X - right.X, left.Y - right.Y);
}
