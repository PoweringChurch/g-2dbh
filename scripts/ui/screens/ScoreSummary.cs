using Godot;
using System;

public partial class ScoreSummary : CanvasLayer
{
    [Export] public Label Score;
    [Export] public Label Graze;
    [Export] public Label HP;
    [Export] public Button ResetButton;
    [Export] public Button QuitButton;

    public event Action RequestReset;
    public event Action RequestReturn;
    public override void _Ready()
    {
        ResetButton.Pressed += RequestReset.Invoke;
        QuitButton.Pressed += RequestReturn.Invoke;
    }
    public void SetScore(int score) =>
        Score.Text = score.ToString();
    public void SetGraze(int graze) =>
        Graze.Text = graze.ToString();
    public void SetHP(int hp) =>
        HP.Text = hp.ToString();
}
