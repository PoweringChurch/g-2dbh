using Godot;

public partial class TimelineMarker : ColorRect
{
    public EditorReference Reference;
    private bool _dragging = false;
    private Editor e => Editor.Instance;
    public void Init(EditorReference reference, float duration, Vector2 timelineSize)
    {
        Size = new Vector2(4, timelineSize.Y/2);
        Reference = reference;
        Refresh(duration, timelineSize);
    }
    public void Refresh(float duration, Vector2 timelineSize)
    {
        Color = Reference.Type == ModelType.Projectile ? 
        RenderingUtils.ColorFromString(e.ProjectileModels[Reference.Id].Name) 
        : RenderingUtils.ColorFromString(e.PatternModels[Reference.Id].Name);
        Position = new Vector2((float)(Reference.T / duration * timelineSize.X - Size.X / 2f), timelineSize.Y/2);
    }
}