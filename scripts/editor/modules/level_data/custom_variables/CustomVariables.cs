using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class CustomVariables : Control
{
    [Export] Button NewDefinition;
    [Export] Container List;
    [Export] PackedScene DefinitionUi;
    public override void _Ready()
    {
        NewDefinition.Pressed += () => CreateNewDefinition(Editor.Instance.levelData.CustomVariables, "", "0");
    }
    public void Load(LevelData data)
    {
        foreach (var child in List.GetChildren())
            child.QueueFree();
        CustomVariableExpr.Definitions.Clear();
        foreach (var def in data.CustomVariables)
        {
            CreateNewDefinition(data.CustomVariables, def.Key, def.Value);
        }
    }
    private void CreateNewDefinition(Dictionary<string, string> modifying, string name, string definition)
    {
        var defUi = DefinitionUi.Instantiate<DefinitionUi>();
        defUi.ModifiedContext += (name, def, old) =>
        {
            if (old != null && CustomVariableExpr.Definitions.TryGetValue(old, out var _))
                CustomVariableExpr.Definitions.Remove(old);
            CustomVariableExpr.Definitions[name] = ExpressionHandler.Parse(def);
        };
        defUi.SetParams(modifying, name, definition);
        List.AddChild(defUi);
        defUi.SizeFlagsHorizontal = SizeFlags.ExpandFill;
    }
}
