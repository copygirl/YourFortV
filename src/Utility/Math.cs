using System;
using System.Collections.Generic;
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

public enum Facing
{
    Left,
    Right,
    Up,
    Down,
}

public static class Facings
{
    public static readonly IReadOnlyCollection<Facing> All = new []{ Facing.Left, Facing.Right, Facing.Up, Facing.Down };
    public static readonly IReadOnlyCollection<Facing> Horizontal = new []{ Facing.Left, Facing.Right };
    public static readonly IReadOnlyCollection<Facing> Vertical   = new []{ Facing.Up, Facing.Down };

    public static BlockPos ToBlockPos(this Facing facing)
    {
        switch (facing) {
            case Facing.Left:  return BlockPos.Left;
            case Facing.Right: return BlockPos.Right;
            case Facing.Up:    return BlockPos.Up;
            case Facing.Down:  return BlockPos.Down;
            default: throw new ArgumentException();
        }
    }
    public static Vector2 ToVector(this Facing facing)
        => facing.ToBlockPos().ToVector();

    public static Facing FromAngle(float radians)
    {
        radians = ((radians % (Mathf.Pi*2)) + Mathf.Pi*2) % (Mathf.Pi*2);
        if      (radians < Mathf.Pi / 4)     return Facing.Right;
        else if (radians < Mathf.Pi / 4 * 3) return Facing.Down;
        else if (radians < Mathf.Pi / 4 * 5) return Facing.Left;
        else if (radians < Mathf.Pi / 4 * 7) return Facing.Up;
        else                                 return Facing.Right;
    }

    public static float ToAngle(this Facing facing)
    {
        switch (facing) {
            case Facing.Right: return 0;
            case Facing.Down:  return Mathf.Pi / 2;
            case Facing.Left:  return Mathf.Pi;
            case Facing.Up:    return Mathf.Pi / 2 * 3;
            default: throw new ArgumentException();
        }
    }
}
