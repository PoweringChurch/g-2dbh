using System;
using Godot;

public partial class ProjectileMovementEditor : Control
{
    private static readonly EvalContext testCtx = new() { T = 0, L = 1 };
    public ProjectileModel Model;
    private readonly Preset[] presets =
    {
        new()
        {
            Name = "Linear",
            FunctionY = "1000*(t/l)"
        }
    };
    [Export] private Container PresetContainer;
    [Export] private LineEdit Xt;
    [Export] private LineEdit Yt;
    [Export] private LineEdit Ft;
    [Export] private SpinBox Lifetime;
    [Export] private MessageDisplay ErrorDisplay;
    private ProjectilePreview pp => ProjectileCreator.Instance.ProjectilePreview;
    public override void _Ready()
    {
        for (int i = 0; i < presets.Length; i++)
        {
            var preset = presets[i];
            var presetBtn = new Button() {Text = preset.Name, SizeFlagsHorizontal = SizeFlags.ExpandFill};
            presetBtn.Pressed += () =>
            {
                if (preset.FunctionX != null) { Xt.Text = preset.FunctionX; FunctionChanged("x(i)", preset.FunctionX); }
                if (preset.FunctionY != null) { Yt.Text = preset.FunctionY; FunctionChanged("y(i)", preset.FunctionY); }
                if (preset.FunctionF != null) { Ft.Text = preset.FunctionF; FunctionChanged("f(i)", preset.FunctionF); }
            };
            PresetContainer.AddChild(presetBtn);
        }
        Xt.TextChanged += (text) => FunctionChanged("x(t)", text);
        Yt.TextChanged += (text) => FunctionChanged("y(t)", text);
        Ft.TextChanged += (text) => FunctionChanged("f(t)", text);
        Lifetime.ValueChanged += (v) => { Model.Lifetime = v; pp.MarkDirty(); };
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
            pp.MarkDirty();
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
        Lifetime.Value = Model.Lifetime;
        FunctionChanged("x(t)", Model.FunctionX);
        FunctionChanged("y(t)", Model.FunctionY);
        FunctionChanged("f(t)", Model.FunctionF);
    }
}