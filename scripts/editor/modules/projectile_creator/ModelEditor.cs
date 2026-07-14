using Godot;
using System;

public partial class ModelEditor : Control
{
    [Export] Button Close;
    public override void _Ready()
    {
        Close.Pressed += () => Visible = false;
    }
    public override void _Input(InputEvent @event)
    {
        if (Editor.CannotUseBinds()) return;
        if (@event.IsActionPressed("model_editor")) Visible = !Visible;
    }

}
