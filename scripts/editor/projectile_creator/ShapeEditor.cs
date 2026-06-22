using Godot;

public partial class ShapeEditor : Control
{
    [Export] public TextureRect TextureDisplay;
    [Export] public VSlider TextureTransparency;
    [Export] public VBoxContainer PointContainer;
    [Export] public Button NewPoint;
    public Editor e;
    private PackedScene pointUi = ResourceLoader.Load<PackedScene>("res://data/scenes/ui/point_ui.tscn");
    private float[][] points = [];
    public float[][] Points => points;
    private Texture2D texture;
    private Vector2 _textureSize = Vector2.One;
    private const float DisplaySize = 120f;
    [Signal] public delegate void ShapeUpdatedEventHandler();
    public override void _Ready()
    {
        e = GetNode<Editor>("/root/Editor");
        TextureTransparency.ValueChanged += OnTransparencyChanged;
        NewPoint.Pressed += OnNewPointPressed;
    }
    public override void _Process(double delta)
    {
        QueueRedraw();
    }
    public void OnTextureUpdate()
    {
        texture = RenderingUtils.LoadTexture(e.LevelPath+"images/", ((ProjectileModel)e.SelectedModel).Texture);
        _textureSize = texture.GetSize();
        TextureDisplay.Texture = texture;
        RefreshPointUIs();
    }

    // converts a model-space point to display-space
    private Vector2 ToDisplay(float x, float y) =>
        new Vector2(x, y) / _textureSize * DisplaySize;

    // converts a display-space point back to model-space
    private Vector2 ToModel(float x, float y) =>
        new Vector2(x, y) / DisplaySize * _textureSize;

    private void OnNewPointPressed()
    {
        var newPoints = new float[points.Length + 1][];
        points.CopyTo(newPoints, 0);
        newPoints[^1] = new float[] { 0f, 0f };
        points = newPoints;
        AddPointUI(points.Length - 1);
        UpdateIndices();
        EmitSignal(SignalName.ShapeUpdated);
    }

    private void AddPointUI(int index)
    {
        var ui      = pointUi.Instantiate<Control>();
        var label   = ui.GetNode<Label>("Label");
        var xInput  = ui.GetNode<LineEdit>("X");
        var yInput  = ui.GetNode<LineEdit>("Y");
        var delete  = ui.GetNode<Button>("Delete");

        label.Text  = index.ToString();
        xInput.Text = points[index][0].ToString("F1");
        yInput.Text = points[index][1].ToString("F1");

        xInput.TextSubmitted += (text) =>
        {
            if (float.TryParse(text, out float val))
                points[index][0] = val;
            else
                xInput.Text = points[index][0].ToString("F1");
        };

        yInput.TextSubmitted += (text) =>
        {
            if (float.TryParse(text, out float val))
                points[index][1] = val;
            else
                yInput.Text = points[index][1].ToString("F1");
        };

        delete.Pressed += () =>
        {
            var list = new System.Collections.Generic.List<float[]>(points);
            list.RemoveAt(index);
            points = list.ToArray();
            ui.QueueFree();
            UpdateIndices();
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
    private void OnTransparencyChanged(double value)
    {
        var color = TextureDisplay.Modulate;
        TextureDisplay.Modulate = new Color(color.R, color.G, color.B, (float)value);
    }
    public override void _Draw()
    {
        if (points.Length == 0) return;

        var displayPoints = new System.Collections.Generic.List<Vector2>();
        var origin = TextureDisplay.GlobalPosition - GlobalPosition;

        foreach (var p in points)
        {
            var dp = ToDisplay(p[0], p[1]);
            displayPoints.Add(origin + dp);
        }

        var arr = displayPoints.ToArray();
        DrawPolyline(arr, Colors.Yellow, 1.5f, true);
        if (arr.Length > 1)
            DrawLine(arr[^1], arr[0], Colors.Yellow, 1.5f);

        foreach (var pt in arr)
            DrawCircle(pt, 3f, Colors.Green);
    }
}