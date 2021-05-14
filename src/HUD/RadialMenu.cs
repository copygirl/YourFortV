using System;
using Godot;

// TODO: Display number of rounds for weapons? Add an even smaller font for this?
public class RadialMenu : Node2D
{
    [Export] public int InnerRadius { get; set; } = 32;
    [Export] public int OuterRadius { get; set; } = 64;
    [Export] public int MinElements { get; set; } = 8;
    [Export] public float Separation { get; set; } = 2F;

    public Cursor Cursor { get; private set; }
    public Label ActiveName { get; private set; }

    private float _startAngle;
    private Node2D _selected;
    private DateTime? _showUntil;

    public override void _Ready()
    {
        _startAngle = (-Mathf.Tau / 4) - (Mathf.Tau / MinElements / 2);

        Cursor     = this.GetClient()?.Cursor;
        ActiveName = GetNode<Label>("ActiveName");
    }

    public IItems GetItems()
        => this.GetClient().LocalPlayer?.Items;

    public override void _UnhandledInput(InputEvent ev)
    {
        if (GetItems() == null) return;

        if (ev.IsActionPressed("interact_select")) {
            Position = this.GetClient().Cursor.ScreenPosition.Round();
            Visible  = true;
            Modulate = Colors.White;

            var items = GetItems();
            _selected = items.Current;
            items.Current = null;

            _showUntil = null;
            ActiveName.Text = _selected?.Name ?? "";
            Update();
        }

        if (ev.IsActionPressed("interact_select_dec") || ev.IsActionPressed("interact_select_inc")) {
            var diff  = ev.IsActionPressed("interact_select_inc") ? 1 : -1;
            var items = GetItems();
            // TODO: Should current item be equipped until radial menu disappears again?
            //       Perhaps for balance reasons?

            if (Visible && (_showUntil == null)) {
                // Don't do anything if radial menu is show due to selection
                // being open and the mouse is outside of the selection radius.
                var cursor = ToLocal(this.GetClient().Cursor.ScreenPosition);
                if (cursor.Length() > InnerRadius) return;
            } else {
                Position   = this.GetClient().LocalPlayer.GetGlobalTransformWithCanvas().origin;
                _selected  = items.Current;
                _showUntil = DateTime.Now + TimeSpan.FromSeconds(0.6);
                Visible    = true;
                Modulate   = Colors.White;
            }

            if (_selected == null) {
                if (items.Count == 0) return;
                _selected = items.Current = items[0];
            } else {
                var index = _selected.GetIndex();
                _selected = items.Current = items[Mathf.PosMod(index + diff, items.Count)];
            }
            ActiveName.Text = _selected?.Name ?? "";
            Update();
        }
    }

    public override void _Process(float delta)
    {
        if (!Visible) return;

        if (_showUntil != null) {
            if (DateTime.Now >= _showUntil) {
                Modulate = new Color(Modulate, Modulate.a - delta * 3);
                if (Modulate.a <= 0) {
                    _showUntil = null;
                    Visible = false;
                }
            }
            return;
        }

        var items = GetItems();
        if (items == null) return;

        var cursor = ToLocal(this.GetClient().Cursor.ScreenPosition);
        var angle  = cursor.Angle() - _startAngle;
        var index  = (int)((angle / Mathf.Tau + 1) % 1 * MinElements);
        if ((cursor.Length() > InnerRadius) && (index < items.Count) && (items[index] != _selected)) {
            _selected = items[index];
            ActiveName.Text = _selected?.Name ?? "";
            Update();
        }

        if (!Input.IsActionPressed("interact_select")) {
            _showUntil = DateTime.Now;
            items.Current = _selected;
        }
    }

    public override void _Draw()
    {
        var items = GetItems();

        var vertices = new Vector2[5];
        var colors   = new Color[5];

        var numElements = Math.Max(MinElements, items.Count);
        for (var i = 0; i < numElements; i++) {
            var angle1 = _startAngle + Mathf.Tau * ( i      / (float)numElements);
            var angle3 = _startAngle + Mathf.Tau * ((i + 1) / (float)numElements);
            var angle2 = (angle1 + angle3) / 2;

            var sep1 = Mathf.Polar2Cartesian(Separation, angle1 + Mathf.Tau / 4);
            var sep2 = Mathf.Polar2Cartesian(Separation, angle3 - Mathf.Tau / 4);

            var isSelected  = (i < items.Count) && (_selected == items[i]);
            var innerRadius = InnerRadius + (isSelected ? 5 : 0);
            var outerRadius = OuterRadius + (isSelected ? 5 : 0);

            vertices[0] = Mathf.Polar2Cartesian(innerRadius             , angle1) + sep1;
            vertices[1] = Mathf.Polar2Cartesian(outerRadius             , angle1) + sep1;
            vertices[2] = Mathf.Polar2Cartesian(outerRadius + Separation, angle2);
            vertices[3] = Mathf.Polar2Cartesian(outerRadius             , angle3) + sep2;
            vertices[4] = Mathf.Polar2Cartesian(innerRadius             , angle3) + sep2;

            var color = new Color(0.1F, 0.1F, 0.1F, isSelected ? 0.7F : 0.4F);
            for (var j = 0; j < colors.Length; j++) colors[j] = color;

            DrawPolygon(vertices, colors, antialiased: true);

            if (i < items.Count) {
                var sprite = (items[i].GetNodeOrNull("Icon") as Sprite) ?? (items[i] as Sprite);
                if (sprite != null) {
                    var pos = Mathf.Polar2Cartesian((innerRadius + outerRadius) / 2, angle2);
                    if (sprite.Centered) pos -= sprite.Texture.GetSize() / 2;
                    DrawTexture(sprite.Texture, pos, sprite.Modulate);
                }
            }
        }
    }
}
