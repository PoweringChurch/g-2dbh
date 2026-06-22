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
    [Export] LineEdit ModelIdInput;

    [Export] LineEdit IdInput;
    [Export] Button Save;
    [Export] MessageDisplay ErrorDisplay;
    private Editor e;
    private PatternModel model = null;
    public PatternModel PatternModel => model;
    public delegate void PatternModelUpdatedEventHandler(PatternModel model, string oldId);
    public event PatternModelUpdatedEventHandler ModelSaved;
    private static readonly Dictionary<string, double> testCtx = new() {["i"] = 0, ["n"] = 1};
    public override void _Ready()
    {
        e = GetNode<Editor>("/root/Editor");

        TInput.ValueChanged          += OnTChanged;
        TSlider.ValueChanged         += OnTSliderChanged;

        FnXInput.TextChanged += OnFnXChanged; // the rest follow this pattern basically
        FnYInput.TextChanged += OnFnYChanged;
        FnTInput.TextChanged += OnFnTChanged;
        FnFwdInput.TextChanged += OnFnFwdChanged;
        ModelIdInput.TextChanged += OnProjectileModelIdChanged;
        CountInput.ValueChanged += OnCountChanged;
        Save.Pressed += OnSavePressed;
        Zoom.ValueChanged += OnZoomChanged;
    }
    public void OnZoomChanged(double value)
    {
        Preview.Scale = Vector2.One*(float)value;
    }
    public void OnOptionSelected(long index)
    {
        var model = e.ProjectileRegistry.Models[(int)index];
        Preview.ProjModel = model;
    }
    public void OnProjectileModelIdChanged(string text)
    {
        var projmodel = e.ProjectileRegistry.GetModel(text);
        if (projmodel != null)
        {
            ErrorDisplay.ClearMessage("ProjectileId");
            Preview.ProjModel = projmodel;
            Preview.ProjModelFnX = ExpressionParser.Parse(projmodel.FunctionX);
            Preview.ProjModelFnY = ExpressionParser.Parse(projmodel.FunctionY);
            model.ProjectileId = text;
        }
        else ErrorDisplay.SetMessage("ProjectileId", $"[Projectile Model Id] Projectile of id '{text}' does not exist.");
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
            ErrorDisplay.SetMessage("Save", "[Save] Cannot save with unresolved errors.");
            return;
        }
        model.Id = IdInput.Text;
        if (string.IsNullOrWhiteSpace(model.Id))
        {
            ErrorDisplay.SetMessage("Save", "[Save] Model must have an ID.");
            return;
        }
        ErrorDisplay.ClearMessage("Save");
        var existing = e.PatternRegistry.GetModel(model.Id);
        if (existing != null)
            e.PatternRegistry.UpdateModel(model);
        else
            e.PatternRegistry.AddModel(model);
        ErrorDisplay.ClearMessage("Save");
        ModelSaved?.Invoke(model, null);
        LoadPattern(model);
    }
    // input & input validation functions
    private void OnFnXChanged(string text)
    {
        try
        {
            var fn = ExpressionParser.Parse(text);
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
            var fn = ExpressionParser.Parse(text);
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
            var fn = ExpressionParser.Parse(text);
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
            var fn = ExpressionParser.Parse(text);
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
        IdInput.Text = loadModel.Id;
        FnXInput.Text = loadModel.FunctionX;
        FnYInput.Text = loadModel.FunctionY;
        FnTInput.Text = loadModel.FunctionT;
        FnFwdInput.Text = loadModel.FunctionFwd;
        ModelIdInput.Text = loadModel.ProjectileId;
        CountInput.Value = loadModel.Count;
        OnProjectileModelIdChanged(ModelIdInput.Text);
        OnCountChanged(loadModel.Count);
    }
}