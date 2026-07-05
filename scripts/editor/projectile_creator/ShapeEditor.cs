using Godot;

public partial class ShapeEditor : Control
{
    [Export] public Control PointDisplay;
    [Export] public VBoxContainer PointContainer;
    [Export] public Button NewPoint;
    public float TextureRenderScale = 1;
    public Editor e;
    private PackedScene pointUi = ResourceLoader.Load<PackedScene>("res://data/scenes/ui/point_ui.tscn");
    private float[][] points = [];
    public float[][] Points => points;
    private float _pointScale = 1;
    private bool _dirty = false;
    [Signal] public delegate void ShapeUpdatedEventHandler();
    public override void _Ready()
    {
        e = GetNode<Editor>("/root/Editor");
        NewPoint.Pressed += OnNewPointPressed;
        PointDisplay.Draw += DrawPoints;
    }
    public override void _Process(double delta)
    {
        if (_dirty)
        {
            PointDisplay.QueueRedraw();
            _dirty = false;
        }
    }
    private void OnNewPointPressed()
    {
        var newPoints = new float[points.Length + 1][];
        points.CopyTo(newPoints, 0);
        newPoints[^1] = [0f, 0f];
        points = newPoints;
        AddPointUI(points.Length - 1);
        UpdateIndices();
        EmitSignal(SignalName.ShapeUpdated);
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
            points[currentIndex][0] = (float)value;
            _dirty = true;
        };

        yInput.ValueChanged += (value) =>
        {
            int currentIndex = ui.GetIndex()-1;
            points[currentIndex][1] = (float)value;
            _dirty = true;
        };

        delete.Pressed += () =>
        {
            int currentIndex = ui.GetIndex()-1;
            var list = new System.Collections.Generic.List<float[]>(points);
            list.RemoveAt(currentIndex);
            points = [.. list];
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
            child.QueueFree();
        for (int i = 0; i < points.Length; i++)
            AddPointUI(i);
        UpdateIndices();
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
        if (points.Length == 0) return;

        var displayPoints = new System.Collections.Generic.List<Vector2>();
        var origin = PointDisplay.GlobalPosition - GlobalPosition;

        foreach (var p in points)
        {
            displayPoints.Add(origin + new Vector2(p[0], p[1])+new Vector2(75,75));
        }

        var arr = displayPoints.ToArray();
        if (arr.Length == 1) return;
        PointDisplay.DrawPolyline(arr, Colors.Yellow, 1.5f, true);
        if (arr.Length > 1)
            PointDisplay.DrawLine(arr[^1], arr[0], Colors.Yellow, 1.5f);
        foreach (var pt in arr)
            PointDisplay.DrawCircle(pt, 3f, Colors.Green);
    }
}