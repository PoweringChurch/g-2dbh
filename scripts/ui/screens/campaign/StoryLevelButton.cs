using Godot;

public partial class StoryLevelButton : Button
{
    [Export] public int LevelIdx;
    [Export] public Vector2 CharacterPosition;
    [Export] public bool PositionAsOffset;
}