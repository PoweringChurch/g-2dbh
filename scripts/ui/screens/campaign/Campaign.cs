using Godot;
using System;
using System.Collections.Generic;

public partial class Campaign : CanvasLayer
{
    public event Action RequestReturn;
    [Export] StoryInfoUi[] StoryInfos;
    [Export] Button Return;
    [Export] CampaignLevelDisplay levelDisplay;
    private int currentStory = 0;
    public override void _Ready()
    {
        for (int i = 0; i < StoryInfos.Length; i++)
        {
            var story = StoryInfos[i];
            if (story != null)
            {
                story.PressButton(0);
                int cached = i;
                story.SelectionButton.ToggleMode = true;
                story.SelectionButton.Pressed += () => ShowStoryInfo(cached);
                story.LevelSelected += OnLevelSelected;
            }
        }
        Return.Pressed += RequestReturn.Invoke;
        ShowStoryInfo(0);
    }
    private void ShowStoryInfo(int id)
    {
        foreach (StoryInfoUi s in StoryInfos)
        {
            s.Visible = false;
            s.SelectionButton.ButtonPressed = false;
        }
        var infoUi = StoryInfos[id];
        infoUi.Visible = true;
        infoUi.SelectionButton.ButtonPressed = true;
        infoUi.PressButton(0);
        levelDisplay.Visible = false;
    }
    private Vector2 levelDisplayOffset = new(37.5f,0);
    private void OnLevelSelected(Stories.CampaignLevel level, Vector2 bottomRightCornerPos)
    {
        if (level == null)
        {
            levelDisplay.Visible = false;
            return;
        }
        levelDisplay.Position = bottomRightCornerPos+levelDisplayOffset;
        levelDisplay.Visible = true;
        levelDisplay.PopAnim();
        levelDisplay.ShowLevel(level);
    }
}