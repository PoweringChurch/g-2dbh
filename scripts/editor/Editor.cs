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
    private float incrementTimeBy = 1;
    public float IncrementTimeBy
    {
        get => incrementTimeBy;
        set
        {
            incrementTimeBy = Math.Clamp(value, 0.125f, 128);
            _skipInput.SetValueNoSignal(value);
            Console.Inst.Log($"Increment time by : {IncrementTimeBy}");
        }
    }
    private bool timeControls;
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
    public List<BackgroundLayerInstance> BGInstances => _preview.BGInstances;
    // Save the input model at the specified id. This function will set the models id to match what was provided
    public void SaveProjectileModel(ProjectileModel model, int id)
    {
        projectileModels[id] = model;
        model.Id = id;
    }
    // Save the input model at the specified id. This function will set the models id to match what was provided
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
            _preview.UpdateModel(projectileModels[id], true);
            projectileModels[id] = null;
            for (int i = levelData.References.Count - 1; i >= 0; i--)
            {
                var r = levelData.References[i];
                if (r.Type == ModelType.Projectile && r.Id == id)
                {
                    DeleteReference(r);
                }
                else if (r.Type == ModelType.Pattern && patternModels[id].ProjectileId == id)
                {
                    DeleteReference(r);
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
            _preview.UpdateModel(patternModels[id], true);
            patternModels[id] = null;
            for (int i = levelData.References.Count - 1; i >= 0; i--)
                if (levelData.References[i].Id == id)
                    DeleteReference(levelData.References[i]);
            ui.QueueFree();
            _preview.Sync();
        };
    }
    public LevelData levelData;
    // paths
    const string Modules = "/root/main/EditorLayer/Sections/Modules";
    const string TimelinePath = "/root/main/EditorLayer/Sections/Timeline";
    const string ModelLibraryPath = Modules + "/Middle/ModelLibrary";
    const string LevelMetaPath = Modules + "/Left/LevelMetadata";
    const string LevelPreviewPath = Modules + "/Left/LevelPreview";
    const string ProjCreatorPath = Modules + "/Right/Creators/ProjectileCreator";
    const string PatternCreatorPath = Modules + "/Right/Creators/PatternCreator";
    const string InspectorPath = Modules + "/Middle/Inspector";
    const string CustomVariablesPath = Modules + "/Middle/Expressions/Custom";

    const string ToolbarButtons = "/root/main/EditorLayer/Sections/Toolbar/Buttons";
    // mode
    const string PlaceButtonPath = ToolbarButtons + "/ModeSelection/Place";
    const string SelectButtonPath = ToolbarButtons + "/ModeSelection/Select";
    const string DeleteButtonPath = ToolbarButtons + "/ModeSelection/Delete";
    const string ModeDisplayPath = ToolbarButtons + "/ModeSelection/ModeDisplay";
    // snap
    const string SnapAngleTogglePath = ToolbarButtons + "/SnapDisplay/SnapAngle";
    const string SnapAnglePath = ToolbarButtons + "/SnapDisplay/SnapDisplay";
    // time controls
    const string SkipInputPath = ToolbarButtons + "/TimeControl/SkipInput";
    // references
    // modules
    private Timeline _timeline;
    private ModelLibrary _modelLibrary;
    private LevelMetadata _levelMeta;
    private LevelPreview _preview;
    private ProjectileCreator _projCreator;
    private PatternCreator _patternCreator;
    private Inspector _inspector;
    private CustomVariables _customVars;
    // toolbar
    // mode
    private Button _placeButton;
    private Button _selectButton;
    private Button _deleteButton;
    private Label _modeDisplay;
    // snap
    private Button _snapButton;
    private Label _snapDisplay;
    // time controls
    private SpinBox _skipInput;
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
        _customVars = GetNode<CustomVariables>(CustomVariablesPath);
        // events
        _levelMeta.AspectRatioChanged += _preview.Fit;
        _levelMeta.DurationChanged += _timeline.UpdateDuration;
        _levelMeta.SaveLevelRequested += SaveLevel;
        _levelMeta.MusicChanged += _timeline.UpdateMusic;
        _projCreator.ModelSaved += _modelLibrary.OnModelSaved;
        _projCreator.ModelSaved += _patternCreator.OnModelUpdate;
        _projCreator.ModelSaved += _preview.CompileProjectile;
        _projCreator.ModelSaved += (model) => _preview.UpdateModel(model, false);

        _patternCreator.ModelSaved += _modelLibrary.OnModelSaved;
        _patternCreator.ModelSaved += _preview.CompilePattern;
        _patternCreator.ModelSaved += (model) => _preview.UpdateModel(model, false);

        GetWindow().FocusEntered += RenderingUtils.EmptyTextureCache;
        // toolbar
        // mode
        _placeButton = GetNode<Button>(PlaceButtonPath);
        _selectButton = GetNode<Button>(SelectButtonPath);
        _deleteButton = GetNode<Button>(DeleteButtonPath);
        _modeDisplay = GetNode<Label>(ModeDisplayPath);
        // snap
        _snapButton = GetNode<Button>(SnapAngleTogglePath);
        _snapDisplay = GetNode<Label>(SnapAnglePath);
        // time controls
        _skipInput = GetNode<SpinBox>(SkipInputPath);

        // toolbar events
        _placeButton.Pressed += () => CurrentMode = Mode.Place;
        _selectButton.Pressed += () => CurrentMode = Mode.Select;
        _deleteButton.Pressed += () => CurrentMode = Mode.Delete;
        _snapButton.Pressed += () => Snap = !Snap;
        
        _skipInput.ValueChanged += v => IncrementTimeBy = (float)v;
    }
    public void UpdateInspector() =>
        _inspector.Update(SelectedReference);
    public void RefreshTimelineMarker(EditorReference r) =>
        _timeline.RefreshMarker(r);
    [Signal] public delegate void CopyEventHandler();
    [Signal] public delegate void PasteEventHandler();
    [Signal] public delegate void DeleteEventHandler();
    [Signal] public delegate void CutEventHandler();

    public override void _Input(InputEvent @event)
    {
        if (!Open) return;
        var focused = GetViewport().GuiGetFocusOwner();
        if (focused is LineEdit or TextEdit)
            return;
        CurrentMode = @event.IsActionPressed("place_bind") ? Mode.Place :
                  @event.IsActionPressed("select_bind") ? Mode.Select :
                  @event.IsActionPressed("delete_bind") ? Mode.Delete : CurrentMode;
        if (@event.IsActionPressed("save_bind")) SaveLevel();
        if (@event.IsActionPressed("playback_toggle")) _timeline.TogglePlaying();
        
        if (@event.IsActionPressed("skip_forward"))
        {
            Console.Inst.Log($"Skipped time to {CurrentTime + IncrementTimeBy}");
            _timeline.SetTime(timeControls ? GetNextReferenceTime() : CurrentTime + IncrementTimeBy);
        }
        else if (@event.IsActionPressed("skip_backward"))
        {
            Console.Inst.Log($"Skipped time to {CurrentTime - IncrementTimeBy}");
            _timeline.SetTime(timeControls ? GetPrevReferenceTime() : CurrentTime - IncrementTimeBy);
        }
        if (timeControls)
        {
            if (@event.IsActionPressed("scroll_up")) IncrementTimeBy *= 2;
            if (@event.IsActionPressed("scroll_down")) IncrementTimeBy /= 2;
        }
        if (@event.IsActionPressed("snap")) Snap = !Snap;

        if      (@event.IsActionPressed("time_control")) timeControls = true;
        else if (@event.IsActionReleased("time_control")) timeControls = false;

        if (@event.IsActionPressed("cut")) EmitSignal(SignalName.Cut);
		if (@event.IsActionPressed("copy")) EmitSignal(SignalName.Copy);
		if (@event.IsActionPressed("paste")) EmitSignal(SignalName.Paste);
		if (@event.IsActionPressed("delete")) EmitSignal(SignalName.Delete);

    }
    private float GetNextReferenceTime()
    {
        float closestTime = levelData.Duration;
        for (int i = 0; i < levelData.References.Count; i++)
        {
            var r = levelData.References[i];
            if (r.T > CurrentTime && r.T < closestTime) closestTime = (float)r.T;
        }
        return closestTime;
    }
    private float GetPrevReferenceTime()
    {
        float closestTime = 0;
        for (int i = 0; i < levelData.References.Count; i++)
        {
            var r = levelData.References[i];
            if (r.T < CurrentTime && r.T > closestTime) closestTime = (float)r.T;
        }
        return closestTime;
    }
    public void NewLevel()
    {
        LevelData data = new();
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
        RepairLevelData(data);
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
        _customVars.Load(data);
        _timeline.Load(data);
        _timeline.UpdateDuration();
        _preview.Fit(PlayingField.Resolutions[data.AspectRatio]);
        _levelMeta.Load(data);
        _projCreator.LoadProjectile(projectileModels[0]);
        _patternCreator.LoadPattern(patternModels[0]);
        _modelLibrary.Refresh();
        _preview.Load(data);
        _preview.Sync();
    }
    private void RepairLevelData(LevelData data)
    {
        data.BackgroundLayers ??= [];
        data.ProjectileModels ??= [];  
        data.PatternModels ??= [];  
        data.References ??= [];  
        data.CustomVariables ??= [];  
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
    public void AddReference(EditorReference reference)
    {
        _timeline.AddMarker(reference);
        levelData.References.Add(reference);
    }
    public void DeleteReference(EditorReference reference)
    {
        _timeline.RemoveMarker(reference);
        levelData.References.Remove(reference);
        _preview.UpdateReferenceInEditor(null, reference.RootEditorId);
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