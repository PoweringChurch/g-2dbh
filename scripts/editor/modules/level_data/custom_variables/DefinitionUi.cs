using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class DefinitionUi : Control
{
    static readonly EvalContext testContext = new() {T = 1, I = 1, L = 1, N = 1};
    [Export] public LineEdit VarName;
    [Export] public LineEdit Definition;
    [Export] public Button Remove;
    [Export] public MessageDisplay ErrorDisplay;
    public string CurrentDef = "0";
    public string CurrentName = "";
    private Dictionary<string, string> modifying;
    private Editor e => Editor.Instance;
    public event Action<string, string, string> ModifiedContext;
    public override void _Ready()
    {
        if (modifying == null)
            Console.Inst.LogErr("This dictionary UI does not have a modifying dictionary set!");
        VarName.TextChanged += ChangeName;
        Definition.TextChanged += (txt) => {ChangeDefinition(CurrentName, txt); ModifiedContext?.Invoke(CurrentName, modifying[CurrentName], null);};
        Remove.Pressed += () => { modifying.Remove(CurrentName); QueueFree();};
    }
    public void SetParams(Dictionary<string, string> toMod, string varname, string definition)
    {
        modifying = toMod;
        VarName.Text = varname;
        Definition.Text = definition;
        ChangeName(varname);
        ChangeDefinition(varname, definition);
    }
    private void ChangeDefinition(string name, string newDef)
    {
        try
        {
            var fn = ExpressionHandler.Parse(newDef);
            fn.Eval(testContext);
            modifying[name] = newDef;
            ErrorDisplay.ClearMessage("Definition");
        }
        catch (Exception e) { ErrorDisplay.SetMessage("Definition", "[Definition] " + e.Message); }
    }
    private void ChangeName(string newName)
    {
        bool hasInvalidChars = newName.Any(c => !char.IsLetterOrDigit(c));
        if (hasInvalidChars)
        {
            ErrorDisplay.SetMessage("Name", "[Variable Name] Name contains invalid characters");
            return;
        }
        if (newName is "pi" or "tau" or "phi" or "deg2rad" or "rad2deg")
        {
            ErrorDisplay.SetMessage("Name", "[Variable Name] A constant with this name already exists");
            return;
        }
        if (CustomVariableExpr.Definitions.ContainsKey(newName))
        {
            ErrorDisplay.SetMessage("Name", "[Variable Name] A variable with this name already exists");
            return;
        }
        if (string.IsNullOrEmpty(newName))
        {
            ErrorDisplay.SetMessage("Name", "[Variable Name] Variable must have a name");
            return;
        }
        ErrorDisplay.ClearMessage("Name");
        // add def with new name
        string oldName = CurrentName;
        CurrentName = newName;
        ChangeDefinition(CurrentName, Definition.Text);
        modifying.Remove(oldName);
        ModifiedContext?.Invoke(CurrentName, Definition.Text, oldName);
    }
}
