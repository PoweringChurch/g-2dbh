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
                                Text = "This is a sample of what dialogue will look like"
                            },
                            new()
                            {
                                SpeakerName = "Blue box",
                                HighlightIdx = 1,
                                Text = "For right now, this feature will be exclusive to campaign levels"
                            },
                            new()
                            {
                                SpeakerName = "Red box",
                                HighlightIdx = 0,
                                Text = "There will be character art in place of these boxes further into development, and the music will change away from the main menu playlist when in dialogue"
                            },
                            new()
                            {
                                SpeakerName = "Red box",
                                HighlightIdx = 0,
                                Text = "Also, I didn't mean for this video to take so long to come out, but trust that a lot of work has been going on in the back"
                            },
                            new()
                            {
                                SpeakerName = "Blue box",
                                HighlightIdx = 1,
                                Text = "I'm aiming to post a video showcase of new stuff at least every week going forward, and maybe get out a playable build when the game reaches it's alpha version"
                            },
                            new()
                            {
                                SpeakerName = "Blue box",
                                HighlightIdx = 1,
                                Text = "Don't mind how the level looks a bit different from what was showcased before, it's not yet updated to the new version and many things are now incompatible"
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