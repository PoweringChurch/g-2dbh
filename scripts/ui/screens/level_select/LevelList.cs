using System;
using System.Collections.Generic;
using Godot;

public partial class LevelList : Control
{
    [Export] public LevelDisplay LevelDisplay;
    [Export] public PackedScene LevelButtonScene;
    private readonly Dictionary<Button, float> hoverAmounts = [];
    private readonly Dictionary<Button, Tween> activeTweens = [];
    private readonly Dictionary<Button, int> buttonIndices = [];

    public void ClearButtons()
    {
        foreach (Button child in new List<Node>(GetChildren()))
        {
            if (activeTweens.TryGetValue(child, out var tween))
            {
                tween.Kill(); // stop it early so it doesn't touch a freed node next frame
                activeTweens.Remove(child);
                hoverAmounts.Remove(child);
            }
            buttonIndices.Remove(child);
            child.QueueFree();
        }
    }

    private enum SortMode { Name, Author, Difficulty }
    public void SearchQuery(string query)
    {
    }
    public void ShowLevelList(List<LevelDataSchema> show)
    {
        ClearButtons();
        for (int i = 0; i < show.Count; i++)
        {
            ShowLevel(show[i], i);
        }
    }

    private AudioStream buttonPressedSfx = AudioUtils.LoadAudio("res://data/sounds/button_pressed.wav");
    private void ShowLevel(LevelDataSchema schema, int index)
    {
        var btn = LevelButtonScene.Instantiate<Button>();

        var name = btn.GetNode<Label>("Sort/Stack/Name");
        var author = btn.GetNode<Label>("Sort/Stack/Author");
        var difficulty = btn.GetNode<Label>("Sort/Difficulty");
        var titleText = string.IsNullOrWhiteSpace(schema.Name) ? "unnamed" : schema.Name;

        name.Text = titleText;
        author.Text = "by " + schema.Author;
        difficulty.Text = $"{schema.Difficulty:F1}";

        btn.Pressed += () => 
        {
            AudioUtils.Instance.PlayAudio(buttonPressedSfx, AudioUtils.SFXVolume);
            LevelDisplay.ShowLevel(schema);
        };
        btn.MouseEntered += () => LevelButtonMouseEntered(btn);
        btn.MouseExited += () => LevelButtonMouseExited(btn);

        btn.MouseFilter = MouseFilterEnum.Pass;

        buttonIndices[btn] = index;
        AddChild(btn);

        var pos = GetTargetPosition(index);
        btn.Position = pos;
        btn.Size = new Vector2(maxDiagonalX, lbHeight);
    }

    private const float scrollStep = 40;
    private const float lbHeight = 100;
    private const float lbSeperation = 5;
    private const float maxDiagonalX = 500f;
    private float scrollOffset = 0f;
    private float goalScrollOffset = 0f;    
    private Vector2 GetTargetPosition(int index)
    {
        float y = index * (lbHeight + lbSeperation) - scrollOffset;
        float t = Mathf.Clamp(y / Size.Y, 0f, 1f);
        float x = t * maxDiagonalX;

        return new Vector2(x, y);
    }
    private Vector2 GetBasePosition(int index)
    {
        float y = index * (lbHeight + lbSeperation) - scrollOffset;
        float t = Mathf.Clamp(y / Size.Y, 0f, 1f);
        float x = t * maxDiagonalX;
        return new Vector2(x, y);
    }
    private void UpdatePositions()
    {
        foreach (var (btn, index) in buttonIndices)
        {
            var basePos = GetBasePosition(index);
            float hover = hoverAmounts.TryGetValue(btn, out var h) ? h : 0f;
            btn.Position = basePos + new Vector2(hover * hoverPushX, 0);
        }
    }
    private void ScrollBy(float delta)
    {
        float contentHeight = buttonIndices.Count * (lbHeight + lbSeperation);
        float maxScroll = Mathf.Max(0, contentHeight - Size.Y);
        goalScrollOffset = Mathf.Clamp(goalScrollOffset + delta, 0, maxScroll);
    }
        
    public override void _Process(double delta)
    {
        float weight = 1f - Mathf.Pow(0.001f, (float)delta);
        scrollOffset = Mathf.Lerp(scrollOffset, goalScrollOffset, weight);
        UpdatePositions();
    }
    
    
    private void CancelAllHovers()
    {
        foreach (var (btn, tween) in activeTweens)
        {
            tween.Kill();
            btn.Scale = Vector2.One;
        }
        activeTweens.Clear();
    }
    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.WheelUp && mb.Pressed)
            {
                ScrollBy(-scrollStep);
            }
            else if (mb.ButtonIndex == MouseButton.WheelDown && mb.Pressed)
            {
                ScrollBy(scrollStep);
            }
        }
    }
    private const float hoverPushX = 70f;
    private AudioStream buttonHoveredSfx = AudioUtils.LoadAudio("res://data/sounds/button_hovered.wav");
    private void LevelButtonMouseEntered(Button btn) 
    {
        btn.ZIndex = 1;
        AudioUtils.Instance.PlayAudio(buttonHoveredSfx, AudioUtils.SFXVolume);
        AnimateHoverAmount(btn, 1f);
    }
    private void LevelButtonMouseExited(Button btn) 
    {
        btn.ZIndex = 0;
        AnimateHoverAmount(btn, 0f);
    }
    private void AnimateHoverAmount(Button btn, float target)
    {
        if (activeTweens.TryGetValue(btn, out var existing))
            existing.Kill();

        float current = hoverAmounts.TryGetValue(btn, out var h) ? h : 0f;

        var tween = CreateTween();
        tween.TweenMethod(
            Callable.From<float>((v) => hoverAmounts[btn] = v),
            current, target, 0.15f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);

        activeTweens[btn] = tween;
    }
}