using Godot;
using System;
using System.Linq;

public partial class CustomVariables : Control
{
    [Export] Button NewDefinition;
    [Export] Container List;
    [Export] PackedScene DefinitionUi;
    public override void _Ready()
    {
        NewDefinition.Pressed += () => CreateNewDefinition();
    }
    public void Load(LevelData data)
    {
        foreach (var child in List.GetChildren())
            child.QueueFree();
        CustomVariableExpr.Definitions.Clear();
        foreach (var def in data.CustomVariables)
        {
            var defui = CreateNewDefinition();
            defui.SetParams(def.Key, def.Value);
        }
    }
    private DefinitionUi CreateNewDefinition()
    {
        var definitionUi = DefinitionUi.Instantiate<DefinitionUi>();
        List.AddChild(definitionUi);
        definitionUi.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        return definitionUi;
    }
}
