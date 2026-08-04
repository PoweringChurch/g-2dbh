using Godot;

public partial class CampaignCharacterDisplay : Control
{
    private AudioStream pokeSfx = AudioUtils.LoadAudio("res://data/sounds/poke.wav");
    public override void _Ready()
    {
        PivotOffset = Size / 2;
    }
    public void MoveTo(Vector2 newpos)
    {
        Tween tween = CreateTween();
        tween.TweenProperty(this, "position", newpos-PivotOffset, 0.5f);
    }
    public void SnapTo(Vector2 newpos) =>
        Position = newpos-PivotOffset;
    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.Left && mb.Pressed)
            {
                TriggerSquishEffect();
            }
        }
    }
    private void TriggerSquishEffect()
    {
        Tween tween = CreateTween();
        Vector2 squishedScale = new Vector2(1.2f, 0.8f);
        Vector2 originalScale = Vector2.One;
        tween.TweenProperty(this, "scale", squishedScale, 0.08f)
             .SetTrans(Tween.TransitionType.Quad)
             .SetEase(Tween.EaseType.Out);
        tween.TweenProperty(this, "scale", originalScale, 0.15f)
             .SetTrans(Tween.TransitionType.Elastic)
             .SetEase(Tween.EaseType.Out);
        AudioUtils.Instance.PlayAudio(pokeSfx, AudioUtils.SFXVolume);
    }
}