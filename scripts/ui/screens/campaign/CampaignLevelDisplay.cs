using Godot;
public partial class CampaignLevelDisplay : Control
{
    [Export] Label LevelName;
    [Export] Label LevelDifficulty;
    [Export] Button Play;
    [Export] public DialogueHandler dialogueHandler;
    private GameSession gs => GameSession.Instance;
    private CampaignLevel toPlay;
    public override void _Ready()
    {
        Play.Pressed += () =>
        {
            dialogueHandler.PlayDialogue(toPlay.dialogue);
            dialogueHandler.DialogueFinished += () => gs.StartLevel(SerializationUtils.ReadJson<LevelData>(toPlay.levelPath), new());
        };
    }
    public void ShowLevel(CampaignLevel level)
    {
        LevelName.Text = level.levelName;
        LevelDifficulty.Text = level.difficulty.ToString();
        toPlay = level;
    }
}