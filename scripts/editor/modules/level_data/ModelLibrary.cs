using Godot;
using System;
using System.Collections.Generic;

public partial class ModelLibrary : Control
{
    [Export] VBoxContainer PatternModelList;
    [Export] VBoxContainer ProjectileModelList;
    [Export] Button ProjectileNextFree;
    [Export] Button PatternNextFree;
    [Export] PackedScene ModelUITemplate;
    private Editor e => Editor.Instance;

    public IEditorModel SelectedModel { get; private set; }

    public override void _Ready()
    {
        base._Ready();
        ProjectileNextFree.Pressed += OnProjNextFree;
        PatternNextFree.Pressed += OnPattNextFree;
    }
    private void OnPattNextFree()
    {
        for (int i = 0; i < Editor.MaxModelCount; i++)
        {
            if (e.PatternModels[i] == null)
            {
                e.OpenModel(new PatternModel() {Id = i});
                return;
            }
        }
    }
    private void OnProjNextFree()
    {
        for (int i = 0; i < Editor.MaxModelCount; i++)
        {
            if (e.ProjectileModels[i] == null)
            {
                e.OpenModel(new ProjectileModel() {Id = i});
                return;
            }
        }
    }
    public void Refresh()
    {
        Load((IEditorModel[])e.PatternModels, PatternModelList);
        Load((IEditorModel[])e.ProjectileModels, ProjectileModelList);
    }
    private void Load(IEditorModel[] models, VBoxContainer container)
    {
        foreach (var child in container.GetChildren())
            if (child != PatternNextFree && child != ProjectileNextFree)
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
                    //e.RemoveProjectileModel(pm.Id, newTemplate);
                }
                else if (m is PatternModel ptm)
                {
                    if (e.SelectedModel == ptm)
                        e.OpenModel(new PatternModel());
                    e.RemovePatternModel(ptm.Id, newTemplate);
                }
            };
            container.AddChild(newTemplate);
        }
    }
}