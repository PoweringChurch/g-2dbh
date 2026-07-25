using System;
using System.Collections.Generic;
using Godot;

public partial class ShapeEditor : Control
{
    [Export] public VBoxContainer PointContainer;
    [Export] private Button NewPoint;
    [Export] private SpinBox XTranslate;
    [Export] private SpinBox YTranslate;
    [Export] private SpinBox ScaleTranslate;
    [Export] private SpinBox ForwardTranslate; // Radians
    [Export] private Button ApplyTranslation;
    [Export] private PackedScene pointUi;

    public event Action<List<Vector2>> ShapeUpdated;

    private readonly List<Vector2> _points = new();
    private readonly List<PointRow> _rows = new();
    private bool _updatePending;

    public override void _Ready()
    {
        NewPoint.Pressed += OnNewPointPressed;
        ApplyTranslation.Pressed += OnApplyTranslationPressed;
    }

    public void Load(List<Vector2> shape)
    {
        Clear();

        if (shape != null)
        {
            foreach (var pt in shape)
                AddPointInternal(pt);
        }

        Relabel();
        RequestUpdate();
    }
    public void Clear()
    {
        foreach (var row in _rows)
            row.Destroy(PointContainer);
        _rows.Clear();
        _points.Clear();
    }
    private void OnNewPointPressed() => AddPoint(Vector2.Zero);
    private void AddPoint(Vector2 position)
    {
        AddPointInternal(position);
        Relabel();
        RequestUpdate();
    }

    private void AddPointInternal(Vector2 position)
    {
        _points.Add(position);

        var row = new PointRow(pointUi, position, OnRowXChanged, OnRowYChanged, OnRowDeletePressed);
        _rows.Add(row);
        PointContainer.AddChild(row.Root);
    }

    private void OnRowXChanged(PointRow row, float value) => SetPoint(row, x: value, y: null);
    private void OnRowYChanged(PointRow row, float value) => SetPoint(row, x: null, y: value);

    private void SetPoint(PointRow row, float? x, float? y)
    {
        int index = _rows.IndexOf(row);
        if (index < 0) return;

        Vector2 p = _points[index];
        if (x.HasValue) p.X = x.Value;
        if (y.HasValue) p.Y = y.Value;
        _points[index] = p;

        RequestUpdate();
    }

    private void OnRowDeletePressed(PointRow row)
    {
        int index = _rows.IndexOf(row);
        if (index < 0) return;

        _points.RemoveAt(index);
        _rows.RemoveAt(index);
        row.Destroy(PointContainer);

        Relabel();
        RequestUpdate();
    }

    private void OnApplyTranslationPressed()
    {
        float dx = (float)XTranslate.Value;
        float dy = (float)YTranslate.Value;
        float scale = (float)ScaleTranslate.Value;
        float angle = (float)ForwardTranslate.Value;

        float cos = MathF.Cos(angle);
        float sin = MathF.Sin(angle);

        for (int i = 0; i < _points.Count; i++)
        {
            Vector2 scaled = _points[i] * scale;
            Vector2 rotated = new(
                scaled.X * cos - scaled.Y * sin,
                scaled.X * sin + scaled.Y * cos
            );
            _points[i] = rotated + new Vector2(dx, dy);
        }

        for (int i = 0; i < _rows.Count; i++)
            _rows[i].SetValuesNoSignal(_points[i]);

        RequestUpdate();
    }

    private void Relabel()
    {
        for (int i = 0; i < _rows.Count; i++)
            _rows[i].SetIndex(i);
    }
    private void RequestUpdate()
    {
        ShapeUpdated?.Invoke([.. _points]);
    }
    private sealed class PointRow
    {
        public readonly Control Root;
        private readonly Label _indexLabel;
        private readonly SpinBox _xInput;
        private readonly SpinBox _yInput;

        public PointRow(
            PackedScene scene,
            Vector2 initial,
            Action<PointRow, float> onXChanged,
            Action<PointRow, float> onYChanged,
            Action<PointRow> onDeletePressed)
        {
            Root = scene.Instantiate<Control>();
            _indexLabel = Root.GetNode<Label>("Label");
            _xInput = Root.GetNode<SpinBox>("X");
            _yInput = Root.GetNode<SpinBox>("Y");
            var delete = Root.GetNode<Button>("Delete");

            _xInput.Value = initial.X;
            _yInput.Value = initial.Y;

            _xInput.ValueChanged += value => onXChanged(this, (float)value);
            _yInput.ValueChanged += value => onYChanged(this, (float)value);
            delete.Pressed += () => onDeletePressed(this);
        }

        public void SetIndex(int index) => _indexLabel.Text = index.ToString();

        public void SetValuesNoSignal(Vector2 value)
        {
            _xInput.SetValueNoSignal(value.X);
            _yInput.SetValueNoSignal(value.Y);
        }

        public void Destroy(Node parent)
        {
            parent.RemoveChild(Root);
            Root.QueueFree();
        }
    }
}