using System;
using Godot;

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

    public override void _Ready()
    {
        _startAngle = (-Mathf.Tau / 4) - (Mathf.Tau / MinElements / 2);

        Cursor     = this.GetClient()?.Cursor;
        ActiveName = GetNode<Label>("ActiveName");
    }

    public IItems GetItems()
        => this.GetClient().LocalPlayer?.GetNode<IItems>("Items");

    public override void _UnhandledInput(InputEvent ev)
    {
        if (ev.IsActionPressed("interact_select")) {
            Position = this.GetClient().Cursor.ScreenPosition.Round();
            Visible = true;

            var items = GetItems();
            _selected = items.Current;
            items.Current = null;

            ActiveName.Text = _selected?.Name ?? "";
            Update();
        }
        // TODO: Add scrollwheel support.
    }

    public override void _Process(float delta)
    {
        if (!Visible) return;
        var items = GetItems();

        var cursorPos = ToLocal(this.GetClient().Cursor.ScreenPosition);
        var angle = cursorPos.Angle() - _startAngle;
        var index = (int)((angle / Mathf.Tau + 1) % 1 * MinElements);
        if ((cursorPos.Length() > InnerRadius) && (index < items.Count) && (items[index] != _selected)) {
            _selected = items[index];
            ActiveName.Text = _selected?.Name ?? "";
            Update();
        }

        if (!Input.IsActionPressed("interact_select")) {
            Visible = false;
            items.Current = _selected;
            Update();
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
