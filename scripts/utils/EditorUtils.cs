using Godot;
using System;
public partial class EditorUtils : Node
{
    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("build_sprite_atlas"))
        {
            RenderingUtils.BuildProjectileAtlas();
        }
    }

}