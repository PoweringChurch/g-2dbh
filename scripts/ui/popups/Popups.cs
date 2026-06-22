using Godot;
using System;
using System.Collections.Generic;

public partial class Popups : FoldableContainer
{
    private const string ScenePath = "res://data/scenes/ui/popup_ui.tscn";
    public enum DefaultType
    {
        OK,
        YN,
        OKCancel
    }

    public Label MessageLabel { get; private set; }
    public HBoxContainer ButtonRow { get; private set; }
    public Button[] Options { get; private set; }

    // Optional: closes the popup automatically when any option is pressed
    public bool CloseOnPress { get; set; } = true;

    public static Popups Create(string[] optionLabels, string message = "")
    {
        var scene = GD.Load<PackedScene>(ScenePath);
        var popup = scene.Instantiate<Popups>();
        popup.SetupNodes();
        popup.SetMessage(message);
        popup.SetOptions(optionLabels);
        return popup;
    }

    public static Popups Create(DefaultType type, string message = "") =>
        Create(GetLabelsFor(type), message);

    private static string[] GetLabelsFor(DefaultType type) => type switch
    {
        DefaultType.OK       => new[] { "OK" },
        DefaultType.YN       => new[] { "Yes", "No" },
        DefaultType.OKCancel => new[] { "OK", "Cancel" },
        _ => Array.Empty<string>()
    };

    private void SetupNodes()
    {
        MessageLabel = GetNode<Label>("VBoxContainer/Label");
        ButtonRow = GetNode<HBoxContainer>("VBoxContainer/HBoxContainer");
    }

    public void SetMessage(string message)
    {
        MessageLabel.Text = message;
        MessageLabel.Visible = !string.IsNullOrEmpty(message);
    }

    public void SetOptions(string[] labels)
    {
        foreach (Node child in ButtonRow.GetChildren())
            child.QueueFree();

        Options = new Button[labels.Length];
        for (int i = 0; i < labels.Length; i++)
        {
            var btn = new Button { Text = labels[i] };
            if (CloseOnPress)
                btn.Pressed += Close;
            ButtonRow.AddChild(btn);
            Options[i] = btn;
        }
    }

    public void Close() => QueueFree();

    public static Popups Show(Node parent, string[] optionLabels, string message = "")
    {
        var popup = Create(optionLabels, message);
        parent.AddChild(popup);
        return popup;
    }

    public static Popups Show(Node parent, DefaultType type, string message = "")
    {
        var popup = Create(type, message);
        parent.AddChild(popup);
        return popup;
    }
}