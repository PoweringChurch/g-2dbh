using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics;
public partial class ProjectileCreator : Control
{
    // Preview
    [Export] SpinBox IdInput;
    [Export] LineEdit NameInput;
    [Export] ProjectileModelPreview Preview;
    [Export] SpinBox TInput; // float
    [Export] SpinBox RenderScaleInput;
    [Export] HSlider TSlider;
    [Export] VSlider Zoom;
    [Export] LineEdit TextureInput;
    [Export] Label XDisplay;
    [Export] Label YDisplay;
    // Behaviour
    [Export] MessageDisplay ErrorDisplay;
    [Export] LineEdit FnXInput;
    [Export] LineEdit FnYInput;
    [Export] SpinBox LifetimeInput; // float
    [Export] CheckButton PersistantCheckbutton;
    // Collision
    [Export] CheckButton UseShapeCheckbutton;
    [Export] SpinBox Radius; // float
    [Export] ShapeEditor ShapeEditor;
    [Export] Button Save;
    private ProjectileModel model = null;
    public ProjectileModel ProjectileModel => model;
    private Editor e;
    public delegate void ModelSaveEventHandler(ProjectileModel model);
    public event ModelSaveEventHandler ModelSaved;
    private double time = 0;
    private int _currentId = 0;
    private static readonly EvalContext testCtx = new() { T = 0 };
    public override void _Ready()
    {
        base._Ready();
        e = GetNode<Editor>("/root/Editor");
        UseShapeCheckbutton.Toggled += ToggleCollisionParams;

        TInput.ValueChanged += OnTChanged;
        TSlider.ValueChanged += OnTSliderChanged;
        TextureInput.TextChanged += OnTextureChanged;
        FnXInput.TextChanged += OnFnXChanged;
        FnYInput.TextChanged += OnFnYChanged;
        LifetimeInput.ValueChanged += OnLifetimeChanged;
        PersistantCheckbutton.Toggled += OnPersistantToggled;
        UseShapeCheckbutton.Toggled += OnUseShapeToggled;
        Radius.ValueChanged += OnRadiusChanged;
        RenderScaleInput.ValueChanged += OnRenderScaleChanged;
        Save.Pressed += OnSavePressed;
        Zoom.ValueChanged += OnZoomChanged;

        ShapeEditor.ShapeUpdated += OnShapeUpdated;
    }

    private void OnRenderScaleChanged(double value)
    {
        Preview.RenderScale = (float)value;
        model.RenderScale = (float)value;
    }
    private void OnTChanged(double t)
    {
        time = t;
        UpdateTime(false);
    }
    private void OnTSliderChanged(double value)
    {
        time = value;
        UpdateTime(true);
    }
    private void UpdateTime(bool tinput)
    {
        if (tinput)
            TInput.SetValueNoSignal(time);
        else
            TSlider.SetValueNoSignal(time);
        Preview.T = time;
        RecalculatePosition();
    }
    private void OnTextureChanged(string text)
    {
        Texture2D found = RenderingUtils.LoadTexture(e.LevelPath + "images/", text);
        if (found != null || text == "default")
        {
            model.Texture = text;
            Preview.TextureName = text;
            ErrorDisplay.ClearMessage("Texture");
        }
        else ErrorDisplay.SetMessage("Texture", $"[Texture] Could not find texture of name {text} in {e.LevelPath}/images/");
    }
    private void OnShapeUpdated()
    {
        model.Shape = ShapeEditor.Points;
        Preview.Shape = ShapeEditor.Points;
    }
    private void OnZoomChanged(double value)
    {
        Preview.Scale = Vector2.One * (float)value;
    }
    private void OnSavePressed()
    {
        ErrorDisplay.ClearMessage("Save");
        if (ErrorDisplay.MessageCount > 0)
        {
            ErrorDisplay.SetMessage("Save", "[Save] Cannot save with unresolved errors.");
            return;
        }
        ErrorDisplay.ClearMessage("Save");
        model.Name = NameInput.Text;
        var newmodel = new ProjectileModel(model);
        e.SaveProjectileModel(newmodel, (int)IdInput.Value);
        ModelSaved?.Invoke(newmodel);
    }
    private void RecalculatePosition()
    {
        Preview.T = time;
        XDisplay.Text = (Preview.PreviewPosition.X).ToString("F2");
        YDisplay.Text = (Preview.PreviewPosition.Y).ToString("F2");
    }
    private void ToggleCollisionParams(bool to)
    {
        ShapeEditor.Visible = to;
        Radius.Visible = !to;
    }
    private void OnFnXChanged(string text)
    {
        try
        {
            var fn = ExpressionHandler.Parse(text);
            fn.Eval(testCtx);
            Preview.FnX = fn;
            model.FunctionX = text;
            RecalculatePosition();
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
            RecalculatePosition();
            ErrorDisplay.ClearMessage("FnY");
        }
        catch (Exception e) { ErrorDisplay.SetMessage("FnY", "[Function Y] " + e.Message); }
    }
    private void OnLifetimeChanged(double lifetime)
    {
        model.Lifetime = (float)lifetime;
        Preview.Lifetime = (float)lifetime;
        TSlider.MaxValue = lifetime;
        TInput.MaxValue = lifetime;
    }
    private void OnPersistantToggled(bool on) =>
        model.Persistant = on;
    private void OnUseShapeToggled(bool on)
    {
        model.UseShape = on;
        Preview.UseShape = on;
        ToggleCollisionParams(on);
    }
    private void OnRadiusChanged(double radius)
    {
        Preview.Radius = (float)radius;
        model.Radius = (float)radius;
    }

    public void LoadProjectile(ProjectileModel newModel)
    {
        model = new ProjectileModel(newModel);
        IdInput.Value = newModel.Id;
        NameInput.Text = newModel.Name;
        TextureInput.Text = newModel.Texture;
        FnXInput.Text = newModel.FunctionX;
        FnYInput.Text = newModel.FunctionY;
        LifetimeInput.Value = newModel.Lifetime;
        Radius.Value = newModel.Radius;
        PersistantCheckbutton.ButtonPressed = newModel.Persistant;
        UseShapeCheckbutton.ButtonPressed =newModel.UseShape; // doesnt need its on changed function because setting it like this automatically calls it 
        Preview.T = 0;
        RenderScaleInput.Value = newModel.RenderScale;
        ToggleCollisionParams(newModel.UseShape);
        OnRenderScaleChanged(newModel.RenderScale);
        OnFnXChanged(newModel.FunctionX);
        OnFnYChanged(newModel.FunctionY);
        OnLifetimeChanged(newModel.Lifetime);
        OnRadiusChanged(newModel.Radius);
        OnTextureChanged(newModel.Texture);
    }
}
