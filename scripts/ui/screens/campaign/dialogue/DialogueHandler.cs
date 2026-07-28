using System;
using System.Collections.Generic;
using Godot;
public partial class DialogueHandler : Control
{
    public event Action DialogueFinished;
    [Export] Container PortraitHolder;
    [Export] Label SpeakerLabel;
    [Export] Label DialogueLabel;
    private List<TextureRect> portraitUis = new();
    private Dialogue currentDialogue;
    private bool playing = false;
    private int currentMessage = 0;

    public override void _Input(InputEvent @event)
    {
        if (!playing) return;
        if (@event.IsAction("skip_dialogue"))
        {
            StopDialogue();
        }
        if (@event is InputEventMouseButton mb)
		{
			if (mb.ButtonIndex == MouseButton.Left && mb.Pressed)
            {
                Next();
            }
        }
    }
    public void PlayDialogue(Dialogue dialogue)
    {
        playing = true;
        portraitUis.Clear();
        foreach (var child in PortraitHolder.GetChildren())
            child.QueueFree();
        foreach (var p in dialogue.Portraits)
        {
            var portrait = new TextureRect()
            {
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                Texture = p
            };
            PortraitHolder.AddChild(portrait);
            portraitUis.Add(portrait);
        }
        currentDialogue = dialogue;
        currentMessage = 0;

        Visible = true;
        Modulate = new Color(1, 1, 1, 0);
        CreateTween()
            .TweenProperty(this, "modulate:a", 1.0f, 0.25f);
        ShowMessage(0);
    }
    private void ShowMessage(int idx)
    {
        var message = currentDialogue.Lines[idx];
        SpeakerLabel.Text = message.SpeakerName;
        DialogueLabel.Text = message.Text;
        const float dim = 0.6f;
        for (int i = 0; i < portraitUis.Count; i++)
        {
            var character = portraitUis[i];
            var targetColor = i == message.HighlightIdx
                ? Colors.White
                : new(dim,dim,dim,1);
            CreateTween()
                .TweenProperty(character, "modulate", targetColor, 0.2f);
        }
    }
    private void Next()
    {
        if (++currentMessage == currentDialogue.Lines.Length)
            StopDialogue();
        else
            ShowMessage(currentMessage);
    }
    private void StopDialogue()
    {
        if (!playing)
            return;
        playing = false;
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.25f);
        tween.Finished += () =>
        {
            currentDialogue = null;
            currentMessage = 0;
            Visible = false;
            Modulate = Colors.White;
        };
        DialogueFinished?.Invoke();
    }
}
public class Dialogue
{
    public Texture2D[] Portraits;
    public Line[] Lines;
}
public struct Line
{
    // What should the speaker label show
    public string SpeakerName;
    // What should the message say
    public string Text;
    // Which character index should be highlighted when this message shows
    public int HighlightIdx;
}