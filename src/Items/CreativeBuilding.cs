using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class CreativeBuilding : Node2D
{
    private enum BuildMode
    {
        Placing,
        Breaking,
    }

    [Export] public int MaxLength { get; set; } = 6;

    private Texture _blockTex;

    public Cursor Cursor { get; private set; }
    public Player Player { get; private set; }

    private BlockPos _startPos;
    private Facing _direction;
    private int _length;
    private bool _canBuild;

    private BuildMode? _currentMode = null;

    public override void _Ready()
    {
        _blockTex = GD.Load<Texture>("res://gfx/block.png");

        Cursor = this.GetClient()?.Cursor;
        Player = GetParent().GetParent<Player>();
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (!Visible || !(Player is LocalPlayer)) return;

        if (ev.IsActionPressed("interact_place")) {
            GetTree().SetInputAsHandled();
            _currentMode = (((_currentMode == null) && _canBuild) ? BuildMode.Placing : (BuildMode?)null);
        }
        if (ev.IsActionPressed("interact_break")) {
            GetTree().SetInputAsHandled();
            _currentMode = ((_currentMode == null) ? BuildMode.Breaking : (BuildMode?)null);
        }
        // NOTE: These ternary operations require extra brackets for some
        //       reason or else the syntax highlighting in VS Code breaks?!
    }

    public override void _Process(float delta)
    {
        if (!(Player is LocalPlayer)) return;
        if (!Visible) _currentMode = null;

        if (_currentMode == BuildMode.Placing) {
            if (!_canBuild) _currentMode = null;
            else if (!Input.IsActionPressed("interact_place")) {
                RpcId(1, nameof(PlaceLine), _startPos.X, _startPos.Y, _direction, _length);
                _currentMode = null;
            }
        }

        if (_currentMode == BuildMode.Breaking) {
            if (!Input.IsActionPressed("interact_break")) {
                RpcId(1, nameof(BreakLine), _startPos.X, _startPos.Y, _direction, _length);
                _currentMode = null;
            }
        }

        if (_currentMode != null) {
            var start  = _startPos.ToVector();
            var angle  = Cursor.Position.AngleToPoint(start); // angle_to_point appears reversed.
            _direction = Facings.FromAngle(angle);
            _length    = Math.Min(MaxLength, Mathf.RoundToInt(start.DistanceTo(Cursor.Position) / 16));
        } else {
            _startPos = BlockPos.FromVector(Cursor.Position);
            _length   = 0;
        }

        var world = this.GetWorld();
        bool IsBlockAt(BlockPos pos) => world.GetBlockAt(pos) != null;
        _canBuild = !IsBlockAt(_startPos) && Facings.All.Any(pos => IsBlockAt(_startPos + pos.ToBlockPos()));

        Update(); // Make sure _Draw is being called.
    }

    public override void _Draw()
    {
        if (!Cursor.Visible || EscapeMenu.Instance.Visible) return;

        var green = Color.FromHsv(1.0F / 3, 1.0F, 1.0F, 0.4F);
        var red   = Color.FromHsv(0.0F, 1.0F, 1.0F, 0.4F);
        var black = new Color(0.0F, 0.0F, 0.0F, 0.65F);

        var world = this.GetWorld();
        foreach (var pos in GetBlockPositions(_startPos, _direction, _length)) {
            var hasBlock = world.GetBlockAt(pos) != null;
            var color    = (_currentMode != BuildMode.Breaking)
                ? ((_canBuild && !hasBlock) ? green : red)
                : (hasBlock ? black : red);
            DrawTexture(_blockTex, ToLocal(pos.ToVector() - _blockTex.GetSize() / 2), color);
        }
    }

    private static IEnumerable<BlockPos> GetBlockPositions(BlockPos start, Facing direction, int length)
        => Enumerable.Range(0, length + 1).Select(i => start + direction.ToBlockPos() * i);


    [Master]
    private void PlaceLine(int x, int y, Facing direction, int length)
    {
        if (Player.NetworkID != GetTree().GetRpcSenderId()) return;

        // TODO: Test if starting block is valid.
        // FIXME: Test if there is a player in the way.

        var start = new BlockPos(x, y);
        var world = this.GetWorld();
        foreach (var pos in GetBlockPositions(start, direction, length)) {
            if (world.GetBlockAt(pos) != null) continue;
            var color = Player.Color.Blend(Color.FromHsv(0.0F, 0.0F, GD.Randf(), 0.2F));
            world.Rpc(nameof(World.SpawnBlock), pos.X, pos.Y, color, false);
        }
    }

    [Master]
    private void BreakLine(int x, int y, Facing direction, int length)
    {
        if (Player.NetworkID != GetTree().GetRpcSenderId()) return;

        // TODO: Do additional verification on the packet.

        var start = new BlockPos(x, y);
        var world = this.GetWorld();
        foreach (var pos in GetBlockPositions(start, direction, length)) {
            var block = world.GetBlockAt(pos);
            if (block?.Unbreakable != false) continue;
            world.Rpc(nameof(World.Despawn), world.GetPathTo(block));
        }
    }
}
