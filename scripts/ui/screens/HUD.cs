using Godot;
using System;

public partial class HUD : CanvasLayer
{
    [Export] Label _scoreLabel;
    [Export] Label _grazeLabel;
    [Export] Label _LevelName;
    [Export] Label _HP;
    [Export] Label _Duration;
    [Export] Label _Completion;

    public void SetScore(int score) 
    {
        _scoreLabel.Text = score.ToString().PadLeft(6, '0');
    }
    public void SetGraze(int graze) => _grazeLabel.Text = graze.ToString();
    public void SetHealth(int lives) => _HP.Text = lives.ToString();
    public void SetLevelName(string name)
    {
        _LevelName.Text = name;
    }
    public void SetCompletion(float completion) => _Completion.Text = $"{completion:P2}";
    public void SetDuration(float duration)
    {
        TimeSpan ts = TimeSpan.FromSeconds(duration);
        _Duration.Text = $"{(int)ts.TotalMinutes:00}:{ts.Seconds:00}";
    }
    public void ResetStats()
    {
        _scoreLabel.Text = "0";
        _grazeLabel.Text = "0";
        _HP.Text = "0";
    }
}