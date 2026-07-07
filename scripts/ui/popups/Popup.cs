using Godot;

public partial class Popup : Node
{
    public Label MessageLabel { get; set; }
    public HBoxContainer ButtonRow { get; set; }
    public Button[] Options { get; set; }
    public bool CloseOnPress {get; set;}

    public void SetupNodes()
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
            var btn = new Button { Text = labels[i], SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            if (CloseOnPress)
                btn.Pressed += QueueFree;
            ButtonRow.AddChild(btn);
            Options[i] = btn;
        }
    }
}