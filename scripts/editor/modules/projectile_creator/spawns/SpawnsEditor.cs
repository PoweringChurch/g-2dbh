using Godot;

public partial class SpawnsEditor : Control
{
    public ProjectileModel Model;
    [Export] Container SpawnsUiContainer;
    [Export] SpinBox MaxDepth;
    [Export] Button NewButton;
    [Export] PackedScene SpawnEditorUi;
    [Export] MessageDisplay WarningDisplay;
    public override void _Ready()
    {
        NewButton.Pressed += NewSpawnReference;
        MaxDepth.ValueChanged += (v) => Model.MaxDepth = (int)v;
    }
    private void NewSpawnReference()
    {
        var reference = new EditorReference();
        Model.Spawns.Add(reference);
        var newUi = SpawnEditorUi.Instantiate<ReferenceUi>();
        newUi.Load(reference);
        SpawnsUiContainer.AddChild(newUi);
        newUi.remove.Pressed += () =>
        {
            newUi.QueueFree();
            Model.Spawns.Remove(reference);
            TrySpawnSizeWarning();
            ProjectileCreator.Instance.ProjectilePreview.MarkDirty();

        };
        TrySpawnSizeWarning();
        ProjectileCreator.Instance.ProjectilePreview.MarkDirty();
    }
    public void Load(ProjectileModel model)
    {
        foreach (var child in SpawnsUiContainer.GetChildren())
            child.QueueFree();
        Model = model;
        MaxDepth.SetValueNoSignal(model.MaxDepth);
        for (int i = 0; i < Model.Spawns.Count; i++)
        {
            var reference = Model.Spawns[i];
            var newUi = SpawnEditorUi.Instantiate<ReferenceUi>();
            newUi.Load(reference);
            SpawnsUiContainer.AddChild(newUi);
        }
        TrySpawnSizeWarning();
        ProjectileCreator.Instance.ProjectilePreview.MarkDirty();
    }
    private void TrySpawnSizeWarning()
    {
        if (Model.Spawns.Count > 9)
            WarningDisplay.SetMessage("spawnSize", "Consider using a Pattern instead. Patterns can be quicker to place spawned objects with if the objects follow a set path.");
        else 
            WarningDisplay.ClearMessage("spawnSize");
    }
}