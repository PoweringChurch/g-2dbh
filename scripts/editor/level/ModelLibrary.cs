using Godot;
using System.Collections.Generic;

public partial class ModelLibrary : Control
{
    [Export] VBoxContainer PatternModelList;
    [Export] VBoxContainer ProjectileModelList;
    private PackedScene ModelUITemplate = ResourceLoader.Load<PackedScene>("res://data/scenes/ui/model_ui.tscn");
    private Editor e;

    public IEditorModel SelectedModel { get; private set; }

    public override void _Ready()
    {
        base._Ready();
        e = GetNode<Editor>("/root/Editor");
    }

    public void OnModelSaved(ProjectileModel _) => Refresh();
    public void OnModelSaved(PatternModel _) => Refresh();

    public void Refresh()
    {
        Load((IEditorModel[])e.PatternModels, PatternModelList);
        Load((IEditorModel[])e.ProjectileModels, ProjectileModelList);
    }
    private void Load(IEditorModel[] models, VBoxContainer container)
    {
        foreach (var child in container.GetChildren())
            child.QueueFree();
        foreach (var m in models)
        {
            if (m == null)
                continue;
            var newTemplate  = ModelUITemplate.Instantiate<VBoxContainer>();
            var colorDisplay = newTemplate.GetNode<ColorRect>("Id/Color");
            var idField      = newTemplate.GetNode<Label>("Id/Field");
            var nameField    = newTemplate.GetNode<Label>("Name/Field");
            var selectButton = newTemplate.GetNode<Button>("Interact/Select");
            var deleteButton = newTemplate.GetNode<Button>("Interact/Delete");
            nameField.Text = m.Name;
            idField.Text = m.Id.ToString();
            colorDisplay.Color = RenderingUtils.ColorFromString(m.Name);
            selectButton.Pressed += () =>
            {
                SelectedModel = m;
                e.OpenModel(m);
            };
            deleteButton.Pressed += () =>
            {
                if (SelectedModel == m)
                    SelectedModel = null;

                if (m is ProjectileModel pm)
                {
                    if (e.SelectedModel == pm)
                        e.OpenModel(new ProjectileModel());
                    e.RemoveProjectileModel(pm.Id);
                }
                else if (m is PatternModel ptm)
                {
                    if (e.SelectedModel == ptm)
                        e.OpenModel(new PatternModel());
                    e.RemovePatternModel(ptm.Id);
                }
                GD.Print($"[ModelLibrary] Deleted model {m.Name}");
                newTemplate.QueueFree();
            };

            container.AddChild(newTemplate);
        }
    }
}