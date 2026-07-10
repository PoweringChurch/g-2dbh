using Godot;
using System;
using System.Linq;

public partial class BackgroundEditor : Control
{
    [Export] Control BackgroundHolder;
    [Export] PackedScene bglayerui;
    [Export] Button Organize;
    [Export] Button NewLayer;
    [Export] Button Close;
    [Export] VBoxContainer List;
    private Editor e;
    public override void _Ready()
    {
        e = GetNode<Editor>("/root/Editor");
        NewLayer.Pressed += () => {var layer = new BackgroundLayer(); e.levelData.BackgroundLayers.Add(layer); CreateNewLayerUi(layer); };
        Organize.Pressed += OrganizeUi;
        Close.Pressed += () => Visible = false;
        BackgroundHolder.Scale = Vector2.One*LevelPreview.PreviewScale;
    }
    private void OrganizeUi()
    {
        foreach (BackgroundLayerUi child in List.GetChildren().Cast<BackgroundLayerUi>())
        {
            List.MoveChild(child, child.EditorLayerInstance.Layer.Order);
        }
    }
    public void Load(LevelData data)
    {
        e.BGInstances.Clear();
        foreach (var child in List.GetChildren())
            child.QueueFree();
        foreach (var layer in data.BackgroundLayers)
        {
            CreateNewLayerUi(layer);
        }
    }
    private BackgroundLayerUi CreateNewLayerUi(BackgroundLayer layer)
    {
        // set up instance
        var sprite = new Sprite2D();
        var instance = new BackgroundLayerInstance { Layer = layer, Sprite = sprite };
        instance.AddChild(sprite);
        instance.ApplyLayerParams(e.LevelPath+"/images/");
        BackgroundHolder.AddChild(instance);
        e.BGInstances.Add(instance);
        // set up layer ui
        var layerUi = bglayerui.Instantiate<BackgroundLayerUi>();
        List.AddChild(layerUi);
        layerUi.EditorLayerInstance = instance;
        layerUi.SetParams(layer);
        return layerUi;
    }
}
