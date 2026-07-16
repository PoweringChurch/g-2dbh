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
        },
        new()
        {
            Name = "Circle",
            FunctionX = "sin(t/l*tau)*100",
            FunctionY = "cos(t/l*tau)*100"
        },
        new()
        {
            Name = "Accelerating",
            FunctionY = "1000*(t/l)^2"
        },
        new()
        {
            Name = "Decelerating",
            FunctionY = "1000*sqrt(t/l)"
        },
        new()
        {
            Name = "Straight Spin",
            FunctionY = "1000*(t/l)",
            FunctionF = "(t/l) * tau * 3"
        },
        new()
        {
            Name = "Arc",
            FunctionX = "sin(t/l*pi) * 300",
            FunctionY = "(1 - cos(t/l*pi)) * 300"
        },
        new()
        {
            Name = "Bounce",
            FunctionY = "1000 * abs(sin(t/l*pi*2))"
        },
        new()
        {
            Name = "Slingshot",
            FunctionY = "1000 * (t/l)^3"
        },
        new()
        {
            Name = "Wide Arc Spin",
            FunctionX = "sin(t/l*pi) * 300",
            FunctionY = "(1 - cos(t/l*pi)) * 300",
            FunctionF = "(t/l) * pi"
        },
        new()
        {
            Name = "Figure Eight",
            FunctionX = "sin(t/l*tau) * 100",
            FunctionY = "sin(t/l*tau*2) * 50"
        },
        new()
        {
            Name = "Orbit",
            FunctionX = "cos(t/l*tau) * 150",
            FunctionY = "sin(t/l*tau) * 150",
            FunctionF = "t/l*tau + pi/2"
        },
        new()
        {
            Name = "Rising Spin",
            FunctionY = "1000*(t/l)",
            FunctionF = "sin(t/l*tau*2) * 0.5"
        },
        new()
        {
            Name = "Pendulum",
            FunctionX = "sin(t/l*tau*2) * exp(-(t/l)*2) * 150",
            FunctionY = "1000*(t/l)"
        },
    };
    [Export] private Container PresetContainer;
    [Export] private LineEdit Xt;
    [Export] private Button XtClear;
    [Export] private LineEdit Yt;
    [Export] private Button YtClear;
    [Export] private LineEdit Ft;
    [Export] private Button FtClear;
    [Export] private SpinBox Lifetime;
    [Export] private MessageDisplay ErrorDisplay;
    private ProjectilePreview pp => ProjectileCreator.Instance.ProjectilePreview;
    public override void _Ready()
    {
        for (int i = 0; i < presets.Length; i++)
        {
            var preset = presets[i];
            var presetBtn = new Button() { Text = preset.Name, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            presetBtn.Pressed += () =>
            {
                if (preset.FunctionX != null) { Xt.Text = preset.FunctionX; FunctionChanged("x(t)", preset.FunctionX); }
                if (preset.FunctionY != null) { Yt.Text = preset.FunctionY; FunctionChanged("y(t)", preset.FunctionY); }
                if (preset.FunctionF != null) { Ft.Text = preset.FunctionF; FunctionChanged("f(t)", preset.FunctionF); }
            };
            PresetContainer.AddChild(presetBtn);
        }
        Xt.TextChanged += (text) => FunctionChanged("x(t)", text);
        Yt.TextChanged += (text) => FunctionChanged("y(t)", text);
        Ft.TextChanged += (text) => FunctionChanged("f(t)", text);
        Lifetime.ValueChanged += (v) => { Model.Lifetime = v; pp.MarkDirty(); };
        XtClear.Pressed += () => { Xt.Text = "0"; FunctionChanged("x(t)", "0"); };
        YtClear.Pressed += () => { Yt.Text = "0"; FunctionChanged("y(t)", "0"); };
        FtClear.Pressed += () => { Ft.Text = "0"; FunctionChanged("f(t)", "0"); };
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