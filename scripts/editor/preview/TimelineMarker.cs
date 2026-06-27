using Godot;

public partial class TimelineMarker : ColorRect
{
    public EditorReference Reference;
    private bool _dragging = false;
    private Editor e;
    public void Init(EditorReference reference, float duration, float timelineWidth)
    {
        e = GetNode<Editor>("/root/Editor");
        Size = new Vector2(6, 16);
        Reference = reference;
        Refresh(duration, timelineWidth);
    }
    public void Refresh(float duration, float timelineWidth)
    {
        Color = Reference.Type == ModelType.Projectile ? 
        RenderingUtils.ColorFromString(e.ProjectileModels[Reference.Id].Name) 
        : RenderingUtils.ColorFromString(e.PatternModels[Reference.Id].Name);
        Position = new Vector2((float)(Reference.T / duration * timelineWidth - Size.X / 2f), 43);
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
                e.DeleteReference(Reference);
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