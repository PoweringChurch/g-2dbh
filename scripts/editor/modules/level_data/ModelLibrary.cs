using Godot;
using System;
using System.Collections.Generic;

public partial class ModelLibrary : Control
{
    [Export] Container PatternModelList;
    [Export] Container ProjectileModelList;
    [Export] PackedScene ModelUITemplate;
    [Export] Button OpenModelEditor;
    [Export] ModelEditor ModelEditor;
    private Editor e => Editor.Instance;
    public IEditorModel SelectedModel { get; private set; }
    public override void _Ready() =>
        OpenModelEditor.Pressed += () => ModelEditor.Visible = !ModelEditor.Visible;
    public void Refresh()
    {
        Load((IEditorModel[])e.PatternModels, PatternModelList);
        Load((IEditorModel[])e.ProjectileModels, ProjectileModelList);
    }
    private void Load(IEditorModel[] models, Container container)
    {
        foreach (var child in container.GetChildren())
            child.QueueFree();
        foreach (var m in models)
        {
            if (m == null)
                continue;
            var newTemplate  = ModelUITemplate.Instantiate<VBoxContainer>();
            var colorDisplay = newTemplate.GetNode<ColorRect>("Id/color");
            var idField      = newTemplate.GetNode<Label>("Id/id");
            var nameField    = newTemplate.GetNode<Label>("Id/name");
            var selectButton = newTemplate.GetNode<Button>("Interact/Select");
            var deleteButton = newTemplate.GetNode<Button>("Interact/Delete");
            nameField.Text = m.Name;
            idField.Text = m.Id.ToString();
            colorDisplay.Color = RenderingUtils.ColorFromString(m.Name);
            selectButton.Pressed += () =>
            {
                SelectedModel = m;
            };
            deleteButton.Pressed += () =>
            {
                if (SelectedModel == m)
                    SelectedModel = null;

                if (m is ProjectileModel pm)
                {
                    if (e.SelectedModel == pm)
                        e.OpenModel(new ProjectileModel());
                    e.DeleteProjectileModel(pm.Id);
                }
                else if (m is PatternModel ptm)
                {
                    if (e.SelectedModel == ptm)
                        e.OpenModel(new PatternModel());
                    e.DeletePatternModel(ptm.Id);
                }
            };
            container.AddChild(newTemplate);
        }
    }
}