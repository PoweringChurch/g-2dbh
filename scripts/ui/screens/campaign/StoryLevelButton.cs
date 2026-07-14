using Godot;

public partial class StoryLevelButton : Button
{
    [Export] public string LevelPath;
    [Export] public Vector2 CharacterPosition;
    [Export] public bool PositionAsOffset;
}