using Godot;
using System;

public partial class GameAudioPlayer : AudioStreamPlayer
{
    bool waiting = false;
    float delaySeconds = 0;
    public override void _Process(double delta)
    {
        if (!waiting) return;
        delaySeconds -= (float)delta;
        if (delaySeconds <= 0)
        {
            Play(0);
            waiting = false;
        }
    }
    public void PlayAfterDelay(float delay)
    {
        waiting = true;
        delaySeconds = delay;
    }
}