using System;
using System.Collections.Generic;
using System.Drawing;
using Godot;

public partial class ShapeEditor : Control
{
    [Export] public Control PointDisplay;
    [Export] public VBoxContainer PointContainer;
    [Export] public Button NewPoint;
    [Export] public SpinBox XTranslate;
    [Export] public SpinBox YTranslate;
    [Export] public SpinBox ScaleTranslate;
    [Export] public SpinBox ForwardTranslate;
    [Export] public Button ApplyTranslation;
    public float TextureRenderScale = 1;
    public Editor e;
    [Export] PackedScene pointUi;
    private List<Vector2> points = [];
    public List<Vector2> Points
    {
        get => points;
        set
        {
            points = value;
            RefreshPointUIs();
        }   
    }
    private float _pointScale = 1;
    private bool _dirty = false;
    [Signal] public delegate void ShapeUpdatedEventHandler();
    public override void _Ready()
    {
        e = GetNode<Editor>("/root/Editor");
        NewPoint.Pressed += OnNewPointPressed;
        PointDisplay.Draw += DrawPoints;
        ApplyTranslation.Pressed += OnApplyPressed;
    }
    private void OnApplyPressed()
    {
        float xtranslate = (float)XTranslate.Value;
        float ytranslate = (float)YTranslate.Value; 
        float scaletranslate = (float)ScaleTranslate.Value;
        float fwdTranslate = (float)ForwardTranslate.Value; // Radians
        float cos = MathF.Cos(fwdTranslate);
        float sin = MathF.Sin(fwdTranslate);
        for (int i = 0; i < points.Count; i++)
        {
            Vector2 originalPoint = points[i];
            // scale
            float scaledX = originalPoint.X * scaletranslate;
            float scaledY = originalPoint.Y * scaletranslate;

            // rotate
            float rotatedX = scaledX * cos - scaledY * sin;
            float rotatedY = scaledX * sin + scaledY * cos;

            // translate
            float finalX = rotatedX + xtranslate;
            float finalY = rotatedY + ytranslate;

            points[i] = new Vector2(finalX, finalY);
        }
        RefreshPointUIs();
        _dirty = true;
    }
    public override void _Process(double delta)
    {
        if (_dirty)
        {
            PointDisplay.QueueRedraw();
            EmitSignal(SignalName.ShapeUpdated);
            _dirty = false;
        }
    }
    private void OnNewPointPressed()
    {
        points.Add(new());
        AddPointUI(points.Count - 1);
        UpdateIndices();
        _dirty = true;
    }
    private void AddPointUI(int index)
    {
        var ui      = pointUi.Instantiate<Control>();
        var label   = ui.GetNode<Label>("Label");
        var xInput  = ui.GetNode<SpinBox>("X");
        var yInput  = ui.GetNode<SpinBox>("Y");
        var delete  = ui.GetNode<Button>("Delete");

        label.Text  = index.ToString();
        xInput.Value = points[index][0];
        yInput.Value = points[index][1];

        xInput.ValueChanged += (value) =>
        {
            int currentIndex = ui.GetIndex()-1;
            points[currentIndex] = new Vector2((float)value, points[currentIndex].Y);
            _dirty = true;
        };

        yInput.ValueChanged += (value) =>
        {
            int currentIndex = ui.GetIndex()-1;
            points[currentIndex] = new Vector2(points[currentIndex].X, (float)value);
            _dirty = true;
        };

        delete.Pressed += () =>
        {
            int currentIndex = ui.GetIndex()-1;
            points.RemoveAt(currentIndex);
            PointContainer.RemoveChild(ui);
            ui.QueueFree();
            UpdateIndices();
            _dirty = true;
        };

        PointContainer.AddChild(ui);
    }
    private void RefreshPointUIs()
    {
        foreach (var child in PointContainer.GetChildren())
        {
            if (child == NewPoint)
                continue;
            child.QueueFree();
        }
            
        for (int i = 0; i < points.Count; i++)
            AddPointUI(i);
        UpdateIndices();
        _dirty = true;
    }
    private void UpdateIndices()
    {
        int i = 0;
        foreach (var child in PointContainer.GetChildren())
        {
            if (child == NewPoint) continue;
            child.GetNode<Label>("Label").Text = i.ToString();
            i++;
        }
    }
    public void DrawPoints()
    {
        if (points.Count == 0 || points.Count == 1) return;
        PointDisplay.DrawPolyline([.. points, points[0]], Colors.Yellow, 1.5f, true);
        foreach (var pt in points)
            PointDisplay.DrawCircle(pt, 3f, Colors.Green);
    }
}