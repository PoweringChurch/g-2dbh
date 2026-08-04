using System;
using Godot;
public partial class GameSession : Node
{
    public static GameSession Instance;
    public const float StartDelay = 2;
    public const float FadeOutTime = 2;
    public double Elapsed
    {
        get
        {
            if (_director == null) return 0;
            return _director.Elapsed;
        }
    }
    private static string[] Characters =
    {
        "default.png", "chinese.png"
    };
    [Export] public SubViewport Svp;
    [Export] public GameAudioPlayer GAP { get; private set;}
    public Node2D GameRoot {get; private set;}
    private PackedScene charScene = ResourceLoader.Load<PackedScene>("res://data/scenes/player_character.tscn");
    private PlayerCharacter _character;
    private LevelDirector _director;
    private BulletRenderer _renderer;
    private UIManager ui => UIManager.Instance;
    private LevelData _lastLevelData;
    private PlayingField playingField;
    private StartParams _lastStartParams;
    private int score, health, graze;
    private float maxHealth, duration;
    private bool running = false;
    public bool Running => running;
    public override void _EnterTree() =>
        Instance = this;
    public override void _Ready()
    {
        playingField = GetNode<PlayingField>("/root/PlayingField");
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
        Svp.AddChild(GameRoot);
        // compile
        var compiled = LevelCompiler.CompileLevel(levelData, GameRoot);
        if (compiled == null) return false;
        // setup character
        _character = charScene.Instantiate<PlayerCharacter>();
        _character.ApplyTextureOfName(Characters[levelData.Character]);
        GameRoot.AddChild(_character);
        var resolution = PlayingField.Resolutions[levelData.AspectRatio];
        _character.ScreenResolution = resolution;
        _character.Position = new Vector2(
            resolution.X / 2, 
            resolution.Y * 0.9f);
        playingField.SetRatio(levelData.AspectRatio, GameRoot);

        score = 0;
        graze = 0;
        duration = levelData.Duration;
        maxHealth = levelData.Health;
        health = levelData.Health;
        if (startParams.Healthy) health = Math.Max(3, health*2);
        else if (startParams.Perfectionist) health = 1;
        
        // setup ui
        ui.HUD.SetHealth(health);
        ui.HUD.SetGraze(0);
        ui.HUD.SetScore(0);
        ui.HUD.SetLevelName(levelData.DisplayName);
        ui.HUD.SetDuration(levelData.Duration);
        ui.HUD.SetMods(startParams);
        _character.OnHurt += OnHurt;
        _character.OnGraze += OnGraze;
        ui.ShowHUD();
        // start
        _lastLevelData = levelData;
        _lastStartParams = startParams;
        _renderer = new BulletRenderer(compiled.Projectiles, PlayingField.Resolutions[levelData.AspectRatio], GameRoot);

        float multiplier = 1f;
        if (startParams.Slower) multiplier = 2/3f;
        else if (startParams.Faster) multiplier = 3/2f;

        GAP.VolumeLinear = AudioUtils.MusicVolume;
        GAP.Stream = AudioUtils.LoadAudio(LevelCompiler.SongData[levelData.MusicId].StreamPath);
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
        ui.ScoreSummary.SetHP(health);
        ui.ScoreSummary.SetScore(score);
        ui.ScoreSummary.SetGraze(graze);
        ui.ScoreSummary.Visible = true;
    }
    private AudioStream grazeSfx = AudioUtils.LoadAudio("res://data/sounds/graze.wav");
    private AudioStream hurtSfx = AudioUtils.LoadAudio("res://data/sounds/hurt.wav");
    public void OnHurt()
    {
        health--;
        if (!_lastStartParams.Paranoid) score -= Math.Max((int)(100*(health+1)/maxHealth), 0);
        ui.HUD.SetHealth(health);
        ui.HUD.SetScore(score);
        if (health <= 0)
            StopLevel();
        AudioUtils.Instance.PlayAudio(hurtSfx, AudioUtils.SFXVolume);
    }
    public void OnGraze()
    {
        graze++;
        if (_lastStartParams.Paranoid && _character.Hurt() && !ConfigHelper.Current.NoHit)
            return;
        score += (int)(100*health/maxHealth);
        ui.HUD.SetScore(score);
        ui.HUD.SetGraze(graze);
        AudioUtils.Instance.PlayAudio(grazeSfx, AudioUtils.SFXVolume);
    }
    public override void _PhysicsProcess(double dt)
    {
        if (!running) return;
        _character.Movement(dt);
        ui.HUD.SetCompletion((float)(_director.Elapsed/duration));
        _director.Tick(dt);
        _character.VisualFeedback();
    }
    public override void _Process(double dt)
    {
        Overlay.Inst.SyncInfo(-1, _director.QueuedCount, _director.ActiveCount);
        if (!running) return;
        _renderer.Sync(ref _director.ActiveProjectiles, _director.ActiveCount, _director.Elapsed);
        // fade out in last two seconds
        if (Elapsed >= duration - FadeOutTime)
        {
            float t = (float)Math.Max(0, duration - Elapsed) / FadeOutTime;
            GAP.VolumeLinear = AudioUtils.MusicVolume * t;
        }
    }
}