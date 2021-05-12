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
    public static BlockPos FromVector(Vector2 vec)
        => new BlockPos(Mathf.RoundToInt(vec.x / 16), Mathf.RoundToInt(vec.y / 16));

    public void Deconstruct(out int x, out int y) { x = X; y = Y; }
    public Vector2 ToVector() => new Vector2(X * 16, Y * 16);
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
    public static BlockPos operator *(BlockPos left, int right)
        => new BlockPos(left.X * right, left.Y * right);
    public static BlockPos operator /(BlockPos left, int right)
        => new BlockPos(left.X / right, left.Y / right);
}
