using Godot;
public partial class ProjectileCreator : Control
{
    public static ProjectileCreator Instance;
    [Export] public ProjectilePreview ProjectilePreview {get; private set;}
    public ProjectileModel Model {get; private set;}
    [Export] ProjectileCollisionEditor Collision;
    [Export] ProjectileVisuals Visuals;
    [Export] ProjectileMovementEditor MovementInspector;
    [Export] ProjectileInfo Info;
    [Export] PackedScene ProjectileModelUi;
    [Export] VBoxContainer ModelList;
    public override void _EnterTree() => 
        Instance = this;
    public void Load(LevelData data)
    {
        foreach (var ui in ModelList.GetChildren())
            ui.QueueFree();
        for (int i = 0; i < Editor.MaxModelCount; i++)
        {
            var proj = data.ProjectileModels[i];
            if (proj == null) continue;
            var ui = ProjectileModelUi.Instantiate<ProjectileUi>();
            ui.ApplyProjectile(proj);
            ModelList.AddChild(ui);
            ui.Pressed += () => LoadProjectile(proj);
        }
    }
    public void LoadProjectile(ProjectileModel newModel)
    {
        newModel ??= new();
        Model = new ProjectileModel(newModel);
        MovementInspector.Load(Model);
        Collision.Load(Model);
        Visuals.Load(Model);
        Info.Load(Model);
        ProjectilePreview.Load(Model);
        ProjectilePreview.MarkDirty();
    }
}
