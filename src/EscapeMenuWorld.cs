using System;
using System.Text;
using Godot;
using static Godot.NetworkedMultiplayerPeer;

public class EscapeMenuWorld : CenterContainer
{
    [Export] public NodePath FilenamePath { get; set; }
    [Export] public NodePath LastSavedPath { get; set; }
    [Export] public NodePath PlaytimePath { get; set; }
    [Export] public NodePath QuickSavePath { get; set; }
    [Export] public NodePath SaveAsPath { get; set; }
    [Export] public NodePath SaveFileDialogPath { get; set; }
    [Export] public NodePath LoadFileDialogPath { get; set; }

    public Label FilenameLabel { get; private set; }
    public Label LastSavedLabel { get; private set; }
    public Label PlaytimeLabel { get; private set; }
    public Button QuickSaveButton { get; private set; }
    public Button SaveAsButton { get; private set; }
    public FileDialog SaveFileDialog { get; private set; }
    public FileDialog LoadFileDialog { get; private set; }

    private string _currentWorld;

    public override void _Ready()
    {
        FilenameLabel  = GetNode<Label>(FilenamePath);
        LastSavedLabel = GetNode<Label>(LastSavedPath);
        PlaytimeLabel  = GetNode<Label>(PlaytimePath);
        QuickSaveButton = GetNode<Button>(QuickSavePath);
        SaveAsButton    = GetNode<Button>(SaveAsPath);
        SaveFileDialog = GetNode<FileDialog>(SaveFileDialogPath);
        LoadFileDialog = GetNode<FileDialog>(LoadFileDialogPath);

        // TODO: Reset this when going back to singleplayer after having connected to a multiplayer server.
        QuickSaveButton.Visible = false;
        SaveAsButton.Text = "Save World As...";
        SaveFileDialog.GetOk().Text = "Save";

        new Directory().MakeDirRecursive(World.WORLDS_DIR);
        SaveFileDialog.CurrentPath = World.WORLDS_DIR;
        LoadFileDialog.CurrentPath = World.WORLDS_DIR;

        this.GetClient().StatusChanged += OnStatusChanged;
    }

    public override void _Process(float delta)
    {
        // TODO: Probably move this to World class.
        var world = this.GetWorld();
        if (!GetTree().Paused || (world.PauseMode != PauseModeEnum.Stop))
            world.Playtime += TimeSpan.FromSeconds(delta);

        var b = new StringBuilder();
        var p = world.Playtime;
        if (p.Days > 0) b.Append(p.Days).Append("d ");
        if (p.Hours > 0) b.Append(p.Hours).Append("h ");
        if (p.Minutes < 10) b.Append('0'); b.Append(p.Minutes).Append("m ");
        if (p.Seconds < 10) b.Append('0'); b.Append(p.Seconds).Append("s");
        PlaytimeLabel.Text = b.ToString();
    }

    private void OnStatusChanged(ConnectionStatus status)
    {
        var server = this.GetClient().GetNode<IntegratedServer>(nameof(IntegratedServer));
        GetParent<TabContainer>().SetTabDisabled(GetIndex(), !server.Server.IsRunning);
    }


    #pragma warning disable IDE0051
    #pragma warning disable IDE1006

    private void _on_QuickSave_pressed()
        => _on_SaveFileDialog_file_selected(_currentWorld);

    private void _on_SaveAs_pressed()
    {
        SaveFileDialog.Invalidate();
        SaveFileDialog.PopupCenteredClamped(new Vector2(480, 320), 0.85F);
    }

    private void _on_SaveFileDialog_file_selected(string path)
    {
        var server = this.GetClient().GetNode<IntegratedServer>(nameof(IntegratedServer)).Server;
        var world  = server.GetWorld();
        world.Save(path);

        _currentWorld = path;
        FilenameLabel.Text  = System.IO.Path.GetFileName(path);
        LastSavedLabel.Text = world.LastSaved.ToString("yyyy-MM-dd HH:mm");
        QuickSaveButton.Visible = true;
        SaveAsButton.Text = "Save As...";
    }

    private void _on_LoadFrom_pressed()
    {
        LoadFileDialog.Invalidate();
        LoadFileDialog.PopupCenteredClamped(new Vector2(480, 320), 0.85F);
    }

    private void _on_LoadFileDialog_file_selected(string path)
    {
        var server = this.GetClient().GetNode<IntegratedServer>(nameof(IntegratedServer)).Server;
        var world  = server.GetWorld();

        foreach (var player in world.Players) {
            // Reset players' positions.
            // Can't use RPC helper method here since player is not a LocalPlayer here.
            player.RsetId(player.NetworkID, "position", Vector2.Zero);
            player.RsetId(player.NetworkID, nameof(Player.Velocity), Vector2.Zero);
            // Reset the visbility tracker so the client will receive new chunks.
            player.VisibilityTracker.Reset();
        }

        world.Load(path);

        _currentWorld = path;
        FilenameLabel.Text  = System.IO.Path.GetFileName(path);
        LastSavedLabel.Text = world.LastSaved.ToString("yyyy-MM-dd HH:mm");
        QuickSaveButton.Visible = true;
        SaveAsButton.Text = "Save As...";
    }
}
