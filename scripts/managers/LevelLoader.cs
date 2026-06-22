// ProjectileReference.cs  (updated — rotOffset renamed to direction)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
public static class ValidExtensions
{
    public static string[] Image = [".png", ".jpg"];
}
public partial class LevelLoader : Node
{
    [Export] public NodePath SubViewportPath = "/root/main/HUD/HBoxContainer/SubViewportContainer/SubViewport";
    [Export] public string ProjectileModelsJsonPath = "/projectiles.json";
    [Export] public string LevelDataJsonPath = "/data.json";
    private Node2D _gameRoot;
    private LevelData _level;
    private List<ISpatialReference> _queue;
    private float _elapsed;
    private bool _running;
    private static string currentLevelDirectory;
    private static string currentLevelId;
    public static string CurrentLevelPath => $"{currentLevelDirectory}{currentLevelId}/";
    private UIManager _ui;

    [Signal] public delegate void LevelStartedEventHandler(string levelId);
    [Signal] public delegate void LevelFinishedEventHandler(string levelId);
    [Signal] public delegate void SpawnFailedEventHandler(string modelId, string reason);
    /// <summary>Abort the current level mid-run</summary>
    public void Abort()
    {
        _running = false;
        SetProcess(false);
        if (IsInstanceValid(_gameRoot))
            _gameRoot.QueueFree();
        _gameRoot = null;
    }
    // Lifecycle
    public override void _Ready()
    {
        SetProcess(false);  // only ticks while a level is active
        _ui = GetNode<UIManager>("/root/UIManager");
    }

    public override void _Process(double dt)
    {
        if (!_running) return;
        _elapsed += (float)dt;
        // drain every ref whose scheduled time has arrived
        while (_queue.Count > 0 && _queue[0].T <= _elapsed)
        {
            if (_queue[0] is ProjectileReference pr)
                SpawnProjectile(pr);
            else if (_queue[0] is PatternReference ptr)
                SpawnPattern(ptr);
            _queue.RemoveAt(0);
        }
        float duration = _level.Duration;
        _ui.HUD.SetCompletion(_elapsed / duration);
        _ui.HUD.SetTime(_elapsed);
        if (_elapsed > duration)
            FinishLevel();
    }

    // ── Internal ───────────────────────────────────────────────────────────
    private T ReadJson<T>(string path)
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
    public bool ResetLevel()
    {
        Abort();
        return BeginLevel(currentLevelDirectory, currentLevelId);
    }
    public bool BeginLevel(string levelsDirectory, string levelId)
    {
        currentLevelDirectory = levelsDirectory;
        currentLevelId = levelId;
        GD.Print("Validating level information...");
        // ensure level exists and has leveldata.json, models.json, projectileData.json, and images directory
        if (!DirAccess.DirExistsAbsolute(CurrentLevelPath)) // check both res and user
        {
            GD.PrintErr($"[LevelLoader] Level folder not found: {CurrentLevelPath}");
            return false;
        }
        if (!FileAccess.FileExists(CurrentLevelPath + "leveldata.json"))
        {
            GD.PrintErr($"[LevelLoader] Missing required file: {CurrentLevelPath}leveldata.json");
            return false;
        }
        if (!DirAccess.DirExistsAbsolute(CurrentLevelPath + "images/"))
        {
            GD.PrintErr($"[LevelLoader] Missing images directory: {CurrentLevelPath}images/");
            return false;
        }
        var levelData = ReadJson<LevelData>(CurrentLevelPath + "leveldata.json");
        levelData.LevelId = levelId;
        GD.Print($"Loading level {levelId}...");
        GD.Print("Validating models...");
        if (levelData.ProjectileModels == null || levelData.ProjectileModels.Count == 0)
        {
            GD.PrintErr($"[LevelLoader] Level '{levelId}' has no projectile models.");
            return false;
        }
        // Set background image
        GD.Print("Setting background...");
        // Validating background 
        Texture2D bgTexture = RenderingUtils.LoadTexture(CurrentLevelPath + "images/", levelData.BgImage);
        var svp = GetNode<SubViewport>(SubViewportPath);
        var backgroundImage = svp.GetNode<TextureRect>("BackgroundImage");
        backgroundImage.Texture = bgTexture;

        // Sort refs ascending by spawn time
        GD.Print("Sorting references...");
        var merged = (levelData.Projectiles ?? Enumerable.Empty<ProjectileReference>())
            .Cast<ISpatialReference>()
            .Concat((levelData.Patterns ?? Enumerable.Empty<PatternReference>()).Cast<ISpatialReference>())
            .ToList();
        _queue = merged;
        _queue.Sort((a, b) => a.T.CompareTo(b.T));

        _level = levelData;
        _elapsed = 0;
        _running = true;
        _gameRoot = new Node2D() { Name = "GameRoot" };
        svp.AddChild(_gameRoot);

        // Set aspect ratio
        var field = GetNode<PlayingField>("/root/main/PlayingField");
        field.SetRatio(levelData.AspectRatio, _gameRoot);

        // Setup UI
        GD.Print("Setting up UI...");
        _ui.HUD.SetHealth(levelData.Health);
        _ui.HUD.SetCompletion(0);
        _ui.HUD.SetLevelName(levelId);
        _ui.HUD.SetDuration(levelData.Duration);

        // start
        SetProcess(true);
        SpawnPlayerCharacter();
        EmitSignal(SignalName.LevelStarted, levelId);
        GD.Print($"[LevelLoader] Started level '{levelId}' - {_queue.Count} projectiles queued");
        return true;
    }
    public Projectile InstantiateProjectile(string modelId)
    {
        ProjectileModel model = _level.GetProjectileModel(modelId);
        Expr motionFnX = null;
        Expr motionFnY = null;
        if (!string.IsNullOrWhiteSpace(model.FunctionX) && model.FunctionX != "0")
            try { motionFnX = ExpressionParser.Parse(model.FunctionX); }
            catch (Exception ex) { GD.PrintErr($"[ProjectileParser] fnX parse error: {ex.Message}"); }
        if (!string.IsNullOrWhiteSpace(model.FunctionY) && model.FunctionY != "0")
            try { motionFnY = ExpressionParser.Parse(model.FunctionY); }
            catch (Exception ex) { GD.PrintErr($"[ProjectileParser] fnY parse error: {ex.Message}"); }
        var projectile = new Projectile
        {
            MotionFnX = motionFnX,
            MotionFnY = motionFnY,
            Name = model.Id,
            Lifetime = (float)model.Lifetime,
            Persistant = model.Persistant,
            Texture = RenderingUtils.LoadTexture(CurrentLevelPath+"images/", model.Texture),
            ColRadius = model.Radius,
            UseShape = model.UseShape,
            ColShape = model.Shape
        };
        var col = new CollisionShape2D { Name = "CollisionShape2D", Shape = BuildShape(model) };
        projectile.AddChild(col);
        col.Owner = projectile;
        return projectile;
    }
    public Pattern InstantiatePattern(string modelId)
    {
        PatternModel model = _level.GetPatternModel(modelId);
        Expr fnX = null;
        Expr fnY = null;
        Expr fnT = null;
        Expr fnFwd = null;
        if (!string.IsNullOrWhiteSpace(model.FunctionX) && model.FunctionX != "0")
            try { fnX = ExpressionParser.Parse(model.FunctionX); }
            catch (Exception ex) { GD.PrintErr($"[PatternParser] fnX parse error: {ex.Message}"); }
        if (!string.IsNullOrWhiteSpace(model.FunctionY) && model.FunctionY != "0")
            try { fnY = ExpressionParser.Parse(model.FunctionY); }
            catch (Exception ex) { GD.PrintErr($"[PatternParser] fnY parse error: {ex.Message}"); }
        if (!string.IsNullOrWhiteSpace(model.FunctionT) && model.FunctionT != "0")
            try { fnT = ExpressionParser.Parse(model.FunctionT); }
            catch (Exception ex) { GD.PrintErr($"[PatternParser] fnT parse error: {ex.Message}"); }
        if (!string.IsNullOrWhiteSpace(model.FunctionFwd) && model.FunctionFwd != "0")
            try { fnFwd = ExpressionParser.Parse(model.FunctionFwd); }
            catch (Exception ex) { GD.PrintErr($"[PatternParser] fnFwd parse error: {ex.Message}"); }
        var pattern = new Pattern
        {
            FnX = fnX,
            FnY = fnY,
            FnT = fnT,
            FnFwd = fnFwd,
            ProjectileModelId = model.ProjectileId,
            Count = model.Count
        };
        return pattern;
    }
    public void SpawnProjectile(ProjectileReference pref)
    {
        var projectile = InstantiateProjectile(pref.Id);
        projectile.SpawnPosition = new Vector2(pref.X, pref.Y);
        projectile.Forward = pref.Forward;

        _gameRoot.AddChild(projectile);
    }
    public void SpawnPattern(PatternReference pref)
    {
        var pattern = InstantiatePattern(pref.Id);
        pattern.Position = new Vector2(pref.X, pref.Y);
        pattern.Forward = pref.Forward;

        _gameRoot.AddChild(pattern);
    }
    public static Shape2D BuildShape(ProjectileModel model)
    {
        if (model.UseShape && model.Shape is { Length: >= 3 })
        {
            var pts = new Vector2[model.Shape.Length];
            for (int i = 0; i < model.Shape.Length; i++)
                pts[i] = new Vector2(
                    model.Shape[i].Length > 0 ? model.Shape[i][0] : 0f,
                    model.Shape[i].Length > 1 ? model.Shape[i][1] : 0f);
            return new ConvexPolygonShape2D { Points = pts };
        }
        return new CircleShape2D { Radius = (float)model.Radius };
    }
    PackedScene playerCharacterScene = ResourceLoader.Load<PackedScene>("res://data/scenes/player_character.tscn");
    private void SpawnPlayerCharacter(string characterSprite = "default")
    {
        var playerCharacter = playerCharacterScene.Instantiate<PlayerCharacter>();
        playerCharacter.characterSprite = characterSprite;
        playerCharacter.SetHealth(_level.Health);
        playerCharacter.Position = new Vector2(PlayingField.Resolutions[_level.AspectRatio].X / 2, PlayingField.Resolutions[_level.AspectRatio].Y * 0.9f);
        _gameRoot.AddChild(playerCharacter);
    }
    private void FinishLevel()
    {
        _running = false;
        SetProcess(false);
        _gameRoot.QueueFree();
        GD.Print($"[LevelLoader] Level '{_level.LevelId}' finished.");
        EmitSignal(SignalName.LevelFinished, _level.LevelId);
    }
}