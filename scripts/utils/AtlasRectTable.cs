using Godot;
using Godot.Collections;

[GlobalClass]
public partial class AtlasRectTable : Resource
{
    [Export] public Dictionary<int, Rect2> Rects = [];
}