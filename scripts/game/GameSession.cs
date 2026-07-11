using System;
using System.Text.Json;
using Godot;
public partial class GameSession : Node
{
    public const float StartDelay = 2;
    public GameAudioPlayer GAP {get; private set;}
    public double Elapsed
    {
        get
        {
            if (_director == null) return 0;
            return _director.Elapsed;
        }
    }
    public static NodePath SubViewportPath = "/root/main/HUD/Sort/SubViewportContainer/SubViewport";
    public static NodePath GameAudioPlayerPath = "/root/main/HUD/GameAudioPlayer";
    public static NodePath BackgroundImagePath = SubViewportPath+"/BackgroundImage";
    public Node2D GameRoot {get; private set;}
    private SubViewport _svp;
    private PackedScene charScene = ResourceLoader.Load<PackedScene>("res://data/scenes/player_character.tscn");
    private PlayerCharacter _character;
    private LevelDirector _director;
    private BulletRenderer _renderer;
    private UIManager _ui;
    private LevelData _lastLevelData;
    private PlayingField _playingField;
    private StartParams _lastStartParams;
    private int score, health, graze;
    private float maxHealth, duration;
    private bool running = false;
    public bool Running => running;
    public override void _Ready()
    {
        _ui = GetNode<UIManager>("/root/UIManager");
        _svp = GetNode<SubViewport>(SubViewportPath);
        GAP = GetNode<GameAudioPlayer>(GameAudioPlayerPath);
        _playingField = GetNode<PlayingField>("/root/PlayingField");
        SetPhysicsProcess(false);
        SetProcess(false);
    }
    public void Abort()
    {
        SetPhysicsProcess(false);
        SetProcess(false);
        GAP.Stop();
        GAP.Stream = null;
        if (_director!=null)
            _director.LevelFinished -= StopLevel;
        _director = null;
        _renderer = null;
        if (IsInstanceValid(_character))
        {
            _character.OnHurt -= OnHurt;
            _character.OnGraze -= OnGraze;
        }
        if (IsInstanceValid(GameRoot))
            GameRoot.QueueFree();
        Input.MouseMode = Input.MouseModeEnum.Visible;
        running = false;
        PlaylistHandler.Instance.FadeIn();
    }
    public bool ResetLevel()
    {
        SetPhysicsProcess(false);
        SetProcess(false);
        if (IsInstanceValid(GameRoot))
            GameRoot.QueueFree();
        return StartLevel(_lastLevelData, _lastStartParams);
    }
    public bool StartLevel(LevelData levelData, StartParams startParams)
    {
        // get level data
        if (levelData == null) return false;
        GameRoot = new Node2D { Name = "GameRoot" };
        _svp.AddChild(GameRoot);
        // compile
        var compiled = LevelCompiler.CompileLevel(levelData, GameRoot);
        if (compiled == null) return false;
        // setup character
        _character = charScene.Instantiate<PlayerCharacter>();
        GameRoot.AddChild(_character);
        var resolution = PlayingField.Resolutions[levelData.AspectRatio];
        _character.ScreenResolution = resolution;
        _character.Position = new Vector2(
            resolution.X / 2, 
            resolution.Y * 0.9f);
        _playingField.SetRatio(levelData.AspectRatio, GameRoot);

        score = 0;
        graze = 0;
        duration = levelData.Duration;
        maxHealth = levelData.Health;
        health = levelData.Health;
        if (startParams.Healthy) health = Math.Max(3, health*2);
        else if (startParams.Perfectionist) health = 1;
        
        // setup ui
        _ui.HUD.SetHealth(health);
        _ui.HUD.SetGraze(0);
        _ui.HUD.SetScore(0);
        _ui.HUD.SetLevelName(levelData.DisplayName);
        _ui.HUD.SetDuration(levelData.Duration);
        _ui.HUD.SetMods(startParams);
        _character.OnHurt += OnHurt;
        _character.OnGraze += OnGraze;
        _ui.ShowHUD();
        // start
        _lastLevelData = levelData;
        _lastStartParams = startParams;
        _renderer = new BulletRenderer(compiled, this);

        float multiplier = 1f;
        if (startParams.Slower) multiplier = 2/3f;
        else if (startParams.Faster) multiplier = 3/2f;

        GAP.VolumeLinear = ConfigHelper.Current.MusicVolume*AudioUtils.MusicVolumeMultiplier;
        GAP.Stream = AudioUtils.LoadAudio(levelData.LevelPath+"/audio/", levelData.Music);
        GAP.PitchScale = multiplier;
        PlaylistHandler.Instance.FadeOut();
        
        _director = new LevelDirector();
        _director.LevelFinished += StopLevel;

        Input.MouseMode = Input.MouseModeEnum.ConfinedHidden;
        _director.StartLevel(compiled, _character, multiplier, StartDelay*multiplier);
        GAP.PlayAfterDelay(StartDelay*multiplier);
        SetPhysicsProcess(true);
        SetProcess(true);
        running = true;
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
        if (!_lastStartParams.Paranoid) score -= Math.Max((int)(100*(health+1)/maxHealth), 0);
        _ui.HUD.SetHealth(health);
        _ui.HUD.SetScore(score);
        if (health <= 0)
            StopLevel();
        AudioUtils.Instance.PlayAudio("hurt", ConfigHelper.Current.HurtVolume);
    }
    public void OnGraze()
    {
        graze++;
        if (_lastStartParams.Paranoid && _character.Hurt() && !ConfigHelper.Current.NoHit)
            return;
        score += (int)(100*health/maxHealth);
        _ui.HUD.SetScore(score);
        _ui.HUD.SetGraze(graze);
        AudioUtils.Instance.PlayAudio("graze", ConfigHelper.Current.GrazeVolume*0.5f);
    }
    public override void _PhysicsProcess(double dt)
    {
        if (!running) return;
        _character.Movement(dt);
        _ui.HUD.SetCompletion((float)(_director.Elapsed/duration));
        _director.Tick(dt);
        _character.VisualFeedback();
    }
    public override void _Process(double dt)
    {
        Overlay.Inst.SyncInfo(-1, _director.QueuedCount, _director.ActiveCount);
        if (!running) return;
        _renderer.Sync(ref _director.ActiveProjectiles, _director.ActiveCount, _director.Elapsed);
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