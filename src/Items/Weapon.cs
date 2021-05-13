using System;
using Godot;

public class Weapon : Sprite
{
    [Export] public int EffectiveRange { get; set; } = 320;
    [Export] public int MaximumRange { get; set; } = 640;
    [Export] public float Knockback { get; set; } = 0.0F;

    [Export] public float Spread { get; set; } = 0.0F;
    [Export] public float SpreadIncrease { get; set; } = 0.0F;

    [Export] public float RecoilMin { get; set; } = 0.0F;
    [Export] public float RecoilMax { get; set; } = 0.0F;

    [Export] public int BulletSpeed { get; set; } = 2000;
    [Export] public int BulletsPetShot { get; set; } = 1;
    [Export] public float BulletOpacity { get; set; } = 0.2F;


    public Cursor Cursor { get; private set; }
    public Player Player { get; private set; }

    private float _currentSpreadInc = 0.0F;
    private float _currentRecoil = 0.0F;

    public float AimDirection { get; private set; }


    public override void _Ready()
    {
        Cursor = this.GetClient()?.Cursor;
        Player = GetParent().GetParent<Player>();
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (!(Player is LocalPlayer localPlayer)) return;

        if (ev.IsActionPressed("interact_primary")) {
            var seed = unchecked((int)GD.Randi());
            ShootInternal(AimDirection, Scale.y > 0, seed);
            RpcId(1, nameof(Shoot), AimDirection, Scale.y > 0, seed);
            localPlayer.Velocity -= Mathf.Polar2Cartesian(Knockback, Rotation);
        }
    }

    [Remote]
    private void Shoot(float aimDirection, bool toRight, int seed)
    {
        if (this.GetGame() is Server) {
            if (Player.NetworkID != GetTree().GetRpcSenderId()) return;
            // TODO: Verify input.
            Rpc(nameof(Shoot), aimDirection, toRight, seed);
        } else if (Player is LocalPlayer) return;
        ShootInternal(aimDirection, toRight, seed);
    }
    private void ShootInternal(float aimDirection, bool toRight, int seed)
    {
        if (this.GetGame() is Client)
            GetNodeOrNull<AudioStreamPlayer2D>("Fire")?.Play();

        var random = new Random(seed);
        var angle = aimDirection - _currentRecoil * (toRight ? 1 : -1);

        var tip = GetNode<Node2D>("Tip").Position;
        if (!toRight) tip.y *= -1;
        tip = tip.Rotated(angle);

        for (var i = 0; i < BulletsPetShot; i++) {
            var spread = (Mathf.Deg2Rad(Spread) + _currentSpreadInc) * Mathf.Clamp(random.NextGaussian(0.4F), -1, 1);
            var dir    = Mathf.Polar2Cartesian(1, angle + spread);
            var color  = new Color(Player.Color, BulletOpacity);
            var bullet = new Bullet(Player.Position + tip, dir, EffectiveRange, MaximumRange, BulletSpeed, color);
            this.GetWorld().AddChild(bullet);
        }

        _currentSpreadInc += Mathf.Deg2Rad(SpreadIncrease);
        _currentRecoil    += Mathf.Deg2Rad(random.NextFloat(RecoilMin, RecoilMax));
    }

    public override void _Process(float delta)
    {
        var spreadDecrease = Mathf.Max(Mathf.Tau / 300, _currentSpreadInc * 2);
        var recoilDecrease = Mathf.Max(Mathf.Tau / 800, _currentRecoil * 2);
        _currentSpreadInc = Mathf.Max(0, _currentSpreadInc - spreadDecrease * delta);
        _currentRecoil    = Mathf.Max(0, _currentRecoil    - recoilDecrease * delta);

        if (Visible && (Player is LocalPlayer)) {
            AimDirection = Cursor.Position.AngleToPoint(Player.Position);
            RpcId(1, nameof(SendAimAngle), AimDirection);
            Update();
        }

        var angle = Mathf.PosMod(AimDirection + Mathf.Pi, Mathf.Tau) - Mathf.Pi;
            angle = Mathf.Abs(Mathf.Rad2Deg(angle));
        if (Scale.y > 0) { if (angle > 100.0F) Scale = new Vector2(1, -1); }
        else               if (angle <  80.0F) Scale = new Vector2(1,  1);
        Rotation = AimDirection - _currentRecoil * ((Scale.y > 0) ? 1 : -1);
    }

    [Remote]
    private void SendAimAngle(float value)
    {
        if (this.GetGame() is Server) {
            if (Player.NetworkID != GetTree().GetRpcSenderId()) return;
            // TODO: Verify input.
            // if ((value < 0) || (value > Mathf.Tau)) return;
            Rpc(nameof(SendAimAngle), value);
        } else if (!(Player is LocalPlayer))
            AimDirection = value;
    }

    public override void _Draw()
    {
        if (!(Player is LocalPlayer)) return;

        var tip   = GetNode<Node2D>("Tip").Position + new Vector2(4, 0);
        var angle = Mathf.Sin((Mathf.Deg2Rad(Spread) + _currentSpreadInc) / 2);
        var color = Colors.Black;

        var points = new Vector2[8];
        var colors = new Color[8];
        colors[0] = colors[7] = new Color(color, 0.0F);
            points[0] = points[7] = tip;
        colors[1] = colors[6] = new Color(color, 0.15F);
            points[1] = tip + new Vector2(1,  angle) * 64;
            points[6] = tip + new Vector2(1, -angle) * 64;
        colors[2] = colors[5] = new Color(color, 0.15F);
            points[2] = tip + new Vector2(1,  angle) * EffectiveRange;
            points[5] = tip + new Vector2(1, -angle) * EffectiveRange;
        colors[3] = colors[4] = new Color(color, 0.0F);
            points[3] = tip + new Vector2(1,  angle) * MaximumRange;
            points[4] = tip + new Vector2(1, -angle) * MaximumRange;

        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.TriangleStrip);
            st.AddColor(colors[0]);
                st.AddVertex(To3(points[0]));
            st.AddColor(colors[1]);
                st.AddVertex(To3(points[1]));
                st.AddVertex(To3(points[6]));
            st.AddColor(colors[2]);
                st.AddVertex(To3(points[2]));
                st.AddVertex(To3(points[5]));
            st.AddColor(colors[3]);
                st.AddVertex(To3(points[3]));
                st.AddVertex(To3(points[4]));
        st.Index();

        DrawMesh(st.Commit(), null);
        DrawPolylineColors(points, colors, antialiased: true);
    }
    private static Vector3 To3(Vector2 vec)
        => new Vector3(vec.x, vec.y, 0);
}
