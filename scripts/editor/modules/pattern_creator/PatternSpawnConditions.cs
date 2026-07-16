using System;
using Godot;

public partial class PatternSpawnConditions : Control
{
    private static readonly EvalContext testCtx = new() { T = 0, L = 1 };
    public PatternModel Model;
    private Preset[] presets =
    {
        new()
        {
            Name = "Circle",
            FunctionX = "sin(i/n*tau)*50",
            FunctionY = "cos(i/n*tau)*50"
        }
    };
    [Export] private Container PresetContainer;
    [Export] private LineEdit Xi;
    [Export] private LineEdit Yi;
    [Export] private LineEdit Fi;
    [Export] private LineEdit Ti;
    [Export] private SpinBox Count;
    [Export] private SpinBox ProjectileId;
    [Export] private MessageDisplay ErrorDisplay;
    private PatternPreview pp => PatternCreator.Instance.PatternPreview;
    public override void _Ready()
    {
        for (int i = 0; i < presets.Length; i++)
        {
            var preset = presets[i];
            var presetBtn = new Button() {Text = preset.Name, SizeFlagsHorizontal = SizeFlags.ExpandFill};
            presetBtn.Pressed += () =>
            {
                if (preset.FunctionX != null) { Xi.Text = preset.FunctionX; FunctionChanged("x(i)", preset.FunctionX); }
                if (preset.FunctionY != null) { Yi.Text = preset.FunctionY; FunctionChanged("y(i)", preset.FunctionY); }
                if (preset.FunctionF != null) { Fi.Text = preset.FunctionF; FunctionChanged("f(i)", preset.FunctionF); }
                if (preset.FunctionT != null) { Ti.Text = preset.FunctionT; FunctionChanged("t(i)", preset.FunctionT); }
            };
            PresetContainer.AddChild(presetBtn);
        }
        Xi.TextChanged += (text) => FunctionChanged("x(i)", text);
        Yi.TextChanged += (text) => FunctionChanged("y(i)", text);
        Fi.TextChanged += (text) => FunctionChanged("f(i)", text);
        Ti.TextChanged += (text) => FunctionChanged("t(i)", text);
        Count.ValueChanged += (v) => { Model.Count = (int)v; pp.MarkDirty(); };
        ProjectileId.ValueChanged += ProjectileIdChanged;
    }
    private void ProjectileIdChanged(double v)
    {
        int id = (int)v;
        if (Editor.Instance.ProjectileModels[id] == null)
        {
            ErrorDisplay.SetMessage("Projectile Id", $"[Projectile Id] Projectile of id {id} does not exist");
            return;
        }
        ErrorDisplay.ClearMessage("Projectile Id");
        Model.Id = id;
        pp.MarkDirty();
    }
    private void FunctionChanged(string funcName, string text)
    {
        try
        {
            var fn = ExpressionHandler.Parse(text);
            fn.Eval(testCtx);
            switch (funcName)
            {
                case "x(i)": Model.FunctionX = text; Model.fnx = ExpressionHandler.Compile(fn); break;
                case "y(i)": Model.FunctionY = text; Model.fny = ExpressionHandler.Compile(fn); break;
                case "f(i)": Model.FunctionF = text; Model.fnf = ExpressionHandler.Compile(fn); break;
                case "t(i)": Model.FunctionT = text; Model.fnt = ExpressionHandler.Compile(fn); break;
            }
            pp.MarkDirty();
            ErrorDisplay.ClearMessage(funcName);
        }
        catch (Exception e) { ErrorDisplay.SetMessage(funcName, $"[{funcName}] {e.Message}"); }
    }
    public void Load(PatternModel newModel)
    {
        Model = newModel;
        Xi.Text = Model.FunctionX;
        Yi.Text = Model.FunctionY;
        Fi.Text = Model.FunctionF;
        Ti.Text = Model.FunctionT;
        Count.Value = Model.Count;
        ProjectileId.Value = Model.ProjectileId;
        FunctionChanged("x(i)", Model.FunctionX);
        FunctionChanged("y(i)", Model.FunctionY);
        FunctionChanged("f(i)", Model.FunctionF);
        FunctionChanged("t(i)", Model.FunctionT);
    }
}