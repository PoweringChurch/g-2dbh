using Godot;
using Godot.Collections;

[GlobalClass]
public partial class AtlasRectTable : Resource
{
    [Export] public Dictionary<string, Rect2> Rects = [];
}