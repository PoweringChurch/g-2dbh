using Godot;
using System.Text.Json;

public partial class LevelSelect : CanvasLayer
{
    [Export] protected VBoxContainer _levelList;      // sidebar scroll container's VBox
    [Export] protected Label _previewId;
    [Export] protected Label _previewRatio;
    [Export] protected Label _previewHealth;
    [Export] protected Label _previewDuration;
    [Export] protected Button _returnButton;
    [Export] protected Button _playButton;

    protected GameSession _gameSession;
    protected string _selectedLevel;
    protected string _levelDirectory = "res://data/levels/";
    public override void _Ready()
    {
        _gameSession = GetNode<GameSession>("/root/GameSession");
        _playButton.Pressed += OnPlayPressed;
        _playButton.Disabled = true;
        _returnButton.Pressed += OnReturnPressed;
    }
    // List
    public void PopulateList()
    {
        ClearPreview();
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
        Console.Instance.Log($"[Level Select] Populated level list");
    }

    protected virtual void AddLevelButton(string levelName)
    {
        var levelData = ReadJson<LevelData>($"{_levelDirectory}{levelName}/leveldata.json");
        bool invalid = levelData == null;
        var buttonText = "INVALID LEVEL";
        if (!invalid)
            buttonText = string.IsNullOrWhiteSpace(levelData.DisplayName) ? "unnamed" : levelData.DisplayName;
        var btn = new Button { Text = buttonText, ToggleMode = true, CustomMinimumSize = new Vector2(150, 0), ClipText = true };
        if (invalid)
            btn.Pressed += ClearPreview;
        btn.Pressed += () => OnLevelSelected(levelData, btn);
        _levelList.AddChild(btn);
    }

    // Selection
    protected virtual void OnLevelSelected(LevelData levelData, Button pressed)
    {
        foreach (Node child in _levelList.GetChildren())
            if (child is Button btn && btn != pressed)
                btn.ButtonPressed = false;
        _selectedLevel = levelData.LevelId;
        _playButton.Disabled = false;
        LoadPreview(levelData);
    }
    protected void LoadPreview(LevelData levelData)
    {
        string ratioLabel = levelData.AspectRatio switch
        {
            0 => "9:16",
            1 => "1:1",
            2 => "3:2",
            _ => "unknown"
        };
        _previewId.Text = string.IsNullOrWhiteSpace(levelData.DisplayName) ? "unnamed" : levelData.DisplayName;
        _previewRatio.Text = ratioLabel;
        _previewHealth.Text = levelData.Health.ToString();
        _previewDuration.Text = $"{levelData.Duration:F2}s";
    }
    protected virtual void ClearPreview()
    {
        _previewId.Text = "-";
        _previewRatio.Text = "-";
        _previewHealth.Text = "-";
        _previewDuration.Text = "-";
        _playButton.Disabled = true;
    }
    // Play
    protected void OnPlayPressed()
    {
        if (string.IsNullOrEmpty(_selectedLevel)) return;
        bool success = _gameSession.StartLevel(_levelDirectory, _selectedLevel);
        if (!success)
        {
            Show();
            Popups.Instance.Show(Popups.DefaultType.OK, $"Something went wrong opening this level. \n Level Id : {_selectedLevel}");
        }
    }
    protected void OnReturnPressed()
    {
        var ui = GetNode<UIManager>("/root/UIManager");
        ui.ShowMainMenu();
        PopulateList();
        ClearPreview();
    }
    // Helpers
    protected T ReadJson<T>(string path)
    {
        if (!FileAccess.FileExists(path)) return default;
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null) return default;
        try { return JsonSerializer.Deserialize<T>(file.GetAsText()); }
        catch { return default; }
    }
}