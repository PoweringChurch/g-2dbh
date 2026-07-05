using System.Text.Json;
using Godot;

public partial class GameSession : Node
{
    public AudioStreamPlayer GAP {get; private set;}
    public double Elapsed
    {
        get => _director.Elapsed;
    }
    public NodePath SubViewportPath = "/root/main/HUD/Sort/SubViewportContainer/SubViewport";
    public NodePath GameAudioPlayerPath = "/root/main/HUD/GameAudioPlayer";
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
    private float maxHealth, duration;
    public override void _Ready()
    {
        _ui = GetNode<UIManager>("/root/UIManager");
        _svp = GetNode<SubViewport>(SubViewportPath);
        GAP = GetNode<AudioStreamPlayer>(GameAudioPlayerPath);
        _playingField = GetNode<PlayingField>("/root/PlayingField");
        SetPhysicsProcess(false);
        SetProcess(false);
        _director.LevelFinished += StopLevel;
    }
    public void Abort()
    {
        SetPhysicsProcess(false);
        SetProcess(false);
        GAP.Stop();
        GAP.Stream = null;
        if (IsInstanceValid(_character))
        {
            _character.OnHurt -= OnHurt;
            _character.OnGraze -= OnGraze;
        }
        if (IsInstanceValid(_gameRoot))
            _gameRoot.QueueFree();
    }
    public bool ResetLevel()
    {
        SetPhysicsProcess(false);
        SetProcess(false);
        if (IsInstanceValid(_gameRoot))
            _gameRoot.QueueFree();
        return StartLevel(_lastStartedLevelDirectory, _lastStartedLevelId);
    }
    public bool StartLevel(string levelsDirectory, string levelId)
    {
        // get level data
        var levelData = ReadJson<LevelData>(levelsDirectory+levelId+"/leveldata.json");
        if (levelData == null) return false;
        _gameRoot = new Node2D { Name = "GameRoot" };
        _svp.AddChild(_gameRoot);
        // compile
        var compiled = LevelCompiler.CompileLevel(levelData, _gameRoot);
        if (compiled == null) return false;
        // setup character
        _character = charScene.Instantiate<PlayerCharacter>();
        _gameRoot.AddChild(_character);
        var resolution = PlayingField.Resolutions[levelData.AspectRatio];
        _character.ScreenResolution = resolution;
        _character.Position = new Vector2(
            resolution.X / 2, 
            resolution.Y * 0.9f);
        _playingField.SetRatio(levelData.AspectRatio, _gameRoot);

        health = levelData.Health;
        score = 0;
        graze = 0;
        duration = levelData.Duration;
        maxHealth = levelData.Health;
        // setup ui
        _ui.HUD.SetHealth(health); // works
        _ui.HUD.SetGraze(0);
        _ui.HUD.SetScore(0);
        _ui.HUD.SetLevelName(levelData.DisplayName); // called but dont work?
        _ui.HUD.SetDuration(levelData.Duration);
        _character.OnHurt += OnHurt;
        _character.OnGraze += OnGraze;

        _ui.ShowHUD();
        // start
        _lastStartedLevelDirectory = levelsDirectory;
        _lastStartedLevelId = levelId;
        _renderer = new BulletRenderer(compiled);
        _director.StartLevel(compiled, _character);
        GAP.VolumeLinear = ConfigHelper.Current.MusicVolume;
        GAP.Stream = AudioUtils.LoadAudio(levelsDirectory+levelId+"/audio/", levelData.Music);
        GAP.Play(0);
        SetPhysicsProcess(true);
        SetProcess(true);
        return true;
    }
    public void StopLevel()
    {
        Abort();
        _ui.ScoreSummary.SetHP(health);
        _ui.ScoreSummary.SetScore(score);
        _ui.ScoreSummary.SetGraze(graze);
        _ui.ShowScoreSummary();
    }
    public void OnHurt()
    {
        health--;
        _ui.HUD.SetHealth(health);
        if (health <= 0)
            StopLevel();
        AudioUtils.Instance.PlayAudio("hurt", ConfigHelper.Current.HurtVolume);
    }
    public void OnGraze()
    {
        graze++;
        score += (int)(graze*100*health/maxHealth);
        _ui.HUD.SetScore(score);
        _ui.HUD.SetGraze(graze);
        AudioUtils.Instance.PlayAudio("graze", ConfigHelper.Current.GrazeVolume);
    }
    public override void _PhysicsProcess(double dt)
    {
        _character.Movement(dt);
        _director.Tick(dt);
        _character.VisualFeedback();
        _ui.HUD.SetCompletion((float)(_director.Elapsed/duration));
    }
    public override void _Process(double dt)
    {
        _renderer.Sync(ref _director.Bullets, _director.BulletCount);
    }
    private static T ReadJson<T>(string path)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            Console.Inst.LogErr($"[GameSession] Could not open file: {path}  (error: {FileAccess.GetOpenError()})");
            return default;
        }
        try { return JsonSerializer.Deserialize<T>(file.GetAsText()); }
        catch (JsonException ex)
        {
            Console.Inst.LogErr($"[GameSession] JSON parse error in '{path}': {ex.Message}");
            return default;
        }
    }
}