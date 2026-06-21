using Godot;
using System;
using System.Collections.Generic;

public partial class Timeline : Control
{
    [Export] public HSlider Playhead;
    [Export] public LineEdit PlayheadPositionInput;
    [Export] public Control TimelineBar;
    [Export] public MessageDisplay ErrorDisplay;
    private float currentTime = 0;
    public float CurrentTime => currentTime;
    private Dictionary<ISpatialReference, TimelineMarker> _markers = new();
    private Dictionary<string, bool> _visibleModels = new();
    private Editor e;
    public override void _Ready()
    {
        base._Ready();
        e = GetNode<Editor>("/root/Editor");
        PlayheadPositionInput.TextChanged += OnTimeChange;
        Playhead.ValueChanged +=  OnValueChanged;
    }
    private void OnTimeChange(string text)
    {
        if (float.TryParse(text, out float time) && time > 0) 
        {
            currentTime = time;
            Playhead.SetValueNoSignal(time);
            ErrorDisplay.ClearMessage("Time");
        }
        else ErrorDisplay.SetMessage("Time", "[Time] Must be number and greater than 0.");
    }
    private void OnValueChanged(double to)
    {
        currentTime = (float)to;
        PlayheadPositionInput.Text = $"{currentTime:F2}";
    }
    public void UpdateDuration(float newDuration)
    {
        Playhead.MaxValue = newDuration;
        foreach (var m in _markers)
            m.Value.Refresh(newDuration, Size.X);
    }
    public void Load(List<ProjectileReference> projRefs, List<PatternReference> patternRefs, float duration)
    {
        ClearMarkers();
        foreach (var r in projRefs)
            AddMarker(r);
        foreach (var r in patternRefs)
            AddMarker(r);
        Playhead.MaxValue = duration;
    }
    public void AddMarker(ISpatialReference r)
    {
        var marker = new TimelineMarker();
        TimelineBar.AddChild(marker);
        marker.Init(r, e.levelData.Duration, Size.X);
        _markers[r] = marker;
    }
    public void RemoveMarker(ISpatialReference r)
    {
        if (_markers.TryGetValue(r, out var marker))
        {
            marker.QueueFree();
            _markers.Remove(r);
        }
    }
    public void ClearMarkers()
    {
        foreach (var marker in _markers.Values)
            marker.QueueFree();
        _markers.Clear();
    }
    public void RefreshMarker(ISpatialReference r) =>
        _markers[r].Refresh(e.levelData.Duration, Size.X);
    public void SetModelVisible(string modelId, bool visible)
    {
        _visibleModels[modelId] = visible;
        foreach (var (r, marker) in _markers)
            if (r.Id == modelId)
                marker.Visible = visible;
    }
}