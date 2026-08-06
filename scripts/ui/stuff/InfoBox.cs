using Godot;

public partial class InfoBox : Control
{
    private const string MessageBoxPath = "/root/main/Popups/MessageBox";
    [Export] public string Message;
    private Label MessageBox;
    private bool hovering = false;
    public override void _Ready()
    {
        MessageBox = GetNode<Label>(MessageBoxPath);
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
    }
    private void OnMouseEntered()
    {
        if (MessageBox == null) return;
        hovering = true;
        MessageBox.Text = Message;
        MessageBox.Visible = true;
        MessageBox.GlobalPosition = GetGlobalMousePosition();
    }
    private void OnMouseExited()
    {
        if (MessageBox == null) return;
        hovering = false;
        MessageBox.Visible = false;
    }
    public override void _Process(double delta)
    {
        if (MessageBox != null && hovering)
            MessageBox.GlobalPosition = GetGlobalMousePosition();
    }
}