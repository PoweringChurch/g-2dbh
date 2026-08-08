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
            if (director == null) return 0;
            return director.Elapsed;
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
    private PlayerCharacter character;
    private LevelDirector director;
    private BulletRenderer _renderer;
    private UIManager ui => UIManager.Instance;
    private RawLevelData lastLevelData;
    private PlayingField playingField;
    private StartParams lastStartParams;
    private const int maxHealth = 3;
    private int health, graze;
    private float duration;
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
        if (director!=null)
            director.LevelFinished -= StopLevel;
        director = null;
        _renderer = null;
        if (IsInstanceValid(character))
        {
            character.OnHurt -= OnHurt;
            character.OnGraze -= OnGraze;
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
        return StartLevel(lastLevelData, lastStartParams);
    }
    public bool StartLevel(RawLevelData levelData, StartParams startParams)
    {
        Console.Log($"[GameSession] Starting level '{levelData.Name}' by '{levelData.Author}'");
        // get level data
        if (levelData == null) return false;
        GameRoot = new Node2D { Name = "GameRoot" };
        Svp.AddChild(GameRoot);
        // compile
        var compiled = LevelCompiler.CompileLevel(levelData, GameRoot);
        if (compiled == null) return false;
        // setup character
        character = charScene.Instantiate<PlayerCharacter>();
        character.ApplyTextureOfName(Characters[levelData.Character]);
        GameRoot.AddChild(character);
        var resolution = PlayingField.Resolutions[levelData.AspectRatio];
        character.ScreenResolution = resolution;
        character.Position = new Vector2(
            resolution.X / 2, 
            resolution.Y * 0.9f);
        playingField.SetRatio(levelData.AspectRatio, GameRoot);

        graze = 0;
        duration = levelData.Duration;
        health = maxHealth;
        if (startParams.Healthy) health = Math.Max(3, health*2);
        else if (startParams.Perfectionist) health = 1;
        
        // setup ui
        ui.HUD.SetHealth(health);
        ui.HUD.SetGraze(graze);
        ui.HUD.SetCompletion(0);
        ui.HUD.SetElapsed(0);

        ui.HUD.SetLevelName(levelData.Name);
        ui.HUD.SetMods(startParams);

        character.OnHurt += OnHurt;
        character.OnGraze += OnGraze;

        ui.ResetScoreSummary();
        ui.ShowHUD();
        // start
        lastLevelData = levelData;
        lastStartParams = startParams;
        _renderer = new BulletRenderer(compiled.Projectiles, PlayingField.Resolutions[levelData.AspectRatio], GameRoot);

        float multiplier = 1f;
        if (startParams.Slower) multiplier = 2/3f;
        else if (startParams.Faster) multiplier = 3/2f;

        GAP.VolumeLinear = AudioUtils.MusicVolume;
        GAP.Stream = AudioUtils.LoadAudio(LevelCompiler.SongData[levelData.MusicId].StreamPath);
        GAP.PitchScale = multiplier;
        PlaylistHandler.Instance.FadeOut();
        
        director = new LevelDirector();
        director.LevelFinished += StopLevel;

        Input.MouseMode = Input.MouseModeEnum.ConfinedHidden;
        director.StartLevel(compiled, character, multiplier, StartDelay*multiplier);
        GAP.PlayAfterDelay(StartDelay*multiplier);
        SetPhysicsProcess(true);
        SetProcess(true);
        running = true;
        return true;
    }
    public void StopLevel()
    {
        ui.ShowScoreSummary((float)(director.Elapsed/duration), health, graze, lastLevelData.Name, lastLevelData.Author);
        Abort();
    }
    public void OnHurt()
    {
        health--;
        ui.HUD.SetHealth(health);
        if (health <= 0)
            StopLevel();
        AudioUtils.PlayAudio("res://data/sounds/player/hurt.wav", AudioUtils.SFXVolume);
    }
    public void OnGraze()
    {
        if (lastStartParams.Paranoid && character.Hurt() && !ConfigHelper.Current.NoHit)
            return;
        graze++;
        ui.HUD.SetGraze(graze);
        AudioUtils.PlayAudio("res://data/sounds/player/graze.wav", AudioUtils.SFXVolume);
    }
    public override void _PhysicsProcess(double dt)
    {
        if (!running) return;
        character.Movement(dt);
        if (director.Elapsed >= 0) 
        { 
            ui.HUD.SetCompletion((float)(director.Elapsed/duration)); 
            ui.HUD.SetElapsed((float)director.Elapsed); 
        }
        director.Tick(dt);
        character.VisualFeedback();
    }
    public override void _Process(double dt)
    {
        Overlay.Inst.SyncInfo(-1, director.QueuedCount, director.ActiveCount);
        if (!running) return;
        _renderer.Sync(ref director.ActiveProjectiles, director.ActiveCount, director.Elapsed);
        if (Elapsed >= duration - FadeOutTime)
        {
            float t = (float)Math.Max(0, duration - Elapsed) / FadeOutTime;
            GAP.VolumeLinear = AudioUtils.MusicVolume * t;
        }
    }
}