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

    private static readonly Vector2[] _neighborPositions = new Vector2[]{
        Vector2.Left*16, Vector2.Right*16, Vector2.Up*16, Vector2.Down*16 };

    [Export] public int MaxLength { get; set; } = 6;

    private Texture _blockTex;
    private Vector2 _startPos;
    private Vector2 _direction;
    private int _length;
    private bool _canBuild;

    private BuildMode? _currentMode = null;

    private IEnumerable<Vector2> BlockPositions =>
        Enumerable.Range(0, _length + 1).Select(i => _startPos + _direction * (i * 16));

    public override void _Ready()
    {
        _blockTex = GD.Load<Texture>("res://gfx/block.png");
    }

    public override void _Process(float delta)
    {
        Update();

        if (EscapeMenu.Instance.Visible || !Game.Cursor.Visible)
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
                    if (_canBuild)
                        foreach (var pos in BlockPositions)
                            PlaceBlock(pos);
                    _currentMode = null;
                }
                break;
            case BuildMode.Breaking:
                if (Input.IsActionJustPressed("interact_place")) _currentMode = null;
                else if (!Input.IsActionPressed("interact_break")) {
                    foreach (var pos in BlockPositions) {
                        var block = Game.Instance.GetBlockAt(pos);
                        if (block != null) BreakBlock(block);
                    }
                    _currentMode = null;
                }
                break;
        }

        if (_currentMode != null) {
            var rad90  = Mathf.Deg2Rad(90.0F);
            var angle  = Mathf.Round(_startPos.AngleToPoint(Game.Cursor.Position) / rad90) * rad90;
            _direction = new Vector2(-Mathf.Cos(angle), -Mathf.Sin(angle));
            _length    = Math.Min(MaxLength, Mathf.RoundToInt(_startPos.DistanceTo(Game.Cursor.Position) / 16));
        } else {
            _startPos = (Game.Cursor.Position / 16).Round() * 16;
            _length   = 0;
        }

        bool IsBlockAt(Vector2 pos) => Game.Instance.GetBlockAt(pos) != null;
        _canBuild = !IsBlockAt(_startPos) && _neighborPositions.Any(pos => IsBlockAt(_startPos + pos));
    }

    private Block PlaceBlock(Vector2 position)
    {
        if (Game.Instance.GetBlockAt(position) != null) return null;
        // FIXME: Test if there is a player in the way.

        var block = Game.Instance.BlockScene.Init<Block>();
        block.Position = position;
        block.Modulate = Game.LocalPlayer.Color.Blend(Color.FromHsv(0.0F, 0.0F, GD.Randf(), 0.2F));
        Game.Instance.BlockContainer.AddChild(block);

        if (Network.IsMultiplayerReady) {
            if (Network.IsServer) Network.API.SendToEveryone(new SpawnBlockPacket(block));
            else Network.API.SendToServer(new PlaceBlockPacket(position));
        }

        return block;
    }

    private void BreakBlock(Block block)
    {
        // FIXME: Use a different (safer) way to check if a block is one of the default ones.
        if (block.Modulate.s < 0.5F) return;

        if (Network.IsMultiplayerReady) {
            if (Network.IsServer) Network.API.SendToEveryone(new DestroyBlockPacket(block));
            else Network.API.SendToServer(new BreakBlockPacket(block));
        }

        block.QueueFree();
    }

    public override void _Draw()
    {
        if (!Game.Cursor.Visible) return;

        var green = Color.FromHsv(1.0F / 3, 1.0F, 1.0F, 0.4F);
        var red   = Color.FromHsv(0.0F, 1.0F, 1.0F, 0.4F);
        var black = new Color(0.0F, 0.0F, 0.0F, 0.65F);

        foreach (var pos in BlockPositions) {
            var hasBlock = Game.Instance.GetBlockAt(pos) != null;
            var color = (_currentMode != BuildMode.Breaking)
                ? ((_canBuild && !hasBlock) ? green : red)
                : (hasBlock ? black : red);
            DrawTexture(_blockTex, ToLocal(pos - _blockTex.GetSize() / 2), color);
        }
    }



    public static void RegisterPackets()
    {
        Network.API.RegisterC2SPacket<PlaceBlockPacket>(OnPlaceBlockPacket);
        Network.API.RegisterC2SPacket<BreakBlockPacket>(OnBreakBlockPacket);
    }

    private class PlaceBlockPacket
    {
        public Vector2 Position { get; }
        public PlaceBlockPacket(Vector2 position) => Position = position;
    }
    private static void OnPlaceBlockPacket(Player player, PlaceBlockPacket packet)
    {
        if (Game.Instance.GetBlockAt(packet.Position) != null) return;
        var block = Game.Instance.BlockScene.Init<Block>();
        block.Position = packet.Position;
        block.Modulate = player.Color.Blend(Color.FromHsv(0.0F, 0.0F, GD.Randf(), 0.2F));
        Game.Instance.BlockContainer.AddChild(block);

        Network.API.SendToEveryone(new SpawnBlockPacket(block));
    }

    private class BreakBlockPacket
    {
        public Vector2 Position { get; }
        public BreakBlockPacket(Block block) => Position = block.Position;
    }
    private static void OnBreakBlockPacket(Player player, BreakBlockPacket packet)
    {
        var block = Game.Instance.GetBlockAt(packet.Position);
        if (block == null) return;

        if (block.Modulate.s < 0.5F) {
            // TODO: Respawn the block the client thought it destroyed?
            return;
        }
        // TODO: Further verify whether player can break a block at this position.

        Network.API.SendToEveryoneExcept(player, new DestroyBlockPacket(block));
        block.QueueFree();
    }
}
