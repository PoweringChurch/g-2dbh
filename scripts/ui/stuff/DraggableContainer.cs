using Godot;

[GlobalClass]
public partial class DraggableContainer : FoldableContainer
{
    private Control DraggableControl;
    private bool _dragging = false;
    private Vector2 _dragOffset = Vector2.Zero;
    public override void _Ready()
    {
        DraggableControl = this;
        DraggableControl.GuiInput += OnFoldButtonGuiInput;
    }
    private void OnFoldButtonGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            _dragging = mb.Pressed;
            if (_dragging)
                _dragOffset = mb.GlobalPosition - GlobalPosition;
        }
        if (@event is InputEventMouseMotion mm && _dragging)
        {
            GlobalPosition = mm.GlobalPosition - _dragOffset;
        }
    }
}