using Godot;
public partial class CampaignLevelDisplay : Control
{
    [Export] Label LevelName;
    [Export] Label Duration;
    [Export] Label Difficulty;
    [Export] Button Play;
    [Export] public DialogueHandler dialogueHandler;
    private GameSession gs => GameSession.Instance;
    private Stories.CampaignLevel toPlay;
    private Tween activeTween;
    public override void _Ready()
    {
        Play.Pressed += () =>
        {
            dialogueHandler.PlayDialogue(toPlay.dialogue);
            var schema = SerializationUtils.ReadJson<LevelDataSchema>(toPlay.levelPath);
            var raw = LevelDataConverter.FromSchema(schema);
            dialogueHandler.DialogueFinished += () => 
            {
                Console.Log($"[CampaignLevelDisplay] Started campaign level '{schema.Name}', data: '{schema.Data}')");
                gs.StartLevel(raw, new());
                dialogueHandler.DisconnectEvents();
            };
            dialogueHandler.DialogueCancelled += dialogueHandler.DisconnectEvents;
        };
    }
    public void PopAnim()
    {
        activeTween?.Kill();
        Scale = Vector2.Zero;
        activeTween = CreateTween();
        activeTween.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        activeTween.TweenProperty(this, "scale", Vector2.One, 0.2f);
    }
    public void ShowLevel(Stories.CampaignLevel level)
    {
        toPlay = level;
        var schema = SerializationUtils.ReadJson<LevelDataSchema>(toPlay.levelPath);
        LevelName.Text = schema.Name;
        Duration.Text = $"{schema.Duration:F2}s";
        Difficulty.Text = $"{schema.Difficulty:F1}";
        toPlay = level;
    }
}