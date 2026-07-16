using Godot;
using System;
[GlobalClass]
public partial class SongData : Resource
{
    [Export] public string SongName { get; set; }
	[Export] public string Author { get; set; }
	[Export] public string StreamPath { get; set; }
}
