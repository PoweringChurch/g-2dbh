using System;
using System.Collections.Generic;
using Godot;
public partial class MainMenu : CanvasLayer
{
    public event Action RequestCampaign;
    public event Action RequestLevelSelect;
    public event Action RequestSettings;
    public event Action RequestReturn;
    // references
    [Export] Button campaignButton;
    [Export] Button customsButton;
    [Export] Button settingsButton;
    [Export] Button quitButton;
    private readonly Dictionary<Button, float> originalXPositions = new();
    private readonly Dictionary<Button, Tween> activeTweens = new();
    private const float TweenDuration = 0.2f;
    private const float HoverOffsetX = 20.0f;
    public override void _Ready()
    {
        SetupButton(campaignButton, RequestCampaign);
        SetupButton(customsButton, RequestLevelSelect);
        SetupButton(settingsButton, RequestSettings);
        SetupButton(quitButton, RequestReturn);
    }
    private void SetupButton(Button button, Action action)
    {
        if (button == null) return;
        if (action != null)
            button.Pressed += action.Invoke;
        originalXPositions[button] = button.Position.X;
        button.MouseEntered += () => AnimateButton(button, true);
        button.MouseExited += () => AnimateButton(button, false);
    }
    private void AnimateButton(Button button, bool isHovered)
    {
        if (activeTweens.ContainsKey(button) && activeTweens[button].IsValid())
        {
            activeTweens[button].Kill();
        }

        float targetX = isHovered 
            ? originalXPositions[button] + HoverOffsetX 
            : originalXPositions[button];

        Tween tween = CreateTween().SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(button, "position:x", targetX, TweenDuration);

        activeTweens[button] = tween;
    }
}