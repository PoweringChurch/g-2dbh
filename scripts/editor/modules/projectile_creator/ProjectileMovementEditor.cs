using System;
using Godot;

public partial class ProjectileMovementEditor : Control
{
    private static readonly EvalContext testCtx = new() { T = 0, L = 1 };
    public ProjectileModel Model;
    [Export] private PresetMovementUi[] presets;
    [Export] private LineEdit Xt;
    [Export] private LineEdit Yt;
    [Export] private LineEdit Ft;
    [Export] private SpinBox Lifetime;
    [Export] private MessageDisplay ErrorDisplay;
    public override void _Ready()
    {
        for (int i = 0; i < presets.Length; i++)
        {
            var preset = presets[i];
            Xt.Text = preset.FunctionX;
            Yt.Text = preset.FunctionY;
            Ft.Text = preset.FunctionF;
        }
        Xt.TextChanged += (text) => FunctionChanged("x(t)", text);
        Yt.TextChanged += (text) => FunctionChanged("y(t)", text);
        Ft.TextChanged += (text) => FunctionChanged("f(t)", text);
        Lifetime.ValueChanged += (v) => Model.Lifetime = v;
    }
    private void FunctionChanged(string funcName, string text)
    {
        try
        {
            var fn = ExpressionHandler.Parse(text);
            fn.Eval(testCtx);
            switch (funcName)
            {
                case "x(t)": Model.FunctionX = text; Model.fnx = ExpressionHandler.Compile(fn); break;
                case "y(t)": Model.FunctionY = text; Model.fny = ExpressionHandler.Compile(fn); break;
                case "f(t)": Model.FunctionF = text; Model.fnf = ExpressionHandler.Compile(fn); break;
            }
            ErrorDisplay.ClearMessage(funcName);
        }
        catch (Exception e) { ErrorDisplay.SetMessage(funcName, $"[{funcName}] {e.Message}"); }
    }
    public void Load(ProjectileModel newModel)
    {
        Model = newModel;
        Xt.Text = Model.FunctionX;
        Yt.Text = Model.FunctionY;
        Ft.Text = Model.FunctionF;
    }
}