using Godot;
using System;

public partial class LevelDisplay : Control
{
    [Export] Label LevelName;
    [Export] Label Author;
    [Export] Label Duration;
    [Export] Label Difficulty;
    [Export] Label AspectRatio;
    [Export] Container TagsContainer;

    [Export] Button Play;
    [Export] Button Edit;
    [Export] Button Delete;
    [Export] Modifiers Mods;
    private LevelDataSchema toPlay;
    private GameSession gs => GameSession.Instance;
    private Editor e => Editor.Instance;
    private UIManager ui => UIManager.Instance;
    public event Action RequestRepopulate;
    public override void _Ready()
    {
        Play.Pressed += OnPlay;
        Delete.Pressed += OnDelete;
        Edit.Pressed += OnEdit;
        Play.Disabled = true;
        Edit.Disabled = true;
        Delete.Disabled = true;
    }
    private void ClearTags()
    {
        foreach (var child in TagsContainer.GetChildren())
            child.QueueFree();
    }
    public void ShowLevel(LevelDataSchema schema)
    {
        toPlay = schema;
        ClearTags();
        if (schema == null)
        {
            LevelName.Text = "-";
            Author.Text = "-";
            Difficulty.Text = "-";
            Duration.Text = "-";
            AspectRatio.Text = "-";
            Play.Disabled = true;
            Edit.Disabled = true;
            Delete.Disabled = true;
            return;
        }
        Play.Disabled = false;
        Edit.Disabled = false;
        Delete.Disabled = false;

        LevelName.Text = schema.Name;
        Author.Text = $"{schema.Author}";
        Duration.Text = $"{schema.Duration:F2}s";
        Difficulty.Text = $"{schema.Difficulty:F1}";
        string ratioLabel = schema.AspectRatio switch
        {
            0 => "500, 900",
            1 => "900, 900",
            2 => "1350, 900",
            _ => "invalid"
        };
        foreach (var tag in schema.Tags)
        {
            var tagColor = RenderingUtils.ColorFromString(tag);
            var textColor = RenderingUtils.GetContrastingColor(tagColor);
            var label = new Label()
            { Text = tag, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis, CustomMinimumSize = new(80, 20), LabelSettings = new() { FontColor = textColor } };
            var colorRect = new ColorRect()
            { Color = tagColor, AnchorTop = 0, AnchorLeft = 0, AnchorBottom = 1, AnchorRight = 1};
            label.AddChild(colorRect);
            TagsContainer.AddChild(label);
        }
        AspectRatio.Text = ratioLabel;
    }
    private void OnPlay()
    {
        if (toPlay == null) return;
        var converted = LevelDataConverter.FromSchema(toPlay);
        gs.StartLevel(converted, Mods.GetStartParams());
    }
    private void OnEdit()
    {
        ui.ShowEditor();
        var converted = LevelDataConverter.FromSchema(toPlay);
        bool success = e.OpenLevel(converted);
        if (!success)
        {
            RequestRepopulate.Invoke();
            Popups.Instance.Show(Popups.DefaultType.OK, "Something went wrong opening this level");
        }
    } 
    private void OnDelete()
    {
        if (toPlay == null) return;
        var popup = Popups.Instance.Show(Popups.DefaultType.YN, $"Are you sure you want to delete '{toPlay.Name}'?");
        popup.CloseOnPress = true;
        popup.Options[0].Pressed += () => 
        {
            LevelStorage.DeleteLevel(toPlay.LocalId);
            ShowLevel(null);
            RequestRepopulate.Invoke();
        };
    }
}
