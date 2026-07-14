using Godot;
using System;

public partial class Modifiers : Control
{
    [Export] CheckBox faster;
    [Export] CheckBox slower;
    [Export] CheckBox healthy;
    [Export] CheckBox perfectionist;
    [Export] CheckBox paranoid;

    public override void _Ready()
    {
        faster.Toggled += (on) => { if (on) slower.ButtonPressed = false; };
        slower.Toggled += (on) => { if (on) faster.ButtonPressed = false; };

        healthy.Toggled += (on) => { if (on) perfectionist.ButtonPressed = false; };
        perfectionist.Toggled += (on) => { if (on) healthy.ButtonPressed = false; };
    }
    public StartParams GetStartParams()
    {
        var param = new StartParams
        {
            Faster = faster.ButtonPressed,
            Slower = slower.ButtonPressed,
            Healthy = healthy.ButtonPressed,
            Perfectionist = perfectionist.ButtonPressed,
            Paranoid = paranoid.ButtonPressed
        };
        return param;
    }
}
