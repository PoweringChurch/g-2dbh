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
    [Export] public AudioStreamPlayer EditorAudioPreview;

    public float CurrentTime
    {
        get => currentTime;
        set
        {
            currentTime = Math.Clamp(value, 0, e.levelData.Duration);
        }
    }
    private float currentTime = 0;
    private bool _playing = false;
    private float _speed = 1f;
    private bool _loop = false;
    private bool _dirty = false;
    private Dictionary<EditorReference, TimelineMarker> _markers = new();
    private Dictionary<string, bool> _visibleModels = new();
    private Editor e => Editor.Instance;
    public override void _Ready()
    {
        PlayheadPositionInput.ValueChanged += OnTimeChange;
        Playhead.ValueChanged +=  OnTimeValueChanged;

        PlayPauseButton.Pressed += TogglePlaying;
        SpeedInput.ValueChanged += OnSpeedChanged;
        LoopToggle.Toggled += OnLoopToggled;

        _speed = (float)SpeedInput.Value;
        _loop = LoopToggle.ButtonPressed;
        UpdatePlayPauseLabel();
    }
    public override void _Input(InputEvent @event)
    {
        if (Editor.CannotUseBinds()) return;
        if (@event.IsActionPressed("playback_toggle")) TogglePlaying();
        if (@event.IsActionPressed("skip_forward"))
        {
            SetTime(e.TimeControls ? GetNextReferenceTime() : CurrentTime + e.IncrementTimeBy);
        }
        else if (@event.IsActionPressed("skip_backward"))
        {
            SetTime(e.TimeControls ? GetPrevReferenceTime() : CurrentTime - e.IncrementTimeBy);
        }
    }
    public override void _Process(double delta)
    {
        if (_dirty) // the dirty flag is necessary
        {
            Playhead.MaxValue = e.levelData.Duration;
            PlayheadPositionInput.MaxValue = e.levelData.Duration;
            foreach (var m in _markers)
                m.Value.Refresh(e.levelData.Duration, new Vector2(1920, 40));
            _dirty = false;
        }
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
        e.SyncPreview();
    }
    private float GetNextReferenceTime()
    {
        float closestTime = e.levelData.Duration;
        for (int i = 0; i < e.levelData.References.Count; i++)
        {
            var r = e.levelData.References[i];
            if (r.T > CurrentTime && r.T < closestTime) closestTime = (float)r.T;
        }
        return closestTime;
    }
    private float GetPrevReferenceTime()
    {
        float closestTime = 0;
        for (int i = 0; i < e.levelData.References.Count; i++)
        {
            var r = e.levelData.References[i];
            if (r.T < CurrentTime && r.T > closestTime) closestTime = (float)r.T;
        }
        return closestTime;
    }
    public void SetTime(float to, bool sync = true)
    {
        currentTime = to;
        Playhead.SetValueNoSignal(currentTime);
        PlayheadPositionInput.SetValueNoSignal(currentTime);
        if (_playing)
            EditorAudioPreview.Play(currentTime);
        if (sync)
            e.SyncPreview();
    }
    public void TogglePlaying() => SetPlaying(!_playing);
    public void SetPlaying(bool playing)
    {
        if (EditorAudioPreview.Stream != null)
        {
            if (playing)
            {
                EditorAudioPreview.VolumeLinear = AudioUtils.MusicVolume;
                EditorAudioPreview.Play(currentTime);
            }
            else
            {
                EditorAudioPreview.Stop();
            }
        }
        _playing = playing;
        UpdatePlayPauseLabel();
    }
    public void UpdateMusic(AudioStream newMusic)
    {
        SetPlaying(false);
        EditorAudioPreview.Stream = newMusic;
    }
    private void UpdatePlayPauseLabel() =>
        PlayPauseButton.Text = _playing ? "❚❚" : "▶";
    private void OnSpeedChanged(double value)
    {
        _speed = (float)value;
        EditorAudioPreview.PitchScale = (float)value;
    }
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
        if (_playing)
            EditorAudioPreview.Play(currentTime);
        e.SyncPreview();
    }

    public void UpdateDuration()
    {
        _dirty = true;
    }
    public void Load(LevelData data)
    {
        ClearMarkers();
        foreach (var r in data.References)
            AddMarker(r);
        var musicname = data.Music;
        var found = AudioUtils.LoadAudio($"{e.LevelPath}/audio/{musicname}");
        EditorAudioPreview.Stream = found;
        SetTime(0, false);
    }
    public void AddMarker(EditorReference r)
    {
        var marker = new TimelineMarker();
        TimelineBar.AddChild(marker);
        marker.Init(r, e.levelData.Duration, new Vector2(1920, 40));
        _markers[r] = marker;
    }
    public void RemoveMarker(EditorReference r)
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
    public void RefreshMarker(EditorReference r)
    {
        if (r == null) return;
        _markers[r].Refresh(e.levelData.Duration, new Vector2(1920, 40));
    }
}