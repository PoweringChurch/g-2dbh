using System;
using Godot;

public partial class PatternInfo : Control
{
    public PatternModel Model;
    [Export] private SpinBox IdSelect;
    [Export] private LineEdit PatternName;
    [Export] private Button DeleteBtn;
    [Export] private Button NextFreeBtn;
    [Export] private Button SaveBtn;
    private Editor e => Editor.Instance;
    public override void _Ready()
    {
        SaveBtn.Pressed += Save;
        DeleteBtn.Pressed += () => e.DeletePatternModel((int)IdSelect.Value);
        NextFreeBtn.Pressed += () => IdSelect.Value = PatternCreator.GetNextFreeId();
    }
    private void Save()
    {
        Model.Name = PatternName.Text;
        Model.Id = (int)IdSelect.Value;
        e.SavePatternModel(new PatternModel(Model));
    }
    public void Load(PatternModel newModel)
    {
        Model = newModel;
        IdSelect.Value = newModel.Id;
        PatternName.Text = newModel.Name;
    }
}