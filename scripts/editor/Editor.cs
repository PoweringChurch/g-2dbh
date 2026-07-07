using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
public partial class Editor : CanvasLayer
{
    public static bool Open = false;
    public const int MaxModelCount = 128;
    public enum Mode { Place, Select, Delete }
    private Mode currentMode = Mode.Select;
    public Mode CurrentMode
    {
        get => currentMode;
        set
        {
            if (value == Mode.Place)
                _modeDisplay.Text = "Mode : Place";
            else if (value == Mode.Select)
                _modeDisplay.Text = "Mode : Select";
            else if (value == Mode.Delete)
                _modeDisplay.Text = "Mode : Delete";
            currentMode = value;
        }
    }
    private bool snap;
    public bool Snap 
    {
        get => snap; 
        set
        {
            snap = value;
            _snapDisplay.Text = value ? "Snap : On" : "Snap : Off";
        }
    }
    public EditorReference SelectedReference
    {
        get => _preview.SelectedReference;
        set
        {
            _preview.SelectedReference = value;
        }
    }
    public float CurrentTime => _timeline.CurrentTime;
    private const string _levelDirectory = "user://data/levels/";
    public string LevelPath => $"{_levelDirectory}{(levelData != null ? levelData.LevelId : "")}/";
    public IEditorModel SelectedModel => _modelLibrary.SelectedModel;
    private ProjectileModel[] projectileModels = new ProjectileModel[MaxModelCount];
    private PatternModel[] patternModels = new PatternModel[MaxModelCount];
    public IReadOnlyList<ProjectileModel> ProjectileModels => projectileModels;
    public IReadOnlyList<PatternModel> PatternModels => patternModels;
    // Save the input model at the specified id. This function will set the models id to match what was provided.
    public void SaveProjectileModel(ProjectileModel model, int id)
    {
        projectileModels[id] = model;
        model.Id = id;
    }
    // Save the input model at the specified id. This function will set the models id to match what was provided.
    public void SavePatternModel(PatternModel model, int id)
    {
        patternModels[id] = model;
        model.Id = id;
    }
    public void RemoveProjectileModel(int id, Control ui)
    {
        var popup = Popups.Instance.Show(Popups.DefaultType.YN, 
        $"Are you sure you want to delete projectile '{projectileModels[id].Name}' (Id {id})? All projectiles and patterns with the associated id will be removed.");
        popup.Options[0].Pressed += () =>
        {
            projectileModels[id] = null;
            for (int i = levelData.References.Count - 1; i >= 0; i--)
            {
                var r = levelData.References[i];
                if (r.Type == ModelType.Projectile && r.Id == id)
                {
                    levelData.References.RemoveAt(i);
                }
                else if (r.Type == ModelType.Pattern && levelData.PatternModels[id].ProjectileId == id)
                {
                    levelData.References.RemoveAt(i);
                }
            }
            ui.QueueFree();
        };
        _preview.Sync();
    }
    public void RemovePatternModel(int id, Control ui)
    {
        var popup = Popups.Instance.Show(Popups.DefaultType.YN, 
        $"Are you sure you want to delete pattern '{patternModels[id].Name}' (Id {id})? All patterns with the associated id will be removed.");
        popup.Options[0].Pressed += () =>
        {
            patternModels[id] = null;
            for (int i = levelData.References.Count - 1; i >= 0; i--)
                if (levelData.References[i].Id == id)
                    levelData.References.RemoveAt(i);
            ui.QueueFree();
        };
        _preview.Sync();
    }
    public LevelData levelData;
    // paths
    const string Modules = "/root/main/EditorLayer/Sections/Modules";
    public NodePath TimelinePath = Modules + "/Middle/Timeline";
    public NodePath ModelLibraryPath = Modules + "/Middle/ModelLibrary";
    public NodePath LevelMetaPath = Modules + "/Left/LevelMetadata";
    public NodePath LevelPreviewPath = Modules + "/Left/LevelPreview";
    public NodePath ProjCreatorPath = Modules + "/Right/Creators/ProjectileCreator";
    public NodePath PatternCreatorPath = Modules + "/Right/Creators/PatternCreator";
    public NodePath InspectorPath = Modules + "/Middle/Inspector";

    const string ToolbarButtons = "/root/main/EditorLayer/Sections/Toolbar/Buttons";
    public NodePath PlaceButtonPath = ToolbarButtons + "/ModeSelection/Place";
    public NodePath SelectButtonPath = ToolbarButtons + "/ModeSelection/Select";
    public NodePath DeleteButtonPath = ToolbarButtons + "/ModeSelection/Delete";
    public NodePath ModeDisplayPath = ToolbarButtons + "/ModeSelection/ModeDisplay";
    public NodePath SnapAngleTogglePath = ToolbarButtons + "/ModeSelection/SnapAngle";
    public NodePath SnapAnglePath = ToolbarButtons + "/ModeSelection/SnapDisplay";
    // references
    private Timeline _timeline;
    private ModelLibrary _modelLibrary;
    private LevelMetadata _levelMeta;
    private LevelPreview _preview;
    private ProjectileCreator _projCreator;
    private PatternCreator _patternCreator;
    private Inspector _inspector;
    private Button _placeButton;
    private Button _selectButton;
    private Button _deleteButton;
    private Label _modeDisplay;
    private Button _snapButton;
    private Label _snapDisplay;
    public override void _Ready()
    {
        // modules
        _timeline = GetNode<Timeline>(TimelinePath);
        _modelLibrary = GetNode<ModelLibrary>(ModelLibraryPath);
        _levelMeta = GetNode<LevelMetadata>(LevelMetaPath);
        _preview = GetNode<LevelPreview>(LevelPreviewPath);
        _projCreator = GetNode<ProjectileCreator>(ProjCreatorPath);
        _patternCreator = GetNode<PatternCreator>(PatternCreatorPath);
        _inspector = GetNode<Inspector>(InspectorPath);
        // events
        _levelMeta.AspectRatioChanged += _preview.Fit;
        _levelMeta.DurationChanged += _timeline.UpdateDuration;
        _levelMeta.SaveLevelRequested += SaveLevel;
        _levelMeta.BgImageChanged += _preview.ChangeBackgroundImage;
        _levelMeta.MusicChanged += _timeline.UpdateMusic;
        _projCreator.ModelSaved += _modelLibrary.OnModelSaved;
        _projCreator.ModelSaved += _preview.CompileProjectile;
        _projCreator.ModelSaved += _preview.Sync;
        _projCreator.ModelSaved += _patternCreator.OnModelUpdate;

        _patternCreator.ModelSaved += _modelLibrary.OnModelSaved;
        _patternCreator.ModelSaved += _preview.CompilePattern;
        _patternCreator.ModelSaved += _preview.Sync;

        GetWindow().FocusEntered += RenderingUtils.EmptyTextureCache;
        // toolbar
        _placeButton = GetNode<Button>(PlaceButtonPath);
        _selectButton = GetNode<Button>(SelectButtonPath);
        _deleteButton = GetNode<Button>(DeleteButtonPath);
        _modeDisplay = GetNode<Label>(ModeDisplayPath);
        _snapButton = GetNode<Button>(SnapAngleTogglePath);
        _snapDisplay = GetNode<Label>(SnapAnglePath);
        // toolbar events
        _placeButton.Pressed += () => CurrentMode = Mode.Place;
        _selectButton.Pressed += () => CurrentMode = Mode.Select;
        _deleteButton.Pressed += () => CurrentMode = Mode.Delete;
        _snapButton.Pressed += () => Snap = !Snap;
    }
    public void UpdateInspector() =>
        _inspector.Update(SelectedReference);
    public void RefreshTimelineMarker(EditorReference r) =>
        _timeline.RefreshMarker(r);
    public override void _Input(InputEvent @event)
    {
        if (!Open) return;
        if (@event.IsActionPressed("place_bind"))
        {
            CurrentMode = Mode.Place;
        }
        else if (@event.IsActionPressed("select_bind"))
        {
            CurrentMode = Mode.Select;
        }
        else if (@event.IsActionPressed("delete_bind"))
        {
            CurrentMode = Mode.Delete;
        }
        else if (@event.IsActionPressed("save_bind"))
        {
            SaveLevel();
        }
        else if (@event.IsActionPressed("playback_toggle"))
        {
            _timeline.TogglePlaying();
        }
        else if (@event.IsActionPressed("snap"))
		{
			Snap = !Snap;
		}
    }
    public void NewLevel()
    {
        LevelData data = new()
        {
            DisplayName = "New Level",
            Author = "Unknown",
            BgImage = "none",
            Music = "none",
            Health = 3,
            AspectRatio = 0,
            Duration = 1f,
            LevelId = Guid.NewGuid().ToString(),
            ProjectileModels = new ProjectileModel[MaxModelCount],
            PatternModels =  new PatternModel[MaxModelCount],
            References = [],
        };
        string levelPath = $"{_levelDirectory}{data.LevelId}/";
        DirAccess.MakeDirRecursiveAbsolute(levelPath);
        DirAccess.MakeDirRecursiveAbsolute(levelPath + "images/");
        DirAccess.MakeDirRecursiveAbsolute(levelPath + "audio/");
        WriteJson(levelPath + "leveldata.json", data);
        ApplyLevelData(data);
    }
    public void SetPlaying(bool to) => _timeline.SetPlaying(to);
    public bool OpenLevel(string levelId)
    {
        string levelDataPath = $"{_levelDirectory}{levelId}/leveldata.json";

        if (!FileAccess.FileExists(levelDataPath))
        {
            Console.Inst.Log($"[Editor] Level not found: {levelDataPath}");
            return false;
        }
        LevelData data = ReadJson<LevelData>(levelDataPath);
        data.LevelId = levelId;
        ApplyLevelData(data);
        return true;
    }
    private void ApplyLevelData(LevelData data)
    {
        levelData = data;
        for (int i = 0; i < MaxModelCount; i++)
        {
            var m = data.ProjectileModels[i];
            projectileModels[i] = m;
        }
        for (int i = 0; i < MaxModelCount; i++)
        {
            var m = data.PatternModels[i];
            patternModels[i] = m;
        }
        _timeline.Load(data);
        _timeline.UpdateDuration();
        _preview.CompileAll();
        _preview.ChangeBackgroundImage(data.BgImage);
        _preview.Fit(PlayingField.Resolutions[data.AspectRatio]);
        _levelMeta.Load(data);
        _projCreator.LoadProjectile(projectileModels[0]);
        _patternCreator.LoadPattern(patternModels[0]);
        _modelLibrary.Refresh();
        _preview.Sync();
    }
    private void SaveLevel()
    {
        levelData.PatternModels = [.. patternModels];
        levelData.ProjectileModels = [.. projectileModels];
        string levelPath = $"{_levelDirectory}{levelData.LevelId}/";
        Console.Inst.Log($"[Editor] Saving level {levelData.DisplayName} ({levelData.LevelId})...");
        WriteJson(levelPath + "leveldata.json", levelData);
        Console.Inst.Log("[Editor] Saved level successfully");
    }
    public void SyncPreview() =>
        _preview.Sync();
    public void AddReference(EditorReference bullet)
    {
        _timeline.AddMarker(bullet);
        levelData.References.Add(bullet);
        _preview.Sync();
    }
    public void DeleteReference(EditorReference bullet)
    {
        _timeline.RemoveMarker(bullet);
        levelData.References.Remove(bullet);
        _preview.Sync();
    }
    // opens a model in its respective creator
    public void OpenModel(IEditorModel model)
    {
        if (model is ProjectileModel pm)
            _projCreator.LoadProjectile(pm);
        else if (model is PatternModel ptm)
            _patternCreator.LoadPattern(ptm);
    }
    private static T ReadJson<T>(string path)
    {
        if (!FileAccess.FileExists(path)) return default;
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null) return default;
        try { return JsonSerializer.Deserialize<T>(file.GetAsText()); }
        catch { return default; }
    }
    private static void WriteJson<T>(string path, T data)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            Console.Inst.LogErr($"Failed to open file for writing: {path}");
        }
        var s = JsonSerializer.Serialize(data);
        file.StoreString(s);
    }
}
