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
    public Node2D Selected { get; private set; } = null;
    public int? SelectedIndex => (Selected != null) ? Selected.GetIndex() : (int?)null;

    public override void _Ready()
    {
        _startAngle = (-Mathf.Tau / 4) - (Mathf.Tau / MinElements / 2);

        Cursor     = this.GetClient()?.Cursor;
        ActiveName = GetNode<Label>("ActiveName");
    }

    public Node GetItems()
        => this.GetClient().LocalPlayer?.GetNode<Node2D>("Items");

    public void Select(Node2D node)
    {
        if (node == Selected) return;
        if ((node != null) && (node.GetParent() != GetItems())) throw new ArgumentException();

        ActiveName.Text = node?.Name ?? "";
        if (Visible)
            Update();
        else {
            SetActive(Selected, false);
            SetActive(node, true);
        }
        Selected = node;
    }
    private static void SetActive(Node2D node, bool value) {
        if (node == null) return;
        node.SetProcessInput(value);
        node.SetProcessUnhandledInput(value);
        node.Visible = value;
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (ev.IsActionPressed("interact_select")) {
            Position = this.GetClient().Cursor.ScreenPosition.Round();
            Visible = true;
            SetActive(Selected, false);
            Update();
        }
        // TODO: Add scrollwheel support.
    }

    public override void _Process(float delta)
    {
        if (!Visible) return;

        var cursorPos = ToLocal(this.GetClient().Cursor.ScreenPosition);
        var angle = cursorPos.Angle() - _startAngle;
        var index = (int)((angle / Mathf.Tau + 1) % 1 * MinElements);
        var items = GetItems();
        if ((cursorPos.Length() > InnerRadius) && (index < items.GetChildCount()))
            Select(items.GetChild<Node2D>(index));

        if (!Input.IsActionPressed("interact_select")) {
            Visible = false;
            SetActive(Selected, true);
            Update();
        }
    }

    public override void _Draw()
    {
        var vertices = new Vector2[5];
        var colors   = new Color[5];

        for (var i = 0; i < MinElements; i++) {
            var angle1 = _startAngle + Mathf.Tau * ( i      / (float)MinElements);
            var angle3 = _startAngle + Mathf.Tau * ((i + 1) / (float)MinElements);
            var angle2 = (angle1 + angle3) / 2;

            var sep1 = new Vector2(Mathf.Cos(angle1 + Mathf.Tau / 4), Mathf.Sin(angle1 + Mathf.Tau / 4)) * Separation;
            var sep2 = new Vector2(Mathf.Cos(angle3 - Mathf.Tau / 4), Mathf.Sin(angle3 - Mathf.Tau / 4)) * Separation;

            var isSelected  = i == SelectedIndex;
            var innerRadius = InnerRadius + (isSelected ? 5 : 0);
            var outerRadius = OuterRadius + (isSelected ? 5 : 0);

            vertices[0] = new Vector2(Mathf.Cos(angle1), Mathf.Sin(angle1)) * innerRadius + sep1;
            vertices[1] = new Vector2(Mathf.Cos(angle1), Mathf.Sin(angle1)) * outerRadius + sep1;
            vertices[2] = new Vector2(Mathf.Cos(angle2), Mathf.Sin(angle2)) * (outerRadius + Separation);
            vertices[3] = new Vector2(Mathf.Cos(angle3), Mathf.Sin(angle3)) * outerRadius + sep2;
            vertices[4] = new Vector2(Mathf.Cos(angle3), Mathf.Sin(angle3)) * innerRadius + sep2;

            var color = new Color(0.1F, 0.1F, 0.1F, isSelected ? 0.7F : 0.4F);
            for (var j = 0; j < colors.Length; j++) colors[j] = color;

            DrawPolygon(vertices, colors, antialiased: true);

            var items = GetItems();
            if ((i < items.GetChildCount()) && (items.GetChild(i)?.GetNodeOrNull("Icon") is Sprite sprite)) {
                var pos = new Vector2(Mathf.Cos(angle2), Mathf.Sin(angle2)) * (innerRadius + outerRadius) / 2;
                if (sprite.Centered) pos -= sprite.Texture.GetSize() / 2;
                DrawTexture(sprite.Texture, sprite.Offset + pos, sprite.Modulate);
            }
        }
    }
}
