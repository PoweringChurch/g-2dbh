using Godot;

public partial class TimelineMarker : ColorRect
{
    public ISpatialReference Reference;
    private bool _dragging = false;
    private Editor e;
    public void Init(ISpatialReference reference, float duration, float timelineWidth)
    {
        e = GetNode<Editor>("/root/Editor");
        Size = new Vector2(6, 16);
        Reference = reference;
        Refresh(duration, timelineWidth);
    }
    public void Refresh(float duration, float timelineWidth)
    {
        Color = RenderingUtils.ColorFromString(Reference.Id);
        Position = new Vector2((Reference.T / duration) * timelineWidth - Size.X / 2f, 43);
    }
    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            if (e.CurrentMode == Editor.Mode.Select)
            {
                _dragging = mb.Pressed; 
            }
            else if (e.CurrentMode == Editor.Mode.Delete)
            {
                if (Reference is ProjectileReference proj)
                    e.DeleteReference(proj);
                else
                    e.DeleteReference((PatternReference)Reference);
            }
        }
        if (@event is InputEventMouseMotion mm 
        && _dragging 
        && e.CurrentMode == Editor.Mode.Select)
        {
            float newX = Mathf.Clamp(Position.X + mm.Relative.X, 0, GetParent<Control>().Size.X - Size.X);
            Position = new Vector2(newX, Position.Y);
            Reference.T = (newX + Size.X / 2f) / GetParent<Control>().Size.X * e.levelData.Duration;
        }
    }
}