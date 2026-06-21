using Godot;
using System;
using System.Collections.Generic;
public partial class ProjectileCreator : Control
{
    // Preview
    [Export] ProjectileModelPreview Preview;
    [Export] SubViewportContainer VPContainer;
    [Export] SpinBox TInput; // float
    [Export] HSlider TSlider;
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
    [Export] LineEdit IdInput;
    [Export] Button Save;
    private ProjectileModel model = null;
    public ProjectileModel ProjectileModel => model;
    private Editor e;
    public delegate void ProjectileModelUpdatedEventHandler(ProjectileModel model, string oldId);
    public event ProjectileModelUpdatedEventHandler ModelSaved;
    private double time = 0;
    private static readonly Dictionary<string, double> testCtx = new() {["t"] = 0};
    public override void _Ready()
    {
        base._Ready();
        e = GetNode<Editor>("/root/Editor");
        UseShapeCheckbutton.Toggled += ToggleCollisionParams;

        TInput.ValueChanged          += OnTChanged;
        TSlider.ValueChanged         += OnTSliderChanged;
        TextureInput.TextChanged     += OnTextureChanged;
        FnXInput.TextChanged         += OnFnXChanged;
        FnYInput.TextChanged         += OnFnYChanged;
        LifetimeInput.ValueChanged   += OnLifetimeChanged;
        PersistantCheckbutton.Toggled+= OnPersistantToggled;
        UseShapeCheckbutton.Toggled  += OnUseShapeToggled;
        Radius.ValueChanged          += OnRadiusChanged;
        Save.Pressed                 += OnSavePressed;
        VPContainer.GuiInput         += OnVPGuiInput;

        ShapeEditor.ShapeUpdated     += OnShapeUpdated;
    }
    private void OnTChanged(double t)
    {
        time = t;
        UpdateTime(false);
        RecalculatePosition();
    }
    private void OnTSliderChanged(double value) 
    {
        time = value;
        UpdateTime(true);
        RecalculatePosition();
    }
    private void UpdateTime(bool tinput)
    {
        if (tinput)
            TInput.Value = time;
        else
            TSlider.SetValueNoSignal(time);
        Preview.T = time;
    }
    private void OnTextureChanged(string text)
    {
        Texture2D found = RenderingUtils.LoadTexture(e.LevelPath+"images/",text);
		if (found != null || text == "default")
		{
			model.Texture = text;
            Preview.TextureName = text;
			ErrorDisplay.ClearMessage("Texture");
		}
        else ErrorDisplay.SetMessage("Texture",$"[Texture] Could not find texture of name {text} in {e.LevelPath}/images/");
    }
    private void OnShapeUpdated()
    {
        model.Shape = ShapeEditor.Points;
        Preview.Shape = ShapeEditor.Points;
    }
    private void OnVPGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed)
        {
            if (mb.ButtonIndex == MouseButton.WheelUp)
                Preview.Scale = Vector2.One * Mathf.Clamp(Preview.Scale.X + 0.1f, 0.1f, 4f);
            else if (mb.ButtonIndex == MouseButton.WheelDown)
                Preview.Scale = Vector2.One * Mathf.Clamp(Preview.Scale.X - 0.1f, 0.1f, 4f);
        }
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
        var existing = e.ProjectileRegistry.GetModel(model.Id);
        if (existing != null)
            e.ProjectileRegistry.UpdateModel(model);
        else
            e.ProjectileRegistry.AddModel(model);
        ErrorDisplay.ClearMessage("Save");
        ModelSaved?.Invoke(model, null);
    }
    private void RecalculatePosition()
    {
        Preview.T = time;
        XDisplay.Text = (Preview.Position.X-128).ToString("F2");
        YDisplay.Text = (Preview.Position.Y-128).ToString("F2");
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
            var fn = ExpressionParser.Parse(text);
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
            var fn = ExpressionParser.Parse(text);
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
        IdInput.Text          = newModel.Id;
        TextureInput.Text     = newModel.Texture;
        FnXInput.Text         = newModel.FunctionX;
        FnYInput.Text         = newModel.FunctionY;
        LifetimeInput.Value    = newModel.Lifetime;
        Radius.Value           = newModel.Radius;
        PersistantCheckbutton.SetPressed(newModel.Persistant);
        UseShapeCheckbutton.SetPressed(newModel.UseShape);

        Preview.FnX = ExpressionParser.Parse(newModel.FunctionX);
        Preview.FnY = ExpressionParser.Parse(newModel.FunctionY);
        Preview.T         = 0;
        ToggleCollisionParams(newModel.UseShape);
    }
}
