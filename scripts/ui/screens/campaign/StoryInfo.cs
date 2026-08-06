using System.Collections.Generic;
public enum StoryId
{
    SnowsweptKingdom
}


public static class Stories
{
    public class StoryInfo
    {
        public string Name;
        public List<CampaignLevel> Levels;
    }
    public class CampaignLevel
    {
        public Dialogue dialogue;
        public string levelPath;
    }
    
    public static readonly StoryInfo SnowsweptKingdom;
    public static readonly Dictionary<StoryId, StoryInfo> StoryInfoById;
    static Stories()
    {
        SnowsweptKingdom = new StoryInfo()
        {
            Name = "Snowswept Kingdom",
            Levels =
            [
                new CampaignLevel() // Snowstorm
                {
                    levelPath = "res://data/stories/snowswept_kingdom/snow_storm.json",
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
                                Text = "1."
                            },
                            new()
                            {
                                SpeakerName = "Blue box",
                                HighlightIdx = 1,
                                Text = "2."
                            },
                        ]
                    }
                }
            ]
        };

        StoryInfoById = new()
        {
            [StoryId.SnowsweptKingdom] = SnowsweptKingdom
        };
    }
}