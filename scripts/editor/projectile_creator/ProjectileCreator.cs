using Godot;
using System;
using System.Collections.Generic;
using System.IO;
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
    [Export] CheckButton UseCustomTextureInput;
    [Export] CheckBox LockRotationCheck;
    [Export] LineEdit TextureInput;
    [Export] Label XDisplay;
    [Export] Label YDisplay;
    // Behaviour
    [Export] MessageDisplay ErrorDisplay;
    [Export] MessageDisplay WarningDisplay;
    [Export] LineEdit FnXInput;
    [Export] LineEdit FnYInput;
    [Export] SpinBox LifetimeInput; // float
    [Export] SpinBox TelegraphTimeInput;
    [Export] CheckBox PersistantCheckbutton;
    [Export] CheckBox FacePlayerCheckbutton;
    // Collision
    [Export] CheckBox CanCollideCheckbutton;
    [Export] CheckButton UseShapeCheckbutton;
    [Export] SpinBox Radius; // float
    [Export] ShapeEditor ShapeEditor;
    [Export] Control ShapeTranslate;
    // Spawn on death
    [Export] Control SpawnOnDeathUi;
    [Export] CheckButton SpawnModelOnDeathCheck;
    [Export] OptionButton DeathModelType;
    [Export] SpinBox SpawnOnDeathIdInput;
    [Export] SpinBox MaxDepthInput;

    [Export] Button Save;
    private ProjectileModel model = null;
    public ProjectileModel ProjectileModel => model;
    private Editor e;
    public delegate void ModelSaveEventHandler(ProjectileModel model);
    public event ModelSaveEventHandler ModelSaved;
    private double time = 0;
    private int _currentId = 0;
    private static readonly EvalContext testCtx = new() { T = 0, L = 1 };
    public override void _Ready()
    {
        base._Ready();
        e = GetNode<Editor>("/root/Editor");
        UseShapeCheckbutton.Toggled += ToggleCollisionParams;

        // preview
        TInput.ValueChanged += OnTChanged;
        TSlider.ValueChanged += OnTSliderChanged;
        TextureInput.TextChanged += OnTextureChanged;
        RenderScaleInput.ValueChanged += OnRenderScaleChanged;
        Zoom.ValueChanged += OnZoomChanged;
        LockRotationCheck.Toggled += OnLockRotationToggled;
        // behavior
        FnXInput.TextChanged += OnFnXChanged;
        FnYInput.TextChanged += OnFnYChanged;
        LifetimeInput.ValueChanged += OnLifetimeChanged;
        PersistantCheckbutton.Toggled += OnPersistantToggled;
        TelegraphTimeInput.ValueChanged += OnTelegraphValueChanged;
        FacePlayerCheckbutton.Toggled += OnFacePlayerToggled;
        // collision
        Radius.ValueChanged += OnRadiusChanged;
        UseShapeCheckbutton.Toggled += OnUseShapeToggled;
        ShapeEditor.ShapeUpdated += OnShapeUpdated;
        CanCollideCheckbutton.Toggled += OnCanCollideToggled;
        // on death model spawn
        SpawnModelOnDeathCheck.Toggled += OnSpawnModelOnDeathCheck;
        DeathModelType.ItemSelected += OnDeathModelItemSelected;
        SpawnOnDeathIdInput.ValueChanged += OnSpawnModelIdChanged;
        MaxDepthInput.ValueChanged += MaxDepthChanged;
        Save.Pressed += OnSavePressed;
    }    
    private void OnFacePlayerToggled(bool toggledOn)
    {
        model.FacePlayer = toggledOn;
    }
    private void OnLockRotationToggled(bool toggledOn)
    {
        model.LockRotation = toggledOn;
    }
    private void OnCanCollideToggled(bool toggledOn)
    {
        model.CanCollide = toggledOn;
        Preview.CanCollide = toggledOn;
    }
    private void OnTelegraphValueChanged(double value)
    {
        model.TelegraphTime = (float)value;
        Preview.TelegraphTime = (float)value;
        WarningDisplay.ClearMessage("Telegraph");
        if (value >= model.Lifetime)
            WarningDisplay.SetMessage("Telegraph", "[Telegraph time] Telegraph time is greater than lifetime, this projectile cannot collide");
    }
    private void MaxDepthChanged(double value)
    {
        model.MaxDepth = (int)value;
        WarningDisplay.ClearMessage("Depth");
        if (value > 2)
            WarningDisplay.SetMessage("Depth", "[Depth] High max depth values can cause lag");
    }
    private void OnSpawnModelIdChanged(double value)
    {
        int id = (int)value;
        if (model.SpawnOnDeathType == ModelType.Pattern)
        {
            var found = e.PatternModels[id];
            if (found != null)
            {
                ErrorDisplay.ClearMessage("SpawnId");
                model.SpawnOnDeathId = id;
            }
            else ErrorDisplay.SetMessage("SpawnId", $"[Spawn on death ID] Pattern of id '{id}' is invalid");
        }
        else if (model.SpawnOnDeathType == ModelType.Projectile)
        {
            var found = e.ProjectileModels[id];
            if (found != null)
            {
                ErrorDisplay.ClearMessage("SpawnId");
                model.SpawnOnDeathId = id;
            }
            else ErrorDisplay.SetMessage("SpawnId", $"[Spawn on death ID] Projectile of id '{id}' is invalid");
        }
        
    }
    private void OnDeathModelItemSelected(long index)
    {
        model.SpawnOnDeathType = (ModelType)index;
    }
    private void OnSpawnModelOnDeathCheck(bool toggledOn)
    {
        model.SpawnModelOnDeath = toggledOn;
        SpawnOnDeathUi.Visible = toggledOn;
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
        var path = UseCustomTextureInput.ButtonPressed ? $"res://data/default-assets/images/{text}" : $"{e.LevelPath}images/{text}";
        Texture2D found = RenderingUtils.LoadTexture(path);
        if (text == "default")
        {
            model.Texture = "default";
            Preview.Texture = "default";
            ErrorDisplay.ClearMessage("Texture");
            if (found != null)
                ErrorDisplay.SetMessage("Texture", $"[Texture] 'default' is a reserved name. Please change the name of this image.");
        }
        else if (found != null)
        {
            model.Texture = path;
            Preview.Texture = path;
            ErrorDisplay.ClearMessage("Texture");
        }
        else ErrorDisplay.SetMessage("Texture", $"[Texture] Could not find texture of name {text} in {e.LevelPath}/images/");
    }
    private void OnShapeUpdated()
    {
        model.Shape = CollisionUtils.Vect2sToFloatArr(ShapeEditor.Points);
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
            ErrorDisplay.SetMessage("Save", "[Save] Cannot save with unresolved errors");
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
        ShapeTranslate.Visible = to;
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
        Preview.L = (float)lifetime;
        TSlider.MaxValue = lifetime;
        TInput.MaxValue = lifetime;
        OnTelegraphValueChanged(model.TelegraphTime);
    }
    private void OnPersistantToggled(bool on)
    {
        model.Persistant = on;
    }
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
        // display
        IdInput.Value = model.Id;
        NameInput.Text = model.Name;

        TextureInput.Text = Path.GetFileName(model.Texture);
        UseCustomTextureInput.ButtonPressed = model.Texture.StartsWith("res://");
        RenderScaleInput.Value = model.RenderScale;
        Preview.T = 0;
        OnTextureChanged(Path.GetFileName(model.Texture));
        // behavior
        FnXInput.Text = model.FunctionX;
        FnYInput.Text = model.FunctionY;
        LifetimeInput.Value = model.Lifetime;
        TelegraphTimeInput.Value = model.TelegraphTime;
        PersistantCheckbutton.ButtonPressed = model.Persistant;
        FacePlayerCheckbutton.ButtonPressed = model.FacePlayer;
        LockRotationCheck.ButtonPressed = model.LockRotation;
        OnFnXChanged(model.FunctionX);
        OnFnYChanged(model.FunctionY);

        // collision
        Radius.Value = model.Radius;
        UseShapeCheckbutton.ButtonPressed = model.UseShape; // doesnt need its on changed function because setting it like this automatically calls it 
        ShapeEditor.Points = CollisionUtils.FloatArrToVect2s(model.Shape);
        Preview.Shape = CollisionUtils.FloatArrToVect2s(model.Shape);
        CanCollideCheckbutton.ButtonPressed = model.CanCollide;
        ToggleCollisionParams(model.UseShape);
        
        // spawn on death
        SpawnModelOnDeathCheck.ButtonPressed = model.SpawnModelOnDeath;
        DeathModelType.Selected = (int)model.SpawnOnDeathType;
        SpawnOnDeathIdInput.Value = model.SpawnOnDeathId;
        MaxDepthInput.Value = model.MaxDepth;

        OnRenderScaleChanged(model.RenderScale); // this is just to set the preview to be dirty
    }
}
