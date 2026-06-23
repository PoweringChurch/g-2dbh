using Godot;
using System;
using System.Collections.Generic;
public partial class Timeline : Control
{
    [Export] public HSlider Playhead;
    [Export] public SpinBox PlayheadPositionInput;
    [Export] public Control TimelineBar;
    [Export] public MessageDisplay ErrorDisplay;
    [Export] public Button PlayPauseButton;
    [Export] public SpinBox SpeedInput;
    [Export] public CheckButton LoopToggle;

    private float currentTime = 0;
    public float CurrentTime => currentTime;
    private bool _playing = false;
    private float _speed = 1f;
    private bool _loop = false;
    private Dictionary<ISpatialReference, TimelineMarker> _markers = new();
    private Dictionary<string, bool> _visibleModels = new();
    private Editor e;
    public override void _Ready()
    {
        base._Ready();
        e = GetNode<Editor>("/root/Editor");
        PlayheadPositionInput.ValueChanged += OnTimeChange;
        Playhead.ValueChanged +=  OnTimeValueChanged;

        PlayPauseButton.Pressed += TogglePlaying;
        SpeedInput.ValueChanged += OnSpeedChanged;
        LoopToggle.Toggled += OnLoopToggled;

        _speed = (float)SpeedInput.Value;
        _loop = LoopToggle.ButtonPressed;
        UpdatePlayPauseLabel();
    }
    public override void _Process(double delta)
    {
        if (!_playing)
            return;

        float duration = (float)Playhead.MaxValue;
        currentTime += (float)delta * _speed;

        if (currentTime >= duration)
        {
            if (_loop)
                currentTime = duration > 0 ? currentTime % duration : 0;
            else
            {
                currentTime = duration;
                SetPlaying(false);
            }
        }
        Playhead.SetValueNoSignal(currentTime);
        PlayheadPositionInput.SetValueNoSignal(currentTime);
    }
    private void TogglePlaying() => SetPlaying(!_playing);
    private void SetPlaying(bool playing)
    {
        _playing = playing;
        UpdatePlayPauseLabel();
    }
    private void UpdatePlayPauseLabel() =>
        PlayPauseButton.Text = _playing ? "❚❚" : "▶";
    private void OnSpeedChanged(double value) =>
        _speed = (float)value;
    private void OnLoopToggled(bool toggled) =>
        _loop = toggled;
    private void OnTimeChange(double time)
    {
        currentTime = (float)time;
        UpdateTime(false);
    }
    private void OnTimeValueChanged(double to)
    {
        currentTime = (float)to;
        UpdateTime(true);
    }
    private void UpdateTime(bool tinput)
    {
        if (tinput)
            PlayheadPositionInput.SetValueNoSignal(currentTime);
        else
            Playhead.SetValueNoSignal(currentTime);
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
        Playhead.MaxValue = duration;
        foreach (var r in projRefs)
            AddMarker(r);
        foreach (var r in patternRefs)
            AddMarker(r);

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