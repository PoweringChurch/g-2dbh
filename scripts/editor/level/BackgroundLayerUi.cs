using System;
using Godot;

public partial class BackgroundLayerUi : Control
{
    private static readonly EvalContext testCtx = new() {T = 1};
    [Export] LineEdit layerName;
    [Export] LineEdit imageName;
    [Export] SpinBox repeatCount;
    [Export] SpinBox order;
    [Export] SpinBox scale;
    [Export] LineEdit scrollFnX;
    [Export] LineEdit scrollFnY;
    [Export] LineEdit transparencyFn;
    [Export] MessageDisplay ErrorDisplay;
    [Export] Button remove;
    public BackgroundLayerInstance EditorLayerInstance;
    private Editor e;
    public override void _Ready()
    {
        e = GetNode<Editor>("/root/Editor");
        layerName.TextChanged += OnNameChanged;
        imageName.TextChanged += OnImageChanged;
        scrollFnX.TextChanged += OnFnXChanged;
        scrollFnY.TextChanged += OnFnyChanged;
        transparencyFn.TextChanged += OnFnTransparencyChanged;
        repeatCount.ValueChanged += OnRepeatChanged;
        order.ValueChanged += OnOrderChanged;
        scale.ValueChanged += OnScaleChanged;
        remove.Pressed += OnRemovePressed;
    }
    private void OnNameChanged(string newText)
    {
        EditorLayerInstance.Layer.Name = newText;
    }
    public void SetParams(BackgroundLayer layer)
    {
        EditorLayerInstance.Layer = layer;
        layerName.Text = layer.Name;
        imageName.Text = layer.ImageName;
        scrollFnX.Text = layer.ScrollFunctionX;
        scrollFnY.Text = layer.ScrollFunctionY;
        transparencyFn.Text = layer.TransparencyFn;
        repeatCount.Value = layer.RepeatCount;
        order.Value = layer.Order;
        scale.Value = layer.Scale;
    }
    private void OnRemovePressed()
    {
        e.BGInstances.Remove(EditorLayerInstance);
        e.levelData.BackgroundLayers.Remove(EditorLayerInstance.Layer);
        EditorLayerInstance.QueueFree();
        QueueFree();
    }
    private void OnFnTransparencyChanged(string newText)
    {
        try
        {
            var fn = ExpressionHandler.Parse(newText);
            fn.Eval(testCtx);
            EditorLayerInstance.Layer.TransparencyFn = newText;
            EditorLayerInstance.ApplyLayerParams(e.LevelPath+"/images/");
            ErrorDisplay.ClearMessage("FnT");
        }
        catch (Exception e) { ErrorDisplay.SetMessage("FnT", "[Transparency] " + e.Message); }
    }
    private void OnFnXChanged(string text)
    {
        try
        {
            var fn = ExpressionHandler.Parse(text);
            fn.Eval(testCtx);
            EditorLayerInstance.Layer.ScrollFunctionX = text;
            EditorLayerInstance.ApplyLayerParams(e.LevelPath+"/images/");
            ErrorDisplay.ClearMessage("FnX");
        }
        catch (Exception e) { ErrorDisplay.SetMessage("FnX", "[Scroll X] " + e.Message); }
    }
    private void OnFnyChanged(string text)
    {
        try
        {
            var fn = ExpressionHandler.Parse(text);
            fn.Eval(testCtx);
            EditorLayerInstance.Layer.ScrollFunctionY = text;
            EditorLayerInstance.ApplyLayerParams(e.LevelPath+"/images/");
            ErrorDisplay.ClearMessage("FnY");
        }
        catch (Exception e) { ErrorDisplay.SetMessage("FnY", "[Scroll Y] " + e.Message); }
    }
    private void OnImageChanged(string text)
    {
        var tex = RenderingUtils.LoadTexture(e.LevelPath+"/images/", text);
        if (tex == null)
        {
            ErrorDisplay.SetMessage("Texture", $"[Image] Image of name {text} could not be found");
            return;
        }
        ErrorDisplay.ClearMessage("Texture");
        EditorLayerInstance.Layer.ImageName = text;
        EditorLayerInstance.ApplyLayerParams(e.LevelPath+"/images/");
    }
    private void OnOrderChanged(double to)
    {
        EditorLayerInstance.Layer.Order = (int)to;
        EditorLayerInstance.ApplyLayerParams(e.LevelPath+"/images/");
    }
    private void OnRepeatChanged(double to)
    {
        EditorLayerInstance.Layer.RepeatCount = (int)to;
        EditorLayerInstance.ApplyLayerParams(e.LevelPath+"/images/");
    }
    private void OnScaleChanged(double to)
    {
        EditorLayerInstance.Layer.Scale = (float)to;
        EditorLayerInstance.ApplyLayerParams(e.LevelPath+"/images/");
    }
}
