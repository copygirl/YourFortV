using Godot;

public class Weapon : Sprite
{
    [Export] public int EffectiveRange { get; set; } = 320;
    [Export] public int MaximumRange { get; set; } = 640;

    [Export] public float Spread { get; set; } = 0.0F;
    [Export] public float SpreadIncrease { get; set; } = 0.0F;
    [Export] public float SpreadRegen { get; set; } = 10.0F;

    [Export] public float RecoilMin { get; set; } = 0.0F;
    [Export] public float RecoilMax { get; set; } = 0.0F;
    [Export] public float RecoilRegen { get; set; } = 10.0F;

    // TODO: Make the Regen multiplicative instead of substractive?


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
        if (!(Player is LocalPlayer)) return;

        // TODO: Is not "place", is shoot!
        if (ev.IsActionPressed("interact_place")) {
            GetNodeOrNull<AudioStreamPlayer2D>("Fire")?.Play();
            // TODO: Spawn bullet or something.
            // TODO: Tell server (and other clients) we shot.
            _currentSpreadInc += Mathf.Deg2Rad(SpreadIncrease);
            _currentRecoil    += Mathf.Deg2Rad((float)GD.RandRange(RecoilMin, RecoilMax));
        }
    }

    public override void _Process(float delta)
    {
        _currentSpreadInc = Mathf.Max(0, _currentSpreadInc - Mathf.Deg2Rad(SpreadRegen) * delta);
        _currentRecoil    = Mathf.Max(0, _currentRecoil    - Mathf.Deg2Rad(RecoilRegen) * delta);

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
