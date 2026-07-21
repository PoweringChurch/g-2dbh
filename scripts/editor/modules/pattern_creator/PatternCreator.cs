using Godot;

public partial class PatternCreator : Control
{
    public static PatternCreator Instance;
    [Export] public PatternPreview PatternPreview;
    public PatternModel Model {get; private set;}
    [Export] PatternInfo Info;
    [Export] PatternSpawnConditions Spawning;
    [Export] PackedScene PatternModelUi;
    [Export] VBoxContainer ModelList;
    [Export] Button NextFree;
    public override void _EnterTree() => 
        Instance = this;
    public override void _Ready()
    {
        NextFree.Pressed += GoNextFree;
    }
    private void GoNextFree()
    {
        for (int i = 0; i < Editor.MaxModelCount; i++)
        {
            if (Editor.Instance.PatternModels[i] == null)
            {
                Editor.Instance.OpenModel(new PatternModel() {Id = i});
                return;
            }
        }
    }
    public void Load(LevelData data)
    {
        foreach (var ui in ModelList.GetChildren())
            ui.QueueFree();
        for (int i = 0; i < Editor.MaxModelCount; i++)
        {
            var patt = data.PatternModels[i];
            if (patt == null) continue;
            var ui = PatternModelUi.Instantiate<PatternUi>();
            ui.ApplyPattern(patt);
            ModelList.AddChild(ui);
            ui.Pressed += () => LoadPattern(patt);
        }
    }
    public void ProjectileModelUpdated(ProjectileModel model)
    {
        if (Model.SpawningType == ModelType.Projectile && model.Id == Model.SpawningId)
        {
            Spawning.Load(Model);
        }
    }
    public void LoadPattern(PatternModel newModel)
    {
        newModel ??= new();
        Model = new PatternModel(newModel);
        Spawning.Load(Model);
        Info.Load(Model);
        PatternPreview.Load(Model);
    }
}