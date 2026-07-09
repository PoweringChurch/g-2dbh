using System;
using System.Collections.Generic;
using Godot;

public partial class PatternCreator : Control
{
    [Export] PatternPreview Preview;
    [Export] SpinBox TInput; // float
    [Export] HSlider TSlider;
    [Export] VSlider Zoom;
    private float time = 0;
    [Export] LineEdit FnXInput;
    [Export] LineEdit FnYInput;
    [Export] LineEdit FnTInput;
    [Export] LineEdit FnFwdInput;
    [Export] SpinBox  CountInput;
    [Export] SpinBox ProjectileIdInput;
    [Export] CheckButton FacePlayer;
    [Export] SpinBox IdInput;
    [Export] LineEdit NameInput;
    [Export] Button Save;
    [Export] MessageDisplay ErrorDisplay;
    private Editor e;
    private PatternModel model = null;
    public PatternModel PatternModel => model;
    public delegate void PatternModelUpdatedEventHandler(PatternModel model);
    public event PatternModelUpdatedEventHandler ModelSaved;
    private static readonly EvalContext testCtx = new() {I = 0, N = 1};
    public override void _Ready()
    {
        e = GetNode<Editor>("/root/Editor");

        TInput.ValueChanged          += OnTChanged;
        TSlider.ValueChanged         += OnTSliderChanged;

        FnXInput.TextChanged += OnFnXChanged; // the rest follow this pattern basically
        FnYInput.TextChanged += OnFnYChanged;
        FnTInput.TextChanged += OnFnTChanged;
        FnFwdInput.TextChanged += OnFnFwdChanged;
        ProjectileIdInput.ValueChanged += OnProjectileModelIdChanged;
        CountInput.ValueChanged += OnCountChanged;
        Save.Pressed += OnSavePressed;
        Zoom.ValueChanged += OnZoomChanged;
        FacePlayer.Toggled += OnFacePlayerToggled;
    }
    private void OnFacePlayerToggled(bool toggledOn)
    {
        model.FacePlayer = toggledOn;
    }
    public void OnModelUpdate(ProjectileModel newmodel)
    {
        if (newmodel.Id == model.ProjectileId)
        {
            ProjectileIdInput.Value = newmodel.Id;
        }
    }
    public void OnZoomChanged(double value)
    {
        Preview.Scale = Vector2.One*(float)value;
    }
    public void OnProjectileModelIdChanged(double id)
    {
        var projmodel = e.ProjectileModels[(int)id];
        if (projmodel != null)
        {
            ErrorDisplay.ClearMessage("ProjectileId");
            Preview.ProjModel = projmodel;
            Preview.ProjModelFnX = ExpressionHandler.Parse(projmodel.FunctionX);
            Preview.ProjModelFnY = ExpressionHandler.Parse(projmodel.FunctionY);
            model.ProjectileId = (int)id;
        }
        else ErrorDisplay.SetMessage("ProjectileId", $"[Projectile Model Id] Projectile of id '{(int)id}' is invalid");
    }
    private void OnTChanged(double t)
    {
        time = (float)t;
        UpdateTime(false);
        Preview.T = time;
    }
    private void OnTSliderChanged(double value) 
    {
        time = (float)value;
        UpdateTime(true);
        Preview.T = time;
    }
    private void UpdateTime(bool tinput)
    {
        if (tinput)
            TInput.SetValueNoSignal(time);
        else
            TSlider.SetValueNoSignal(time);
    }
    private void OnSavePressed()
    {
        ErrorDisplay.ClearMessage("Save");
        if (ErrorDisplay.MessageCount > 0)
        {
            ErrorDisplay.SetMessage("Save", "[Save] Cannot save with unresolved errors");
            return;
        }
        model.Name = NameInput.Text;
        ErrorDisplay.ClearMessage("Save");
        var clone = new PatternModel(model);
        e.SavePatternModel(clone, (int)IdInput.Value);
        ModelSaved?.Invoke(clone);
        LoadPattern(model);
    }
    // input & input validation functions
    private void OnFnXChanged(string text)
    {
        try
        {
            var fn = ExpressionHandler.Parse(text);
            fn.Eval(testCtx);
            Preview.FnX = fn;
            model.FunctionX = text;
            ErrorDisplay.ClearMessage("FnX");
        }
        catch (Exception e) { ErrorDisplay.SetMessage("FnX", "[Function X] " + e.Message); }
    }
    private void OnFnYChanged(string text)
    {
        try
        {
            var fn = ExpressionHandler.Parse(text);
            fn.Eval(testCtx);
            Preview.FnY = fn;
            model.FunctionY = text;
            ErrorDisplay.ClearMessage("FnY");
        }
        catch (Exception e) { ErrorDisplay.SetMessage("FnY", "[Function Y] " + e.Message); }
    }
    private void OnFnTChanged(string text)
    {
        try
        {
            var fn = ExpressionHandler.Parse(text);
            fn.Eval(testCtx);
            Preview.FnT = fn;
            model.FunctionT = text;
            ErrorDisplay.ClearMessage("FnT");
        }
        catch (Exception e) { ErrorDisplay.SetMessage("FnT", "[Function Time] " + e.Message); }
    }
    private void OnFnFwdChanged(string text)
    {
        try
        {
            var fn = ExpressionHandler.Parse(text);
            fn.Eval(testCtx);
            Preview.FnFwd = fn;
            model.FunctionFwd = text;
            ErrorDisplay.ClearMessage("FnFwd");
        }
        catch (Exception e) { ErrorDisplay.SetMessage("FnFwd", "[Function Forward] " + e.Message); }
    }
    private void OnCountChanged(double to) 
    {
        Preview.Count = (int)to;
        model.Count = (int)to;
        OnFnFwdChanged(FnFwdInput.Text);
        OnFnTChanged(FnTInput.Text);
        OnFnXChanged(FnXInput.Text);
        OnFnYChanged(FnYInput.Text);
    }
    public void LoadPattern(PatternModel loadModel)
    {
        model = new PatternModel(loadModel);
        IdInput.Value = model.Id;
        NameInput.Text = model.Name;
        FnXInput.Text = model.FunctionX;
        FnYInput.Text = model.FunctionY;
        FnTInput.Text = model.FunctionT;
        FnFwdInput.Text = model.FunctionFwd;
        ProjectileIdInput.Value = model.ProjectileId;
        CountInput.Value = model.Count;
        FacePlayer.ButtonPressed = model.FacePlayer;
        OnProjectileModelIdChanged(ProjectileIdInput.Value);
        OnCountChanged(model.Count);
    }
}