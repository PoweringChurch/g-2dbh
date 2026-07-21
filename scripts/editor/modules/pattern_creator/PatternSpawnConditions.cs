using System;
using System.Collections.Generic;
using Godot;

public partial class PatternSpawnConditions : Control
{
    private static readonly EvalContext testCtx = new() { T = 0, L = 1 };
    public PatternModel Model;
    private Preset[] presets =
    {
        // overwrites
        new()
        {
            Name = "Explosion",
            FunctionX = "sin((i/n) * tau) * 90",
            FunctionY = "cos((i/n) * tau) * 90",
            FunctionF = "(i/n) * tau",
            FunctionT = "(i/n) * 0.5"
        },
        new()
        {
            Name = "Implosion",
            FunctionX = "sin((i/n) * tau) * 90",
            FunctionY = "cos((i/n) * tau) * 90",
            FunctionF = "(i/n) * tau + pi",
            FunctionT = "1 - (i/n) * 0.5"
        },
        // partial
        new()
        {
            Name = "Fan",
            FunctionX = "sin( (tau/8) * ( (i/n) - 0.5) ) * 40",
            FunctionY = "cos( (tau/8) * ( (i/n) - 0.5) ) * 40",
            FunctionF = "(tau/8) * ( (i/n) - 0.5)"
        },
        new()
        {
            Name = "Circle",
            FunctionX = "sin( (i/n) * tau) * 50",
            FunctionY = "cos( (i/n) * tau) * 50"
        },
        new()
        {
            Name = "Wall",
            FunctionX = "(i/n - 0.5) * 100",
            FunctionY = "0"
        },
        // forward
        new()
        {
            Name = "Outward",
            FunctionF = "(i/n) * tau"
        },
        new()
        {
            Name = "Inward",
            FunctionF = "(i/n) * tau + tau/2"
        },
        // time
        new()
        {
            Name = "Over Time",
            FunctionT = "(i/n) * 5"
        },
        new()
        {
            Name = "Spaced by",
            FunctionT = "i * 5"
        },
    };
    [Export] private Container PresetContainer;
    [Export] private LineEdit Xi;
    [Export] private Button XiClear;
    [Export] private LineEdit Yi;
    [Export] private Button YiClear;
    [Export] private LineEdit Fi;
    [Export] private Button FiClear;
    [Export] private LineEdit Ti;
    [Export] private Button TiClear;
    [Export] private SpinBox Count;
    [Export] private SpinBox SpawningId;
    [Export] private OptionButton SpawningType;
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
        SpawningId.ValueChanged += SpawningIdChanged;
        XiClear.Pressed += () => {Xi.Text = "0"; FunctionChanged("x(i)", "0");};
        YiClear.Pressed += () => {Yi.Text = "0"; FunctionChanged("y(i)", "0");};
        FiClear.Pressed += () => {Fi.Text = "0"; FunctionChanged("f(i)", "0");};
        TiClear.Pressed += () => {Ti.Text = "0"; FunctionChanged("t(i)", "0");};
        SpawningType.ItemSelected += (idx) => {Model.SpawningType = (ModelType)idx; SpawningIdChanged(Model.SpawningId); };
    }
    private bool RecurseCheckInvalid(int currentId, ModelType currentType, HashSet<int> visitedPatterns = null)
    {
        visitedPatterns ??= new HashSet<int>();
        if (currentType == ModelType.Pattern)
        {
            if (currentId == Model.Id || visitedPatterns.Contains(currentId))
                return true;
            visitedPatterns.Add(currentId);
        }
        // recurse
        if (currentType == ModelType.Pattern)
        {
            var pattern = Editor.Instance.PatternModels[currentId];
            // traverse to whatever this pattern spawns
            return RecurseCheckInvalid(pattern.SpawningId, pattern.SpawningType, new HashSet<int>(visitedPatterns));
        }
        else if (currentType == ModelType.Projectile)
        {
            var proj = Editor.Instance.ProjectileModels[currentId];
            // check all child models spawned by this projectile
            foreach (var reference in proj.Spawns)
                if (RecurseCheckInvalid(reference.Id, reference.Type, new HashSet<int>(visitedPatterns)))
                    return true;
        }
        return false; // valid
    }
    private void SpawningIdChanged(double v)
    {
        int id = (int)v;
        if (Model.SpawningType == ModelType.Projectile && Editor.Instance.ProjectileModels[id] == null)
        {
            ErrorDisplay.SetMessage("SpawningId", $"[Spawning Id] Projectile of id {id} does not exist");
            return;
        } else if (Model.SpawningType == ModelType.Pattern && Editor.Instance.PatternModels[id] == null)
        {
            ErrorDisplay.SetMessage("SpawningId", $"[Spawning Id] Pattern of id {id} does not exist");
            return;
        } else if (RecurseCheckInvalid(id, Model.SpawningType))
        {
            ErrorDisplay.SetMessage("SpawningId", $"[Spawning Id] This spawn id would cause a recursive loop");
            return;
        }
        ErrorDisplay.ClearMessage("SpawningId");
        Model.SpawningId = id;
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
        SpawningId.SetValueNoSignal(Model.SpawningId);
        SpawningType.Select((int)Model.SpawningType);
        GD.Print(SpawningType);
        FunctionChanged("x(i)", Model.FunctionX);
        FunctionChanged("y(i)", Model.FunctionY);
        FunctionChanged("f(i)", Model.FunctionF);
        FunctionChanged("t(i)", Model.FunctionT);
    }
}