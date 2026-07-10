using Godot;
using System;

public partial class LevelDisplay : Control
{
    [Export] bool HideEditAndDelete = false;
    [Export] Label LevelName;
    [Export] Label Author;
    [Export] Label HP;
    [Export] Label Duration;
    [Export] Label AspectRatio;
    [Export] Button Play;
    [Export] Button Edit;
    [Export] Button Delete;
    [Export] Modifiers Mods;
    private LevelData toPlay;
    private GameSession gs;
    private Editor e;
    private UIManager ui;
    [Signal] public delegate void RepopulateEventHandler();
    public override void _Ready()
    {
        gs = GetNode<GameSession>("/root/GameSession");
        ui = GetNode<UIManager>("/root/UIManager");
        e = GetNode<Editor>("/root/Editor");
        if (HideEditAndDelete)
        {
            Edit.Visible = false;
            Delete.Visible = false;
        }
        Play.Pressed += OnPlay;
        Delete.Pressed += OnDelete;
        Edit.Pressed += OnEdit;
    }
    public void ShowLevel(LevelData level)
    {
        toPlay = level;
        if (level == null)
        {
            LevelName.Text = "-";
            Author.Text = "-";
            HP.Text = "-";
            Duration.Text = "-";
            AspectRatio.Text = "-";
            return;
        }
        LevelName.Text = level.DisplayName;
        Author.Text = level.Author;
        HP.Text = $"{level.Health} hp";
        Duration.Text = $"{level.Duration:F2}s";
        string ratioLabel = level.AspectRatio switch
        {
            0 => "9:16",
            1 => "1:1",
            2 => "3:2",
            _ => "invalid"
        };
        AspectRatio.Text = ratioLabel;
    }
    private void OnPlay()
    {
        if (toPlay == null) return;
        gs.StartLevel(toPlay, Mods.GetStartParams());
    }
    private void OnDelete()
    {
        if (toPlay == null) return;
        var levelDirectory = ProjectSettings.GlobalizePath($"{toPlay.LevelPath}");
        if (!DirAccess.DirExistsAbsolute(levelDirectory))
        {
            Console.Inst.LogErr($"[Editor Level Select] Could not find level director {levelDirectory}");
            return;
        }
        var popup = Popups.Instance.Show(Popups.DefaultType.YN, "Are you sure you want to delete this level?");
        popup.CloseOnPress = true;
        popup.Options[0].Pressed += () => 
        {
            using var dir = DirAccess.Open(levelDirectory);
            if (DirAccess.DirExistsAbsolute(levelDirectory))
            {
                DeleteDirectoryRecursive(levelDirectory);
            }
            DirAccess.RemoveAbsolute(levelDirectory);
            ShowLevel(null);
            EmitSignal(SignalName.Repopulate);
        };
    }
    private static void DeleteDirectoryRecursive(string path)
    {
        using var dir = DirAccess.Open(path);
        if (dir == null) return;
        dir.ListDirBegin();
        string fileName = dir.GetNext();
        while (fileName != "")
        {
            if (fileName != "." && fileName != "..")
            {
                string fullPath = $"{path}/{fileName}";

                if (dir.CurrentIsDir())
                    DeleteDirectoryRecursive(fullPath);
                else
                    DirAccess.RemoveAbsolute(fullPath);
            }
            fileName = dir.GetNext();
        }

        dir.ListDirEnd();
        DirAccess.RemoveAbsolute(path);
    }
    private void OnEdit()
    {
        ui.ShowEditor();
        bool success = e.OpenLevel(toPlay);
        Editor.Open = success;
        if (!success)
        {
            ui.ShowCustoms();
            Popups.Instance.Show(Popups.DefaultType.OK, "Something went wrong opening this level");
        }
    } 
}
