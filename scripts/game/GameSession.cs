using System.Text.Json;
using Godot;

public partial class GameSession : Node
{
    public NodePath SubViewportPath = "/root/main/HUD/HBoxContainer/SubViewportContainer/SubViewport";
    private Node2D _gameRoot;
    private SubViewport _svp;
    private PackedScene charScene = ResourceLoader.Load<PackedScene>("res://data/scenes/player_character.tscn");
    private PlayerCharacter _character;
    private LevelDirector _director = new LevelDirector();
    private BulletRenderer _renderer;
    private UIManager _ui;
    private PlayingField _playingField;
    private string _lastStartedLevelDirectory, _lastStartedLevelId;
    private int score, health, graze;
    public override void _Ready()
    {
        _ui = GetNode<UIManager>("/root/UIManager");
        _svp = GetNode<SubViewport>(SubViewportPath);
        _playingField = GetNode<PlayingField>("/root/PlayingField");
        SetPhysicsProcess(false);
        SetProcess(false);
    }
    public void StopLevel()
    {
        SetPhysicsProcess(false);
        SetProcess(false);
        _gameRoot.QueueFree();
        _ui.ScoreSummary.SetHP(health);
        _ui.ScoreSummary.SetScore(score);
        _ui.ScoreSummary.SetGraze(graze);
        _ui.ShowScoreSummary();
    }
    public bool ResetLevel()
    {
        SetPhysicsProcess(false);
        SetProcess(false);
        _gameRoot.QueueFree();
        return StartLevel(_lastStartedLevelDirectory, _lastStartedLevelId);
    }
    public bool StartLevel(string levelsDirectory, string levelId)
    {
        var levelData = ReadJson<LevelData>(levelsDirectory+levelId+"/leveldata.json");
        GD.Print(levelData);
        if (levelData == null) return false;
        _gameRoot = new Node2D { Name = "GameRoot" };
        _svp.AddChild(_gameRoot);
        var compiled = LevelCompiler.CompileLevel(levelData, _gameRoot);
        if (compiled == null) return false;
        _character = charScene.Instantiate<PlayerCharacter>();
        _gameRoot.AddChild(_character);
        _character.ScreenResolution = PlayingField.Resolutions[levelData.AspectRatio];
        _playingField.SetRatio(levelData.AspectRatio, _gameRoot);
        health = levelData.Health;
        score = 0;
        graze = 0;
        _ui.HUD.SetHealth(health);
        _ui.HUD.SetGraze(0);
        _ui.HUD.SetScore(0);
        _ui.HUD.SetLevelName(levelId);
        _ui.ShowHUD();
        _renderer = new BulletRenderer(compiled);
        _director.StartLevel(compiled, _character);
        SetPhysicsProcess(true);
        SetProcess(true);
        return true;
    }
    public override void _PhysicsProcess(double dt)
    {
        _character.Movement(dt);
        _director.Tick(dt);
        _character.VisualFeedback();
    }
    public override void _Process(double dt)
    {
        _renderer.Sync(_director.Bullets, _director.BulletCount);
    }
    private static T ReadJson<T>(string path)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr($"[LevelLoader] Could not open file: {path}  (error: {FileAccess.GetOpenError()})");
            return default;
        }
        try { return JsonSerializer.Deserialize<T>(file.GetAsText()); }
        catch (JsonException ex)
        {
            GD.PrintErr($"[LevelLoader] JSON parse error in '{path}': {ex.Message}");
            return default;
        }
    }
}