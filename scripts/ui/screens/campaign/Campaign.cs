using Godot;
using System;

public partial class Campaign : CanvasLayer
{
    public event Action RequestReturn;
    [Export] private StoryInfoUi[] StoryInfos;
    [Export] Button Return;
    [Export] CampaignLevelDisplay levelDisplay;
    [Export] Button PreviousWorld;
    [Export] Label WorldNameLabel;
    [Export] Button NextWorld;
    private int currentStory = 0;
    public override void _Ready()
    {
        for (int i = 0; i < StoryInfos.Length; i++)
        {
            var story = StoryInfos[i];
            if (story != null)
            {
                story.PressButton(0);
                story.LevelSelected += OnLevelSelected;
            }
        }
        PreviousWorld.Pressed += () =>
        {
            if (currentStory > 0)
                currentStory--;
            ShowStoryInfo(currentStory);
        };
        NextWorld.Pressed += () =>
        {
            if (currentStory < StoryInfos.Length-1)
                currentStory++;
            ShowStoryInfo(currentStory);
        };
        Return.Pressed += RequestReturn.Invoke;
        ShowStoryInfo(0);
    }
    private void ShowStoryInfo(int id)
    {
        foreach (StoryInfoUi s in StoryInfos)
            s.Visible = false;
        var infoUi = StoryInfos[id];
        infoUi.Visible = true;
        var storyInfo = Stories.StoryInfoById[infoUi.Id];
        WorldNameLabel.Text = storyInfo.Name;
        infoUi.PressButton(0);

    }
    private void OnLevelSelected(CampaignLevel level)
    {
        levelDisplay.ShowLevel(level);
    }
}