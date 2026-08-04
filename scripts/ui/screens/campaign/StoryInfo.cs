using System.Collections.Generic;
public enum StoryId
{
    SnowWorld
}
public class CampaignLevel
{
    public Dialogue dialogue;
    public string levelPath;
    public string levelName;
    public int difficulty;
}
public class StoryInfo
{
    public string Name;
    public List<CampaignLevel> Levels;
}
public static class Stories
{
    public static readonly StoryInfo SnowWorld;
    public static readonly Dictionary<StoryId, StoryInfo> StoryInfoById;
    static Stories()
    {
        SnowWorld = new StoryInfo()
        {
            Name = "Snow World",
            Levels = new()
            {
                new CampaignLevel() // Snowstorm
                {
                    levelName = "Snow Storm",
                    levelPath = "res://data/levels/snow-world/snowstorm.json",
                    dialogue = new()
                    {
                        Portraits = [ 
                            RenderingUtils.LoadTexture("res://data/images/portraits/placeholder0.png"), 
                            RenderingUtils.LoadTexture("res://data/images/portraits/placeholder1.png") 
                        ],
                        Lines = [ 
                            new()
                            {
                                SpeakerName = "Red box",
                                HighlightIdx = 0,
                                Text = "Welcome back! I have a dialogue system to show off."
                            },
                            new()
                            {
                                SpeakerName = "Blue box",
                                HighlightIdx = 1,
                                Text = "Campaign levels can now have dialogue before or after gameplay. Right now this is just a placeholder, but it'll eventually have character artwork and unique music."
                            },
                            new()
                            {
                                SpeakerName = "Red box",
                                HighlightIdx = 0,
                                Text = "I've been working on the engine a lot behind the scenes, which is why this update took longer than expected."
                            },
                            new()
                            {
                                SpeakerName = "Red box",
                                HighlightIdx = 0,
                                Text = "This level looks different from the showcase because it's still waiting to be updated to the newest version of the game."
                            },
                            new()
                            {
                                SpeakerName = "Blue box",
                                HighlightIdx = 1,
                                Text = "I'm aiming to post a showcase around once a week as development continues."
                            },
                            new()
                            {
                                SpeakerName = "Blue box",
                                HighlightIdx = 1,
                                Text = "Once the game reaches alpha, I'll be looking into releasing a playable build. Thanks for following along!"
                            }
                        ]
                    },
                    difficulty = 1
                }
            }
        };
        
        StoryInfoById = new()
        {
            [StoryId.SnowWorld] = SnowWorld
        };
    }
}