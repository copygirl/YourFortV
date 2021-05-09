using System;
using System.Text;
using Godot;
using Path = System.IO.Path;
using File = System.IO.File;
using Directory = System.IO.Directory;
using System.Linq;
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

    private TimeSpan _playtime;
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

        var worldsFolder = OS.GetUserDataDir() + "/worlds/";
        Directory.CreateDirectory(worldsFolder);
        SaveFileDialog.CurrentPath = worldsFolder;
        LoadFileDialog.CurrentPath = worldsFolder;

        this.GetClient().StatusChanged += OnStatusChanged;
    }

    public override void _Process(float delta)
    {
        if (!GetTree().Paused || (this.GetWorld().PauseMode != PauseModeEnum.Stop))
            _playtime += TimeSpan.FromSeconds(delta);

        var b = new StringBuilder();
        if (_playtime.Days > 0) b.Append(_playtime.Days).Append("d ");
        if (_playtime.Hours > 0) b.Append(_playtime.Hours).Append("h ");
        if (_playtime.Minutes < 10) b.Append('0'); b.Append(_playtime.Minutes).Append("m ");
        if (_playtime.Seconds < 10) b.Append('0'); b.Append(_playtime.Seconds).Append("s");
        PlaytimeLabel.Text = b.ToString();
    }

    private void OnStatusChanged(ConnectionStatus status)
    {
        var server = this.GetClient().GetNodeOrNull<IntegratedServer>(nameof(IntegratedServer));
        GetParent<TabContainer>().SetTabDisabled(GetIndex(), server == null);
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
        // var server = this.GetClient().GetNode<IntegratedServer>(nameof(IntegratedServer)).Server;
        // var save   = Save.CreateFromWorld(server, _playtime);
        // save.WriteToFile(path + ".tmp");
        // File.Delete(path); // TODO: In later .NET, there is a File.Move(source, dest, overwrite).
        // File.Move(path + ".tmp", path);

        // _currentWorld = path;
        // FilenameLabel.Text = Path.GetFileName(path);
        // LastSavedLabel.Text = save.LastSaved.ToString("yyyy-MM-dd HH:mm");
        // QuickSaveButton.Visible = true;
        // SaveAsButton.Text = "Save As...";
    }

    private void _on_LoadFrom_pressed()
    {
        LoadFileDialog.Invalidate();
        LoadFileDialog.PopupCenteredClamped(new Vector2(480, 320), 0.85F);
    }

    private void _on_LoadFileDialog_file_selected(string path)
    {
        // var server = this.GetClient().GetNode<IntegratedServer>(nameof(IntegratedServer)).Server;
        // var save   = Save.ReadFromFile(path);

        // // Clear out all objects that have a SaveAttribute.
        // var objectsToRemove = server.Objects.Select(x => x.Item2)
        //     .Where(x => SaveRegistry.GetOrNull(x.GetType()) != null).ToArray();
        // foreach (var obj in objectsToRemove) obj.RemoveFromParent();

        // // Reset players' positions.
        // foreach (var (id, player) in server.Players)
        //     player.RPC(new []{ id }, player.ResetPosition, Vector2.Zero);

        // save.AddToWorld(server);
        // _playtime = save.Playtime;

        // _currentWorld = path;
        // FilenameLabel.Text = Path.GetFileName(path);
        // LastSavedLabel.Text = save.LastSaved.ToString("yyyy-MM-dd HH:mm");
        // QuickSaveButton.Visible = true;
        // SaveAsButton.Text = "Save As...";
    }
}
