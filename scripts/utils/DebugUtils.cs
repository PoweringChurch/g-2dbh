using Godot;
using System;
public partial class DebugUtils : Node
{
    public static DebugUtils Instance;
    [Export] public MeshInstance2D meshinstance;
    public override void _Ready()
    {
        Instance = this;
    }

}