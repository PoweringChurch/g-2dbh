using Godot;
using System.Text.Json;

public partial class LevelSelect : CanvasLayer
{
    [Export] protected VBoxContainer _levelList;
    [Export] protected Button _returnButton;
    [Export] protected LevelDisplay _levelDisplay;
    [Export] protected PackedScene levelButton;
    [Export] protected Button _newLevelButton;
    [Export] protected Button _refreshButton;
    [Export] protected Button _openLevelsFolder;
    private Editor e;

    private const string levelDirectory = "user://data/levels/";
    public override void _Ready()
    {
        e = GetNode<Editor>("/root/Editor");
        _returnButton.Pressed += OnReturnPressed;
        _levelDisplay.Repopulate += PopulateList;
        _refreshButton.Pressed  += PopulateList;
        _openLevelsFolder.Pressed += OpenLevelsFolder;
        _newLevelButton.Pressed += OnNewLevelPressed;

    }
    // List
    public void PopulateList()
    {
        _levelDisplay.ShowLevel(null);
        // clear existing buttons
        foreach (Node child in _levelList.GetChildren())
            child.QueueFree();

        var dir = DirAccess.Open(levelDirectory);
        dir.ListDirBegin();
        string entry = dir.GetNext();
        while (entry != "")
        {
            if (dir.CurrentIsDir() && !entry.StartsWith("."))
                AddLevelButton(entry);
            entry = dir.GetNext();
        }
        dir.ListDirEnd();
        Console.Inst.Log($"[Level Select] Populated level list");
    }
    protected virtual void AddLevelButton(string levelName)
    {
        var levelData = ReadJson<LevelData>($"{levelDirectory}{levelName}/leveldata.json");
        bool invalid = levelData == null;
        if (invalid) return;
        var titleText = string.IsNullOrWhiteSpace(levelData.DisplayName) ? "unnamed" : levelData.DisplayName;
        levelData.LevelPath = $"{levelDirectory}{levelName}/";
        var btn = levelButton.Instantiate<LevelButton>();
        btn.levelName.Text = titleText;
        btn.author.Text = "by "+levelData.Author;
        btn.duration.Text = $"{levelData.Duration:F2}";
        btn.Pressed += () => {_levelDisplay.ShowLevel(levelData);};
        _levelList.AddChild(btn);
    }
    private void OpenLevelsFolder()
    {
        var path = ProjectSettings.GlobalizePath(levelDirectory);
		OS.ShellOpen(path);
    }
    protected void OnNewLevelPressed()
    {
        var ui = GetNode<UIManager>("/root/UIManager");
        ui.ShowEditor();
        e.NewLevel();
        PopulateList();
    }
    protected void OnReturnPressed()
    {
        var ui = GetNode<UIManager>("/root/UIManager");
        ui.ShowMainMenu();
        PopulateList();
        _levelDisplay.ShowLevel(null);
    }
    protected static T ReadJson<T>(string path)
    {
        if (!FileAccess.FileExists(path)) return default;
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null) return default;
        try { return JsonSerializer.Deserialize<T>(file.GetAsText()); }
        catch { return default; }
    }
}