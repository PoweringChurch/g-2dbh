using Godot;
using System;
using System.Collections.Generic;

public partial class ScoreSummary : CanvasLayer
{
    [ExportGroup("Completion")] 
    [Export] public Control CompletionHolder;
    [Export] public Label Completion;
    [ExportGroup("Graze")] 
    [Export] public Control GrazeHolder;
    [Export] public Label Graze;
    [ExportGroup("Hp")] 
    [Export] public Control HpHolder;
    [Export] public Label HP;
    [ExportGroup("Info")] 
    [Export] public Label LevelName;
    [Export] public Label LevelAuthor;
    [ExportGroup("Buttons")] 
    [Export] public Button ResetButton;
    [Export] public Button QuitButton;

    public event Action RequestReset;
    public event Action RequestReturn;

    private readonly Dictionary<Control, float> originalXPositions = new();
    public override void _Ready()
    {
        ResetButton.Pressed += () => { RequestReset.Invoke(); ResetHolders(); };
        QuitButton.Pressed += () => { RequestReturn.Invoke(); ResetHolders(); };

        originalXPositions[HpHolder] = HpHolder.Position.X;
        originalXPositions[GrazeHolder] = GrazeHolder.Position.X;
        originalXPositions[CompletionHolder] = CompletionHolder.Position.X;

        ResetHolders();
    }
    public void AnimateScoreSummary(float completion, int graze, int hp, string levelName, string creatorName)
    {
        Completion.Text = $"{completion:P2}";
        Graze.Text = graze.ToString();
        HP.Text = hp.ToString();
        
        LevelName.Text = levelName;
        LevelAuthor.Text = $"by {creatorName}";

        AnimateMoveIn(CompletionHolder, 0.2f);
        AnimateMoveIn(HpHolder, 0.4f);
        AnimateMoveIn(GrazeHolder, 0.6f);
    }
    public void ResetHolders()
    {
        CompletionHolder.Position = new Vector2( originalXPositions[CompletionHolder], CompletionHolder.Position.Y );
        HpHolder.Position = new Vector2( originalXPositions[HpHolder], HpHolder.Position.Y );
        GrazeHolder.Position = new Vector2( originalXPositions[GrazeHolder], GrazeHolder.Position.Y );

        CompletionHolder.Modulate = new(1,1,1,0);
        HpHolder.Modulate = new(1,1,1,0);
        GrazeHolder.Modulate = new(1,1,1,0);
    }
    private const float slideOffset = 30;
    private void AnimateMoveIn(Control holder, float delay = 0)
    {
        Tween tween = CreateTween()
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out)
            .SetParallel();
        
        tween.TweenProperty(holder, "position:x", originalXPositions[holder] + slideOffset, 0.5).SetDelay(delay);
        tween.TweenProperty(holder, "modulate:a", 1.0f, 0.5f).SetDelay(delay);
    }

}
