using System;
using Godot;
public partial class PauseMenu : CanvasLayer
{
    public event Action RequestResume;
    public event Action RequestReset;
    public event Action RequestReturn;
    // references
    [Export] Button _resumeButton;
    [Export] Button _resetButton;
    [Export] Button _quitButton;
    public override void _Ready()
    {
        _resumeButton.Pressed += RequestResume.Invoke;
        _resetButton.Pressed += RequestReset.Invoke;
        _quitButton.Pressed += RequestReturn.Invoke;
    }
    public void ToggleReset(bool on)
    {
        _resetButton.Visible = on;
    }
}