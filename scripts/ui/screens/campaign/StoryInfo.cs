using System.Collections.Generic;
public enum StoryId
{
    SnowsweptKingdom
}


public static class Stories
{
    public class CampaignLevel
    {
        public Dialogue dialogue;
        public string levelPath;
    }
    
    public static readonly Dictionary<StoryId, CampaignLevel[]> StoryInfoById;
    private static CampaignLevel[] SnowsweptKingdom()
    {
        var Snowstorm = new CampaignLevel()
        {
            levelPath = "res://data/stories/snowswept_castle/snow_storm.json",
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
        };
        return [Snowstorm];
    }
    static Stories()
    {
        StoryInfoById = new()
        {
            [StoryId.SnowsweptKingdom] = SnowsweptKingdom()
        };
    }
}