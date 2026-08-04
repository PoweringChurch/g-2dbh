using System;
using Godot;

public partial class Toolbar : Control
{
    public enum Mode { Place, Select, Delete }
    private Mode currentMode = Mode.Select;
    public Mode CurrentMode
    {
        get => currentMode;
        set
        {
            if (value == Mode.Place)
                _modeDisplay.Text = "Mode : Place";
            else if (value == Mode.Select)
                _modeDisplay.Text = "Mode : Select";
            else if (value == Mode.Delete)
                _modeDisplay.Text = "Mode : Delete";
            currentMode = value;
        }
    }
    private bool snap;
    public bool Snap 
    {
        get => snap; 
        set
        {
            snap = value;
            _snapDisplay.Text = value ? "Snap : On" : "Snap : Off";
        }
    }
    private float incrementTimeBy = 1;
    public float IncrementTimeBy
    {
        get => incrementTimeBy;
        set
        {
            incrementTimeBy = Math.Clamp(value, 0.125f, 128);
            _skipInput.SetValueNoSignal(value);
        }
    }
    private bool timeControls;
    public bool TimeControls => timeControls;
    // toolbar
    [Export] private Button _placeButton;
    [Export] private Button _selectButton;
    [Export] private Button _deleteButton;
    [Export] private Label _modeDisplay;
    // snap
    [Export] private Button _snapButton;
    [Export] private Label _snapDisplay;
    // time controls
    [Export] private SpinBox _skipInput;
    Editor e => Editor.Instance;
    public override void _Ready()
    {
        _placeButton.Pressed += () => CurrentMode = Mode.Place;
        _selectButton.Pressed += () => CurrentMode = Mode.Select;
        _deleteButton.Pressed += () => CurrentMode = Mode.Delete;
        _snapButton.Pressed += () => Snap = !Snap;
        
        _skipInput.ValueChanged += v => IncrementTimeBy = (float)v;
    }
    public override void _Input(InputEvent @event)
    {
        if (Editor.CannotUseBinds()) return;
        CurrentMode = @event.IsActionPressed("place_bind") ? Mode.Place :
                  @event.IsActionPressed("select_bind") ? Mode.Select :
                  @event.IsActionPressed("delete_bind") ? Mode.Delete : CurrentMode;
        if (@event.IsActionPressed("snap")) Snap = !Snap;
        if      (@event.IsActionPressed("time_control")) timeControls = true;
        else if (@event.IsActionReleased("time_control")) timeControls = false;
        
        if (e.TimeControls)
        {
            if (@event.IsActionPressed("scroll_up")) IncrementTimeBy *= 2;
            if (@event.IsActionPressed("scroll_down")) IncrementTimeBy /= 2;
        }
    }
}