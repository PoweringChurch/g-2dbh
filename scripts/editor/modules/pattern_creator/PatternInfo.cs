using System;
using Godot;

public partial class PatternInfo : Control
{
    public PatternModel Model;
    [Export] private SpinBox IdSelect;
    [Export] private LineEdit ProjectileName;
    [Export] private Button DeleteBtn;
    [Export] private Button SaveBtn;
    public override void _Ready()
    {
        SaveBtn.Pressed += Save;
        DeleteBtn.Pressed += () => Editor.Instance.DeleteProjectileModel((int)IdSelect.Value);
    }
    private void Save()
    {
        Model.Name = ProjectileName.Text;
        Model.Id = (int)IdSelect.Value;
        Editor.Instance.SavePatternModel(new PatternModel(Model));
    }
    public void Load(PatternModel newModel)
    {
        Model = newModel;
        IdSelect.Value = newModel.Id;
        ProjectileName.Text = newModel.Name;
    }
}