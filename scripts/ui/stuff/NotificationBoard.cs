using Godot;
using System;

public partial class NotificationBoard : Container
{
    [Export] public LabelSettings[] NotificationLabelSettings;
    public void ShowMessage(object message, int labelSettingsId = 0, float fadeTime = 5)
    {
        var newLabel = new Label() {
            Text = message.ToString(), 
            LabelSettings = NotificationLabelSettings[labelSettingsId],
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.Word,
            CustomMinimumSize = new(40,34),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        AddChild(newLabel);

        Tween tween = newLabel.CreateTween();
        tween.TweenProperty(newLabel, "modulate:a", 0.0f, fadeTime);
        tween.TweenCallback(Callable.From(newLabel.QueueFree));
    }
    public void Clear()
    {
        foreach (var child in GetChildren())
            child.QueueFree();
    }
}
