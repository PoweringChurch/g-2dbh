using Godot;
using System;

public partial class HUD : CanvasLayer
{
    [Export] Label LevelName;
    [Export] Label Health;
    [Export] Label Graze;
    [Export] Label Completion;
    [Export] Label Elapsed;

    [Export] TextureRect faster;
    [Export] TextureRect slower;
    [Export] TextureRect hearty;
    [Export] TextureRect perfect;
    [Export] TextureRect paranoid;
    public void SetGraze(int graze) => Graze.Text = graze.ToString();
    public void SetHealth(int lives) => Health.Text = lives.ToString();
    public void SetLevelName(string name)
    {
        LevelName.Text = name;
    }
    public void SetCompletion(float completion) => Completion.Text = $"{completion:P2}";
    public void SetElapsed(float elapsed)
    {
        TimeSpan ts = TimeSpan.FromSeconds(elapsed);
        Elapsed.Text = $"{(int)ts.TotalMinutes:00}:{ts.Seconds:00}.{ts.Milliseconds/10}";
    }
    public void SetMods(StartParams startParams)
    {
        faster.Visible = startParams.Faster;
        slower.Visible = startParams.Slower;
        hearty.Visible = startParams.Healthy;
        perfect.Visible = startParams.Perfectionist;
        paranoid.Visible = startParams.Paranoid;
    }
    public void ResetStats()
    {
        Completion.Text = "00.00%";
        Elapsed.Text = "00:00.00";
        Graze.Text = "0";
        Health.Text = "0";
    }
}