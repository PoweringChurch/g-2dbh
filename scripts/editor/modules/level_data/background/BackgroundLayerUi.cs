using System;
using System.Collections.Generic;
using System.IO;
using Godot;

public partial class BackgroundLayerUi : Control
{
    private static readonly EvalContext testCtx = new() {T = 1};
    [Export] LineEdit layerName;
    [Export] OptionButton imageSelect;
    [Export] SpinBox repeatCount;
    [Export] SpinBox order;
    [Export] SpinBox scale;
    [Export] LineEdit scrollFnX;
    [Export] LineEdit scrollFnY;
    [Export] LineEdit transparencyFn;
    [Export] MessageDisplay ErrorDisplay;
    [Export] Button remove;
    public BackgroundLayerInstance EditorLayerInstance;
    private Editor e => Editor.Instance;
    private string[] backgrounds;
    public override void _Ready()
    {
        var bgList = new List<string>() {"none"};
        var dir = DirAccess.Open("res://data/default-assets/images/backgrounds/");
        var files = dir.GetFiles();
        for (int i = 0; i < files.Length; i++)
        {
            var file = files[i];
            if (file.EndsWith(".import") || file.StartsWith('.')) continue;
            var fileName = Path.GetFileNameWithoutExtension(file);
            bgList.Add("res://data/default-assets/images/backgrounds/"+fileName);
            imageSelect.AddItem(fileName);
        }
        backgrounds = [.. bgList];
        dir.ListDirEnd();
        imageSelect.ItemSelected += OnImageChanged;

        layerName.TextChanged += OnNameChanged;
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
        scrollFnX.Text = layer.ScrollFunctionX;
        scrollFnY.Text = layer.ScrollFunctionY;
        transparencyFn.Text = layer.TransparencyFn;
        repeatCount.Value = layer.RepeatCount;
        order.Value = layer.Order;
        scale.Value = layer.Scale;

        int found = 0;
        for (int i = 0; i < backgrounds.Length; i++)
        {
            if (backgrounds[i] == layer.Image)
            {
                found = i;
                break;
            }
        }
        imageSelect.Select(found);
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
            EditorLayerInstance.ApplyLayerParams();
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
            EditorLayerInstance.ApplyLayerParams();
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
            EditorLayerInstance.ApplyLayerParams();
            ErrorDisplay.ClearMessage("FnY");
        }
        catch (Exception e) { ErrorDisplay.SetMessage("FnY", "[Scroll Y] " + e.Message); }
    }
    private void OnImageChanged(long idx)
    {
        EditorLayerInstance.Layer.Image = backgrounds[idx];
        EditorLayerInstance.ApplyLayerParams();
    }
    private void OnOrderChanged(double to)
    {
        EditorLayerInstance.Layer.Order = (int)to;
        EditorLayerInstance.ApplyLayerParams();
    }
    private void OnRepeatChanged(double to)
    {
        EditorLayerInstance.Layer.RepeatCount = (int)to;
        EditorLayerInstance.ApplyLayerParams();
    }
    private void OnScaleChanged(double to)
    {
        EditorLayerInstance.Layer.Scale = (float)to;
        EditorLayerInstance.ApplyLayerParams();
    }
}
