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
        foreach (var story in StoryInfos)
        {
            if (story != null)
            {
                story.LevelSelected += OnLevelSelected;
            }
        }
        PreviousWorld.Pressed += () =>
        {
            if (currentStory > 0)
                currentStory--;
            ShowStoryInfo(currentStory);
            levelDisplay.ShowLevel(null);
        };
        NextWorld.Pressed += () =>
        {
            if (currentStory < StoryInfos.Length-1)
                currentStory++;
            ShowStoryInfo(currentStory);
            levelDisplay.ShowLevel(null);
        };
        Return.Pressed += RequestReturn.Invoke;
    }
    private void ShowStoryInfo(int id)
    {
        foreach (StoryInfo s in StoryInfos)
            s.Visible = false;
        var info = StoryInfos[id];
        info.Visible = true;
        WorldNameLabel.Text = info.WorldName;
    }
    private void OnLevelSelected(LevelData data)
    {
        levelDisplay.ShowLevel(data);
    }
}