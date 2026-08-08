using Godot;
using System;
public partial class StoryInfoUi : Control
{
    [Export] public Button SelectionButton;
    [Export] public StoryId Id;
    [Export] public StoryLevelButton[] LevelButtons;
    [Export] public CampaignCharacterDisplay CharacterDisplay;
    public event Action<Stories.CampaignLevel, Vector2> LevelSelected;
    public override void _Ready()
    {
        for (int i = 0; i < LevelButtons.Length; i++)
        {
            int cached = i;
            LevelButtons[i].Pressed += () => PressButton(cached);
        }
    }
    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
            LevelSelected?.Invoke(null, Vector2.Zero);
    }

    public void PressButton(int idx, bool snap = false)
    {
        var btn = LevelButtons[idx];
        var level = Stories.StoryInfoById[Id][btn.LevelIdx];
        var pos = btn.PositionAsOffset ? btn.Position+btn.CharacterPosition
            : btn.CharacterPosition;
        var brCorner = btn.Position+btn.Size;
        AudioUtils.PlayAudio("res://data/sounds/ui/button_pressed.wav", AudioUtils.SFXVolume);
        LevelSelected?.Invoke(level, brCorner);
        if (snap)
            CharacterDisplay.SnapTo(pos);
        else
            CharacterDisplay.MoveTo(pos);
    }
}