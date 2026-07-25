using Godot;

public partial class TimelineMarker : ColorRect
{
    private const float MarkerWidth = 4;
    private const float TimelineUiWidth  = 1920;
    private const float TimelineUiHeight  = 40;
    private EditorReference Reference;
    private static bool holding = false;
    private bool dragging = false;
    private Editor e => Editor.Instance;
    public void Init(EditorReference reference)
    {
        Size = new Vector2(MarkerWidth, TimelineUiHeight/2);
        Reference = reference;
        Refresh();
    }
    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb)
        {
            if (e.CurrentMode == Toolbar.Mode.Select || e.CurrentMode == Toolbar.Mode.Place)
            {
                dragging = mb.Pressed;
            } else if (e.CurrentMode == Toolbar.Mode.Delete && mb.Pressed)
            {
                e.DeleteReference(Reference);
                e.SyncPreview();
            }
        }
        if (@event is InputEventMouseMotion mm)
        {
            if (dragging && (e.CurrentMode == Toolbar.Mode.Select || e.CurrentMode == Toolbar.Mode.Place))
            {
                float newPositionX = Position.X + mm.Relative.X;
                Position = new Vector2( newPositionX,  TimelineUiHeight / 2f );
                Reference.T = PositionXToTime(newPositionX, e.levelData.Duration);
                
                e.UpdateReference(Reference);
                e.SyncPreview();
            } else if (holding && e.CurrentMode == Toolbar.Mode.Delete)
            {
                e.DeleteReference(Reference);
                e.SyncPreview();
            }
        }
    }
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb)
        {
            holding = mb.Pressed;
        }
    }

    public void Refresh()
    {
        Color = Reference.Type == ModelType.Projectile ? 
        RenderingUtils.ColorFromString(e.ProjectileModels[Reference.Id].Name) 
        : RenderingUtils.ColorFromString(e.PatternModels[Reference.Id].Name);
        Position = new Vector2(
            TimeToPositionX((float)Reference.T, e.levelData.Duration), 
            TimelineUiHeight / 2f
        );
    }
    private static float TimeToPositionX(float t, float duration)
    {
        if (duration <= 0f) return 0f;
        return (t / duration * TimelineUiWidth) - (MarkerWidth / 2f);
    }
    private static float PositionXToTime(float posX, float duration)
    {
        float centeredX = posX + (MarkerWidth / 2f);
        return Mathf.Clamp((centeredX / TimelineUiWidth) * duration, 0f, duration);
    }
}