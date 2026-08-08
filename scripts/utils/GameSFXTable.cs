using Godot;
using Godot.Collections;

[GlobalClass]
public partial class GameSFXTable : Resource
{
    [Export] public Dictionary<string, string> Paths = [];
}