using Godot;
using System;
using System.Linq;

public partial class DefinitionUi : Control
{
    static readonly EvalContext testContext = new() {T = 1, I = 1, L = 1, N = 1};
    [Export] public LineEdit VarName;
    [Export] public LineEdit Definition;
    [Export] public Button Remove;
    [Export] public MessageDisplay ErrorDisplay;
    public string currentName = "";
    Editor e;
    public override void _Ready()
    {
        e = GetNode<Editor>("/root/Editor");
        VarName.TextChanged += NameChanged;
        Definition.TextChanged += DefinitionChanged;
        Remove.Pressed += RemovePressed;
    }
    public void SetParams(string varname, string definition)
    {
        VarName.Text = varname;
        Definition.Text = definition;
        NameChanged(varname);
        DefinitionChanged(definition);
    }
    private void DefinitionChanged(string to)
    {
        try
        {
            var fn = ExpressionHandler.Parse(to);
            fn.Eval(testContext);
            CustomVariableExpr.Definitions[currentName] = fn;
            e.levelData.CustomVariables[currentName] = to;
            ErrorDisplay.ClearMessage("Definition");
        }
        catch (Exception e) { ErrorDisplay.SetMessage("Definition", "[Definition] " + e.Message); }
    }
    private void RemovePressed()
    {
        CustomVariableExpr.Definitions.Remove(currentName);
        QueueFree();
    }
    private void NameChanged(string to)
    {
        bool hasInvalidChars = to.Any(c => !char.IsLetterOrDigit(c));
        if (hasInvalidChars)
        {
            ErrorDisplay.SetMessage("Name", "[Variable Name] Name contains invalid characters");
            return;
        }
        if (to is "pi" or "tau" or "phi" or "deg2rad" or "rad2deg")
        {
            ErrorDisplay.SetMessage("Name", "[Variable Name] A constant with this name already exists");
            return;
        }
        if (CustomVariableExpr.Definitions.ContainsKey(to))
        {
            ErrorDisplay.SetMessage("Name", "[Variable Name] A variable with this name already exists");
            return;
        }
        if (string.IsNullOrEmpty(to))
        {
            ErrorDisplay.SetMessage("Name", "[Variable Name] Variable must have a name");
            return;
        }
        ErrorDisplay.ClearMessage("Name");
        CustomVariableExpr.Definitions.Remove(currentName);
        e.levelData.CustomVariables.Remove(currentName);
        currentName = to;
        DefinitionChanged(Definition.Text);
    }
}
