using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
public partial class Editor : CanvasLayer
{
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
    public float CurrentTime => _timeline.CurrentTime;
    private const string _levelDirectory = "user://data/levels/";
    public string LevelPath => $"{_levelDirectory}{(levelData != null ? levelData.LevelId : "")}/";
    public IEditorModel SelectedModel => _modelLibrary.SelectedModel;
    private ProjectileModel[] projectileModels = new ProjectileModel[128];
    private PatternModel[] patternModels = new PatternModel[128];
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
    public void RemoveProjectileModel(int id)
    {
        projectileModels[id] = null;
    }
    public void RemovePatternModel(int id)
    {
        patternModels[id] = null;
    }
    public LevelData levelData;
    // paths
    const string Modules = "/root/main/EditorLayer/Sections/Modules";
    public NodePath TimelinePath = Modules + "/Middle/Timeline";
    public NodePath ModelLibraryPath = Modules + "/Middle/ModelLibrary";
    public NodePath LevelMetaPath = Modules + "/Left/LevelMetadata";
    public NodePath LevelPreviewPath = Modules + "/Left/LevelPreview";
    public NodePath ProjCreatorPath = Modules + "/Right/ProjectileCreator";
    public NodePath PatternCreatorPath = Modules + "/Right/PatternCreator";
    public NodePath InspectorPath = Modules + "/Middle/Inspector";

    const string ToolbarButtons = "/root/main/EditorLayer/Sections/Toolbar/Buttons";
    public NodePath PlaceButtonPath = ToolbarButtons + "/ModeSelection/Place";
    public NodePath SelectButtonPath = ToolbarButtons + "/ModeSelection/Select";
    public NodePath DeleteButtonPath = ToolbarButtons + "/ModeSelection/Delete";
    public NodePath ModeDisplayPath = ToolbarButtons + "/ModeSelection/ModeDisplay";
    // references
    private Timeline _timeline;
    private ModelLibrary _modelLibrary;
    private LevelMetadata _levelMeta;
    private LevelPreview _preview;
    private ProjectileCreator _projCreator;
    private PatternCreator _patternCreator;
    private Button _placeButton;
    private Button _selectButton;
    private Button _deleteButton;
    private Label _modeDisplay;
    public override void _Ready()
    {
        // modules
        _timeline = GetNode<Timeline>(TimelinePath);
        _modelLibrary = GetNode<ModelLibrary>(ModelLibraryPath);
        _levelMeta = GetNode<LevelMetadata>(LevelMetaPath);
        _preview = GetNode<LevelPreview>(LevelPreviewPath);
        _projCreator = GetNode<ProjectileCreator>(ProjCreatorPath);
        _patternCreator = GetNode<PatternCreator>(PatternCreatorPath);

        // events
        _levelMeta.AspectRatioChanged += _preview.Fit;
        _levelMeta.DurationChanged += _timeline.UpdateDuration;
        _levelMeta.SaveLevelRequested += SaveLevel;
        _levelMeta.BgImageChanged += _preview.ChangeBackgroundImage;

        _projCreator.ModelSaved += _modelLibrary.OnModelSaved;
        _projCreator.ModelSaved += _preview.Sync;
        _projCreator.ModelSaved += _patternCreator.OnModelUpdate;

        _patternCreator.ModelSaved += _modelLibrary.OnModelSaved;
        _patternCreator.ModelSaved += _preview.Sync;

        GetWindow().FocusEntered += RenderingUtils.EmptyTextureCache;
        // toolbar
        _placeButton = GetNode<Button>(PlaceButtonPath);
        _selectButton = GetNode<Button>(SelectButtonPath);
        _deleteButton = GetNode<Button>(DeleteButtonPath);
        _modeDisplay = GetNode<Label>(ModeDisplayPath);

        _placeButton.Pressed += () => CurrentMode = Mode.Place;
        _selectButton.Pressed += () => CurrentMode = Mode.Select;
        _deleteButton.Pressed += () => CurrentMode = Mode.Delete;
    }
    public void NewLevel()
    {
        LevelData data = new()
        {
            DisplayName = "New Level",
            Author = "Unknown",
            BgImage = "none",
            Health = 3,
            AspectRatio = 0,
            Duration = 1f,
            LevelId = Guid.NewGuid().ToString(),
            ProjectileModels = [],
            PatternModels = [],
            References = [],
        };

        string levelPath = $"{_levelDirectory}{data.LevelId}/";
        DirAccess.MakeDirRecursiveAbsolute(levelPath);
        DirAccess.MakeDirRecursiveAbsolute(levelPath + "images/");
        WriteJson(levelPath + "leveldata.json", data);
        ApplyLevelData(data);
    }
    public bool OpenLevel(string levelId)
    {
        string levelDataPath = $"{_levelDirectory}{levelId}/leveldata.json";

        if (!FileAccess.FileExists(levelDataPath))
        {
            GD.PrintErr($"[Editor] Level not found: {levelDataPath}");
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
        foreach (var model in data.ProjectileModels)
        {
            projectileModels[model.Id] = model;
        }
        foreach (var model in data.PatternModels)
        {
            patternModels[model.Id] = model;
        }
        _timeline.Load(data.References);
        _timeline.UpdateDuration();
        _levelMeta.Load(data);
        _preview.ChangeBackgroundImage(data.BgImage);
        _projCreator.LoadProjectile(new());
        _patternCreator.LoadPattern(new());
        var merged = (data.ProjectileModels ?? Enumerable.Empty<ProjectileModel>())
            .Cast<IEditorModel>()
            .Concat((data.PatternModels ?? Enumerable.Empty<PatternModel>()).Cast<IEditorModel>())
            .ToList();
        _modelLibrary.Load(merged);
    }
    private void SaveLevel()
    {
        string levelPath = $"{_levelDirectory}{levelData.LevelId}/";
        GD.Print($"[Editor] Saving level {levelData.DisplayName} ({levelData.LevelId})...");
        WriteJson(levelPath + "leveldata.json", levelData);
        GD.Print("[Editor] Saved level successfully");
    }
    public void AddReference(EditorReference bullet)
    {
        _timeline.AddMarker(bullet);
        levelData.References.Add(bullet);
        _preview.Sync();
    }
    public void DeleteBullet(EditorReference bullet)
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
            GD.PrintErr($"[Editor] Failed to open file for writing: {path}");
        }
        var s = JsonSerializer.Serialize(data);
        file.StoreString(s);
    }
}
