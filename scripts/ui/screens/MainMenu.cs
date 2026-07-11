using System.Collections.Generic;
using Godot;
public partial class MainMenu : CanvasLayer
{
    [Signal] public delegate void StartRequestedEventHandler();
    [Signal] public delegate void CustomsRequestedEventHandler();
    [Signal] public delegate void SettingsRequestedEventHandler();
    [Signal] public delegate void QuitRequestedEventHandler();
    // references
    [Export] Button _startButton;
    [Export] Button _customsButton;
    [Export] Button _settingsButton;
    [Export] Button _quitButton;
    public override void _Ready()
    {
        _startButton.Pressed += OnStartPressed;
        _customsButton.Pressed += OnCustomsPressed;
        _settingsButton.Pressed  += OnSettingsPressed;
        _quitButton.Pressed   += OnQuitPressed;
    }
    void OnStartPressed() => EmitSignal(SignalName.StartRequested);
    void OnCustomsPressed() => EmitSignal(SignalName.CustomsRequested);
    void OnSettingsPressed() => EmitSignal(SignalName.SettingsRequested);
    void OnQuitPressed()   => EmitSignal(SignalName.QuitRequested);
}