using Godot;
using System;

public partial class Campaign : CanvasLayer
{
    public event Action RequestReturn;
    [Export] private StoryInfo[] StoryInfos;
    [Export] Button Return;
    [Export] LevelDisplay levelDisplay;
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
                story.ClickButton(0);
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
        foreach (StoryInfo s in StoryInfos)
            s.Visible = false;
        var info = StoryInfos[id];
        info.Visible = true;
        WorldNameLabel.Text = info.WorldName;
        info.ClickButton(0);
    }
    private void OnLevelSelected(LevelData data)
    {
        levelDisplay.ShowLevel(data);
    }
}