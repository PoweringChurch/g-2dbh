using Godot;
using System;

public partial class Inspector : Control
{
    [Export] LineEdit XInput;
    [Export] LineEdit YInput;
    [Export] LineEdit ForwardInput;
    [Export] LineEdit SpawnTimeInput;
    [Export] LineEdit ModelIdInput;
    [Export] MessageDisplay ErrorDisplay;
    private Editor e;
    private ISpatialReference _reference;
    public ISpatialReference Reference => _reference;
    public delegate void ReferenceUpdatedEventHandler(ISpatialReference r);
    public event ReferenceUpdatedEventHandler ReferenceUpdated;
    public override void _Ready()
    {
        base._Ready();
        e = GetNode<Editor>("/root/Editor");
        XInput.TextChanged += OnXChanged;
        YInput.TextChanged += OnYChanged;
        ForwardInput.TextChanged += OnForwardChanged;
        SpawnTimeInput.TextChanged += OnSpawnTimeChanged;
        ModelIdInput.TextChanged += OnIdChanged;
    }
    public void SelectReference(ISpatialReference r)
    {
        if (r == null)
        {
            _reference = null;
            XInput.Text = string.Empty;
            YInput.Text = string.Empty;
            ForwardInput.Text = string.Empty;
            SpawnTimeInput.Text = string.Empty;
            ModelIdInput.Text = string.Empty;
            return;
        }
        _reference = r;
        XInput.Text = r.X.ToString();
        YInput.Text = r.Y.ToString();
        ForwardInput.Text = r.Forward.ToString();
        SpawnTimeInput.Text = r.T.ToString();
        ModelIdInput.Text = r.Id.ToString();
    }
    private void OnIdChanged(string newText)
    {
        if (e.ProjectileRegistry.GetModel(newText) != null)
        {
            _reference.Id = newText;
            ReferenceUpdated?.Invoke(e.SelectedReference);
            ErrorDisplay.ClearMessage("ModelId");
        }
        else ErrorDisplay.SetMessage("ModelId", $"[Model Id] Model Id {newText} does not exist.");
    }
    private void OnSpawnTimeChanged(string newText)
    {
        if (float.TryParse(newText, out float t) && t > 0)
        {
            _reference.T = t;
            ReferenceUpdated?.Invoke(e.SelectedReference);
            ErrorDisplay.ClearMessage("SpawnTime");
        }
        else ErrorDisplay.SetMessage("SpawnTime", $"[Spawn Time] Must be a number and greater than 0.");
    }
    private void OnForwardChanged(string newText)
    {
        if (float.TryParse(newText, out float r))
        {
            _reference.Forward = r;
            ReferenceUpdated?.Invoke(e.SelectedReference);
            ErrorDisplay.ClearMessage("Forward");
        }
        else ErrorDisplay.SetMessage("Forward", $"[Forward] Must be a number.");
    }
    private void OnYChanged(string newText)
    {
        if (float.TryParse(newText, out float y))
        {
            _reference.Y = y;
            ReferenceUpdated?.Invoke(e.SelectedReference);
            ErrorDisplay.ClearMessage("Y");
        }
        else ErrorDisplay.SetMessage("Y", $"[Y] Must be a number.");
    }
    private void OnXChanged(string newText)
    {
        if (float.TryParse(newText, out float x))
        {
            _reference.X = x;
            ReferenceUpdated?.Invoke(e.SelectedReference);
            ErrorDisplay.ClearMessage("X");
        }
        else ErrorDisplay.SetMessage("X", $"[X] Must be a number.");
    }
}
