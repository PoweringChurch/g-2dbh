using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
public partial class Editor : CanvasLayer
{
    public static Editor Instance;
    public const int MaxModelCount = 128;
    public static bool CannotUseBinds()
    {
        if (GameSession.Instance.Running) return true;
        var focused = Instance.GetViewport().GuiGetFocusOwner();
        if (focused is LineEdit or TextEdit)
            return true;
        return false;
    }
    public EditorReference SelectedReference
    {
        get => preview.SelectedReference;
        set
        {
            preview.SelectedReference = value;
        }
    }
    public float CurrentTime => timeline.CurrentTime;
    public Toolbar.Mode CurrentMode => toolbar.CurrentMode;
    public bool Snap => toolbar.Snap;
    public bool TimeControls => toolbar.TimeControls;
    public float IncrementTimeBy => toolbar.IncrementTimeBy;
    public string LevelPath => $"{(levelData != null ? levelData.LevelPath : "")}";
    public IEditorModel SelectedModel => modelLibrary.SelectedModel;
    public IReadOnlyList<ProjectileModel> ProjectileModels => levelData.ProjectileModels;
    public IReadOnlyList<PatternModel> PatternModels => levelData.PatternModels;
    public List<BackgroundLayerInstance> BGInstances => preview.BGInstances;
    public void SaveProjectileModel(ProjectileModel model)
    {
        levelData.ProjectileModels[model.Id] = model;
        patternCreator.ProjectileModelUpdated(model);
        preview.CompileProjectile(model);
        preview.UpdateModel(model);
        modelLibrary.Refresh();
        projCreator.Load(levelData);
    }
    public void SavePatternModel(PatternModel model)
    {
        levelData.PatternModels[model.Id] = model;
        preview.CompilePattern(model);
        preview.UpdateModel(model);
        modelLibrary.Refresh();
    }
    public void DeleteProjectileModel(int id)
    {
        var popup = Popups.Instance.Show(Popups.DefaultType.YN, 
        $"Are you sure you want to delete projectile '{levelData.ProjectileModels[id].Name}' (Id {id})? All projectiles and patterns with the associated id will be removed.");
        popup.Options[0].Pressed += () =>
        {
            preview.UpdateModel(levelData.ProjectileModels[id], true);
            levelData.ProjectileModels[id] = null;
            for (int i = levelData.References.Count - 1; i >= 0; i--)
            {
                var r = levelData.References[i];
                if (r.Type == ModelType.Projectile && r.Id == id)
                {
                    DeleteReference(r);
                }
                else if (r.Type == ModelType.Pattern && levelData.PatternModels[id].ProjectileId == id)
                {
                    DeleteReference(r);
                }
            }
            preview.Sync();
        };
    }
    public void RemovePatternModel(int id, Control ui)
    {
        var popup = Popups.Instance.Show(Popups.DefaultType.YN, 
        $"Are you sure you want to delete pattern '{levelData.PatternModels[id].Name}' (Id {id})? All patterns with the associated id will be removed.");
        popup.Options[0].Pressed += () =>
        {
            preview.UpdateModel(levelData.PatternModels[id], true);
            levelData.PatternModels[id] = null;
            for (int i = levelData.References.Count - 1; i >= 0; i--)
                if (levelData.References[i].Id == id)
                    DeleteReference(levelData.References[i]);
            ui.QueueFree();
            preview.Sync();
        };
    }
    public LevelData levelData;
    // references
    // modules
    [Export] private Toolbar toolbar;
    [Export] private Timeline timeline;
    [Export] private ModelLibrary modelLibrary;
    [Export] private LevelMetadata levelMeta;
    [Export] private LevelPreview preview;
    [Export] private ProjectileCreator projCreator;
    [Export] private PatternCreator patternCreator;
    [Export] private Inspector inspector;
    [Export] private CustomVariables customVars;
    GameSession gs => GameSession.Instance;
    public override void _EnterTree() =>
        Instance = this;

    public override void _Ready()
    {
        // events
        levelMeta.AspectRatioChanged += preview.Fit;
        levelMeta.DurationChanged += timeline.UpdateDuration;
        levelMeta.SaveLevelRequested += SaveLevel;
        levelMeta.MusicChanged += timeline.UpdateMusic;

        GetWindow().FocusEntered += RenderingUtils.EmptyTextureCache;
    }
    public void UpdateInspector() =>
        inspector.Update(SelectedReference);
    public void RefreshTimelineMarker(EditorReference r) =>
        timeline.RefreshMarker(r);
    [Signal] public delegate void CopyEventHandler();
    [Signal] public delegate void PasteEventHandler();
    [Signal] public delegate void DeleteEventHandler();
    [Signal] public delegate void CutEventHandler();
    public override void _Input(InputEvent @event)
    {
        if (CannotUseBinds()) return;
        if (@event.IsActionPressed("save_bind")) SaveLevel();

        if (@event.IsActionPressed("cut")) EmitSignal(SignalName.Cut);
		if (@event.IsActionPressed("copy")) EmitSignal(SignalName.Copy);
		if (@event.IsActionPressed("paste")) EmitSignal(SignalName.Paste);
		if (@event.IsActionPressed("delete")) EmitSignal(SignalName.Delete);
    }
    public void NewLevel()
    {
        LevelData data = new();
        data.LevelPath = $"user://data/levels/{Guid.NewGuid()}/";
        DirAccess.MakeDirRecursiveAbsolute(data.LevelPath);
        DirAccess.MakeDirRecursiveAbsolute(data.LevelPath + "audio/");
        WriteJson(data.LevelPath + "leveldata.json", data);
        ApplyLevelData(data);
    }
    public void SetPlaying(bool to) => timeline.SetPlaying(to);
    public bool OpenLevel(LevelData data)
    {
        ApplyLevelData(data);
        return true;
    }
    private void ApplyLevelData(LevelData data)
    {
        PlaylistHandler.Instance.FadeOut();
        RepairLevelData(data);
        levelData = data;
        customVars.Load(data);
        timeline.Load(data);
        timeline.UpdateDuration();
        preview.Fit(PlayingField.Resolutions[data.AspectRatio]);
        levelMeta.Load(data);
        projCreator.Load(data);
        projCreator.LoadProjectile(levelData.ProjectileModels[0]);
        patternCreator.Load(data);
        patternCreator.LoadPattern(levelData.PatternModels[0]);
        modelLibrary.Refresh();
        preview.Load(data);
        preview.Sync();
    }
    private static void RepairLevelData(LevelData data)
    {
        data.BackgroundLayers ??= [];
        data.ProjectileModels ??= [];  
        data.PatternModels ??= [];  
        data.References ??= [];  
        data.CustomVariables ??= [];  
    }
    private void SaveLevel()
    {
        Console.Inst.Log($"[Editor] Saving level {levelData.DisplayName} ({levelData.LevelPath})...");
        WriteJson(levelData.LevelPath + "leveldata.json", levelData);
        Console.Inst.Log("[Editor] Saved level successfully");
    }
    public void SyncPreview() =>
        preview.Sync();
    public void AddReference(EditorReference reference)
    {
        timeline.AddMarker(reference);
        levelData.References.Add(reference);
    }
    public void DeleteReference(EditorReference reference)
    {
        timeline.RemoveMarker(reference);
        levelData.References.Remove(reference);
        preview.UpdateReferenceInEditor(null, reference.RootEditorId);
    }
    // opens a model in its respective creator
    public void OpenModel(IEditorModel model)
    {
        if (model is ProjectileModel pm)
            projCreator.LoadProjectile(pm);
        else if (model is PatternModel ptm)
            patternCreator.LoadPattern(ptm);
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