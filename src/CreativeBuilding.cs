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
    private BlockPos _startPos;
    private Facing _direction;
    private int _length;
    private bool _canBuild;

    private BuildMode? _currentMode = null;

    public override void _Ready()
    {
        _blockTex = GD.Load<Texture>("res://gfx/block.png");
    }

    public override void _PhysicsProcess(float delta)
    {
        if (!(this.GetGame() is Client client)) return;
        Update();

        if (EscapeMenu.Instance.Visible || !client.Cursor.Visible)
            { _currentMode = null; return; }

        switch (_currentMode) {
            case null:
                if (Input.IsActionJustPressed("interact_place"))
                    if (_canBuild) _currentMode = BuildMode.Placing;
                if (Input.IsActionJustPressed("interact_break"))
                    _currentMode = BuildMode.Breaking;
                break;
            case BuildMode.Placing:
                if (Input.IsActionJustPressed("interact_break")) _currentMode = null;
                else if (!Input.IsActionPressed("interact_place")) {
                    if (_canBuild) this.GetClient()?.RPC(PlaceLine, _startPos, _direction, _length);
                    _currentMode = null;
                }
                break;
            case BuildMode.Breaking:
                if (Input.IsActionJustPressed("interact_place")) _currentMode = null;
                else if (!Input.IsActionPressed("interact_break")) {
                    this.GetClient()?.RPC(BreakLine, _startPos, _direction, _length);
                    _currentMode = null;
                }
                break;
        }

        if (_currentMode != null) {
            var start  = _startPos.ToVector();
            var angle  = client.Cursor.Position.AngleToPoint(start); // angle_to_point appears reversed.
            _direction = Facings.FromAngle(angle);
            _length    = Math.Min(MaxLength, Mathf.RoundToInt(start.DistanceTo(client.Cursor.Position) / 16));
        } else {
            _startPos = BlockPos.FromVector(client.Cursor.Position);
            _length   = 0;
        }

        bool IsBlockAt(BlockPos pos) => client.GetBlockAt(pos) != null;
        _canBuild = !IsBlockAt(_startPos) && Facings.All.Any(pos => IsBlockAt(_startPos + pos.ToBlockPos()));
    }

    public override void _Draw()
    {
        if (!(this.GetGame() is Client client) || !client.Cursor.Visible || EscapeMenu.Instance.Visible) return;

        var green = Color.FromHsv(1.0F / 3, 1.0F, 1.0F, 0.4F);
        var red   = Color.FromHsv(0.0F, 1.0F, 1.0F, 0.4F);
        var black = new Color(0.0F, 0.0F, 0.0F, 0.65F);

        foreach (var pos in GetBlockPositions(_startPos, _direction, _length)) {
            var hasBlock = client.GetBlockAt(pos) != null;
            var color    = (_currentMode != BuildMode.Breaking)
                ? ((_canBuild && !hasBlock) ? green : red)
                : (hasBlock ? black : red);
            DrawTexture(_blockTex, ToLocal(pos.ToVector() - _blockTex.GetSize() / 2), color);
        }
    }

    private static IEnumerable<BlockPos> GetBlockPositions(BlockPos start, Facing direction, int length)
        => Enumerable.Range(0, length + 1).Select(i => start + direction.ToBlockPos() * i);


    [RPC(PacketDirection.ClientToServer)]
    private static void PlaceLine(Server server, NetworkID networkID, BlockPos start, Facing direction, int length)
    {
        var player = server.GetPlayer(networkID);
        // TODO: Test if starting block is valid.
        foreach (var pos in GetBlockPositions(start, direction, length)) {
            if (server.GetBlockAt(pos) != null) continue;
            // FIXME: Test if there is a player in the way.

            server.Spawn<Block>(block => {
                block.Position = pos;
                block.Color    = player.Color.Blend(Color.FromHsv(0.0F, 0.0F, GD.Randf(), 0.2F));
            });
        }
    }

    [RPC(PacketDirection.ClientToServer)]
    private static void BreakLine(Server server, NetworkID networkID, BlockPos start, Facing direction, int length)
    {
        // var player = server.GetPlayer(networkID);
        // TODO: Do additional verification on the packet.
        foreach (var pos in GetBlockPositions(start, direction, length)) {
            var block = server.GetBlockAt(pos);
            if (block?.Unbreakable != false) continue;
            block.Destroy();
        }
    }
}
