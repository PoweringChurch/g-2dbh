using Godot;
using System;
public partial class StoryInfoUi : Control
{
    [Export] public StoryId Id;
    [Export] public StoryLevelButton[] LevelButtons;
    [Export] public CampaignCharacterDisplay CharacterDisplay;
    public event Action<CampaignLevel> LevelSelected;
    public override void _Ready()
    {
        for (int i = 0; i < LevelButtons.Length; i++)
        {
            int cached = i;
            LevelButtons[i].Pressed += () => PressButton(cached);
        }
    }
    public void PressButton(int idx, bool snap = false)
    {
        var btn = LevelButtons[idx];
        var level = Stories.StoryInfoById[Id].Levels[btn.LevelIdx];
        LevelSelected?.Invoke(level);
        var pos = btn.PositionAsOffset ? btn.Position+btn.CharacterPosition
            : btn.CharacterPosition;
        if (snap)
            CharacterDisplay.SnapTo(pos);
        else
            CharacterDisplay.MoveTo(pos);
    }
}