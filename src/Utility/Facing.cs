using System;
using System.Collections.Generic;
using Godot;

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
        radians = Mathf.PosMod(radians, Mathf.Tau);
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
