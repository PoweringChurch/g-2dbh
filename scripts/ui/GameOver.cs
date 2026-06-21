// PauseMenu.cs — emits signals upward, doesn't touch game state directly
using Godot;
public partial class GameOver : CanvasLayer
{
    [Signal] public delegate void ResetRequestedEventHandler();
    [Signal] public delegate void QuitRequestedEventHandler();
    // references
    [Export] Button _resetButton;
    [Export] Button _quitButton;
    public override void _Ready()
    {
        _resetButton.Pressed  += OnResetPressed;
        _quitButton.Pressed   += OnQuitPressed;
    }

    void OnResetPressed() => EmitSignal(SignalName.ResetRequested);
    void OnQuitPressed()   => EmitSignal(SignalName.QuitRequested);
}