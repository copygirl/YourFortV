using System;
using Godot;

// TODO: "Click" sound when attempting to fire when not ready, or empty.
// TODO: "Single reload" for revolver & shotgun.
// TODO: Add outline around weapon sprites.

public class Weapon : Sprite
{
    private const float NETWORK_EPSILON = 0.05F;

    [Export] public bool Automatic   { get; set; } = false;
    [Export] public int RateOfFire   { get; set; } = 100; // rounds/minute
    [Export] public int Capacity     { get; set; } = 12;
    [Export] public float ReloadTime { get; set; } = 1.0F;

    [Export] public float Knockback      { get; set; } = 0.0F;
    [Export] public float Spread         { get; set; } = 0.0F;
    [Export] public float SpreadIncrease { get; set; } = 0.0F;
    [Export] public float RecoilMin      { get; set; } = 0.0F;
    [Export] public float RecoilMax      { get; set; } = 0.0F;

    [Export] public int EffectiveRange  { get; set; } = 320;
    [Export] public int MaximumRange    { get; set; } = 640;
    [Export] public int BulletVelocity  { get; set; } = 2000;
    [Export] public int BulletsPerShot  { get; set; } = 1;
    [Export] public float Damage        { get; set; } = 0.0F;
    [Export] public float BulletOpacity { get; set; } = 0.2F;


    public float _fireDelay;
    public float _reloadDelay;
    public bool _lowered;

    private float _currentSpreadInc = 0.0F;
    private float _currentRecoil    = 0.0F;

    public int Rounds { get; private set; }
    public float AimDirection { get; private set; }
    public TimeSpan? HoldingTrigger { get; private set; }
    // TODO: Tell the server when we're pressing/releasing the trigger.

    public bool IsReloading => _reloadDelay > 0.0F;
    public float ReloadProgress => 1 - _reloadDelay / ReloadTime;


    public Cursor Cursor { get; private set; }
    public Player Player { get; private set; }
    public Vector2 TipOffset { get; private set; }

    public override void _Ready()
    {
        Rounds = Capacity;

        Cursor    = this.GetClient()?.Cursor;
        Player    = GetParent().GetParent<Player>();
        TipOffset = GetNode<Node2D>("Tip").Position;
        GetNode<Node2D>("Tip").RemoveFromParent();
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (!(Player is LocalPlayer)) return;
        if (ev.IsActionPressed("interact_primary"))
            { HoldingTrigger = TimeSpan.Zero; OnTriggerPressed(); }
        if (ev.IsActionPressed("interact_reload")) Reload();
    }

    protected virtual void OnTriggerPressed() => Fire();
    protected virtual void OnTriggerReleased() {  }

    public override void _Process(float delta)
    {
        var spreadDecrease = Mathf.Max(Mathf.Tau / 200, _currentSpreadInc * 1.5F);
        var recoilDecrease = Mathf.Max(Mathf.Tau / 600, _currentRecoil * 1.5F);
        _currentSpreadInc = Mathf.Max(0, _currentSpreadInc - spreadDecrease * delta);
        _currentRecoil    = Mathf.Max(0, _currentRecoil    - recoilDecrease * delta);

        if (!Player.IsAlive) {
            // TODO: Do this once when player respawns.
            _fireDelay     = 0.0F;
            _reloadDelay   = 0.0F;
            Rounds         = Capacity;
            HoldingTrigger = null;
            if (Player is LocalPlayer) Update();
        } else if (Visible) {
            if (HoldingTrigger is TimeSpan holding)
                HoldingTrigger = holding + TimeSpan.FromSeconds(delta);

            if (IsReloading && ((_reloadDelay -= delta) <= 0)) {
                _reloadDelay = 0.0F;
                Rounds = Capacity;
            }

            if (_fireDelay > 0) {
                _fireDelay -= delta;
                // We allow _fireDelay to go into negatives to allow
                // for more accurate rate of fire for automatic weapons.
                // Though, if the trigger isn't held down, reset it to 0.
                if ((_fireDelay < 0) && (!Automatic || (HoldingTrigger == null)))
                    _fireDelay = 0;
            }

            if (Player is LocalPlayer) {
                // Automatically reload when out of rounds.
                if (Rounds <= 0) Reload();

                if (HoldingTrigger != null) {
                    if (!Input.IsActionPressed("interact_primary")) {
                        HoldingTrigger = null;
                        OnTriggerReleased();
                    } else if (Automatic)
                        Fire();
                }

                //           Gun   TipOffset                     C = Cursor
                //            v        v        b                     v
                //      x---###########x------------------------------x
                //      |   ##==#                      _____-----
                //    a |   ##               _____-----
                //      |          _____-----  c
                //      x_____-----
                //      ^
                // B = Player

                // The length of `a` and `c` as well as the angle of `c` are known.
                // `a` is the y component of the weapon's TipOffset.
                // `c` is the line connecting the player and cursor.
                // Find out the angle `C` to subtract from the already known angle of `c`.
                // CREDIT to lizzie for helping me figure out this trigonometry problem.

                var a = TipOffset.y * ((Scale.y > 0) ? 1 : -1);
                var c = Player.Position.DistanceTo(Cursor.Position);
                if (c < TipOffset.x) {
                    // If the cursor is too close to the player, put the
                    // weapon in a "lowered" state, where it can't be shot.
                    AimDirection = Mathf.Deg2Rad((Cursor.Position.x > Player.Position.x) ? 30 : 150);
                    _lowered     = true;
                } else {
                    var angleC   = Mathf.Asin(a / c);
                    AimDirection = Cursor.Position.AngleToPoint(Player.Position) - angleC;
                    _lowered     = false;
                }

                RPC.Unreliable(1, SendAimAngle, AimDirection);
                Update();
            }
        } else {
            _reloadDelay = 0.0F;
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
            if ((Player.NetworkID != GetTree().GetRpcSenderId()) || !Player.IsAlive) return;
            if (float.IsNaN(value = Mathf.PosMod(value, Mathf.Tau))) return;

            RPC.Unreliable(SendAimAngle, value);
        } else if (!(Player is LocalPlayer))
            AimDirection = value;
    }


    private void Fire()
    {
        var seed = unchecked((int)GD.Randi());
        if (!FireInternal(AimDirection, Scale.y > 0, seed)) return;
        RPC.Reliable(1, SendFire, AimDirection, Scale.y > 0, seed);
        ((LocalPlayer)Player).Velocity -= Mathf.Polar2Cartesian(Knockback, Rotation);
    }

    protected virtual bool FireInternal(float aimDirection, bool toRight, int seed)
    {
        var isServer = this.GetGame() is Server;
        var epsilon  = isServer ? NETWORK_EPSILON : 0.0F;
        if (!Visible || _lowered || !Player.IsAlive || (_fireDelay > epsilon)) return false;

        if (Rounds <= 0) {
            if (_reloadDelay <= epsilon) {
                _reloadDelay += ReloadTime;
                Rounds = Capacity;
            } else return false;
        }

        if (!isServer) GetNodeOrNull<AudioStreamPlayer2D>("Fire")?.Play();

        var random = new Random(seed);
        var angle = aimDirection - _currentRecoil * (toRight ? 1 : -1);

        var tip = (toRight ? TipOffset : TipOffset * new Vector2(1, -1)).Rotated(angle);
        for (var i = 0; i < BulletsPerShot; i++) {
            var spread = (Mathf.Deg2Rad(Spread) + _currentSpreadInc) * Mathf.Clamp(random.NextGaussian(0.4F), -1, 1);
            var dir    = Mathf.Polar2Cartesian(1, angle + spread);
            var color  = new Color(Player.Color, BulletOpacity);
            var bullet = new Bullet(Player.Position + tip, dir, EffectiveRange, MaximumRange,
                                    BulletVelocity, Damage / BulletsPerShot, color);
            this.GetWorld().AddChild(bullet);
        }

        _currentSpreadInc += Mathf.Deg2Rad(SpreadIncrease);
        _currentRecoil    += Mathf.Deg2Rad(random.NextFloat(RecoilMin, RecoilMax));

        if (isServer || (Player is LocalPlayer)) {
            // Do not keep track of fire rate or ammo for other players.
            _fireDelay += 60.0F / RateOfFire;
            Rounds -= 1;
        }
        return true;
    }

    [Remote]
    private void SendFire(float aimDirection, bool toRight, int seed)
    {
        if (this.GetGame() is Server) {
            if (Player.NetworkID != GetTree().GetRpcSenderId()) return;
            if (float.IsNaN(aimDirection = Mathf.PosMod(aimDirection, Mathf.Tau))) return;

            if (FireInternal(aimDirection, toRight, seed))
                RPC.Reliable(SendFire, aimDirection, toRight, seed);
        } else if (!(Player is LocalPlayer))
            FireInternal(aimDirection, toRight, seed);
    }


    private void Reload()
        { if (ReloadInternal()) RPC.Reliable(1, SendReload); }

    private bool ReloadInternal()
    {
        if (!Visible || !Player.IsAlive || (Rounds >= Capacity) || IsReloading) return false;

        // TODO: Play reload sound.
        _reloadDelay += ReloadTime;
        return true;
    }

    [Remote]
    private void SendReload()
    {
        if (this.GetGame() is Server) {
            if (Player.NetworkID != GetTree().GetRpcSenderId()) return;
            if (ReloadInternal()) RPC.Reliable(SendReload);
        } else if (!(Player is LocalPlayer))
            ReloadInternal();
    }


    public override void _Draw()
    {
        if (!(Player is LocalPlayer) || !Player.IsAlive || _lowered) return;
        // Draws an "aiming cone" to show where bullets might travel.

        var tip   = TipOffset + new Vector2(4, 0);
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
