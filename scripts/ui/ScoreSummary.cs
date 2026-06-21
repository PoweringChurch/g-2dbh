using Godot;
using System;

public partial class ScoreSummary : CanvasLayer
{
    [Export] public Label Score;
    [Export] public Label Graze;
    [Export] public Label HP;
    [Export] public Button ResetButton;
    [Export] public Button QuitButton;

    [Signal] public delegate void ResetRequestedEventHandler();
    [Signal] public delegate void QuitRequestedEventHandler();
    public override void _Ready()
    {
        ResetButton.Pressed += OnResetPressed;
        QuitButton.Pressed += OnQuitPressed;
    }
    public void SetScore(int score) =>
        Score.Text = score.ToString();
    public void SetGraze(int graze) =>
        Graze.Text = graze.ToString();
    public void SetHP(int hp) =>
        HP.Text = hp.ToString();
    public void OnResetPressed() =>
        EmitSignal(SignalName.ResetRequested);
    public void OnQuitPressed() =>
        EmitSignal(SignalName.ResetRequested);
}
