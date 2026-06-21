using Godot;
public partial class PauseMenu : CanvasLayer
{
    [Signal] public delegate void ResumeRequestedEventHandler();
    [Signal] public delegate void ResetRequestedEventHandler();
    [Signal] public delegate void QuitRequestedEventHandler();
    // references
    [Export] Button _resumeButton;
    [Export] Button _resetButton;
    [Export] Button _quitButton;
    public override void _Ready()
    {
        _resumeButton.Pressed += OnResumePressed;
        _resetButton.Pressed  += OnResetPressed;
        _quitButton.Pressed   += OnQuitPressed;
    }

    void OnResumePressed() => EmitSignal(SignalName.ResumeRequested);
    void OnResetPressed() => EmitSignal(SignalName.ResetRequested);
    void OnQuitPressed()   => EmitSignal(SignalName.QuitRequested);
}