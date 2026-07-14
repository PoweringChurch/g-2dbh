using Godot;
public partial class ProjectileCreator : Control
{
    private ProjectileModel Model;
    private Editor e => Editor.Instance;
    [Export] ProjectileCollisionEditor Collision;
    [Export] ProjectileVisuals Visuals;
    [Export] ProjectileMovementEditor MovementInspector;
    [Export] ProjectileInfo Info;
    [Export] PackedScene ProjectileModelUi;
    [Export] VBoxContainer ModelList;
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
        Collision.Load(Model);
        Visuals.Load(Model);
        MovementInspector.Load(Model);
        Info.Load(Model);
    }
}
