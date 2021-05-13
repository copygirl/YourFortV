using System;
using Godot;

// TODO: Render rounds as sprites?

public class WeaponInfo : Node2D
{
    private static readonly TimeSpan VISIBLE_DURATION = TimeSpan.FromSeconds(0.8);

    public Label Rounds { get; private set; }
    public ProgressBar Reloading { get; private set; }

    private float _visibleFor;
    private Weapon _previousWeapon;
    private int _previousRounds;

    public override void _Ready()
    {
        Rounds    = GetNode<Label>("Rounds");
        Reloading = GetNode<ProgressBar>("Reloading");
    }

    public override void _Process(float delta)
    {
        if (!(this.GetClient().LocalPlayer?.GetNode<IItems>("Items").Current is Weapon weapon))
            { Visible = false; _previousWeapon = null; return; }

        Visible = true;
        if ((_visibleFor += delta) > VISIBLE_DURATION.TotalSeconds) {
            Modulate = new Color(Modulate, Modulate.a - delta);
            if (Modulate.a <= 0) Visible = false;
        }

        if ((weapon != _previousWeapon) ||
            (weapon.Rounds != _previousRounds) ||
            (weapon.ReloadProgress != null)) {
            _visibleFor = 0.0F;
            Modulate    = Colors.White;

            Rounds.Visible = weapon.Capacity > 1;
            Rounds.Text    = $"{weapon.Rounds}/{weapon.Capacity}";

            if (weapon.ReloadProgress is float reloading) {
                Reloading.Visible = true;
                Reloading.Value = reloading;
            } else Reloading.Visible = false;

            _previousWeapon = weapon;
            _previousRounds = weapon.Rounds;
        }
    }
}
