using Godot;
using System.Text.Json;

public partial class LevelSelect : CanvasLayer
{
    [Export] protected VBoxContainer _levelList;
    [Export] protected Button _returnButton;
    [Export] protected LevelDisplay _levelDisplay;
    [Export] protected PackedScene levelButton;
    protected string _levelDirectory = "res://data/levels/";
    public override void _Ready()
    {
        _returnButton.Pressed += OnReturnPressed;
        _levelDisplay.Repopulate += PopulateList;
    }
    // List
    public void PopulateList()
    {
        _levelDisplay.ShowLevel(null);
        // clear existing buttons
        foreach (Node child in _levelList.GetChildren())
            child.QueueFree();

        var dir = DirAccess.Open(_levelDirectory);
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
        var levelData = ReadJson<LevelData>($"{_levelDirectory}{levelName}/leveldata.json");
        bool invalid = levelData == null;
        if (invalid) return;
        var titleText = string.IsNullOrWhiteSpace(levelData.DisplayName) ? "unnamed" : levelData.DisplayName;
        levelData.LevelPath = $"{_levelDirectory}{levelName}/";
        var btn = levelButton.Instantiate<LevelButton>();
        btn.levelName.Text = titleText;
        btn.author.Text = "by "+levelData.Author;
        btn.duration.Text = $"{levelData.Duration:F2}";
        btn.Pressed += () => {_levelDisplay.ShowLevel(levelData);};
        _levelList.AddChild(btn);
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