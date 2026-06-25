using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Godot;
public partial class LevelPreview : Node2D
{
    [Export] TextureRect BackgroundImage;
    [Export] SubViewport PreviewVP;
    Editor e;
    float resScale = 1;
    private Vector2 _dragOffset = Vector2.Zero;
    private ReferenceTracker<ProjectileEditorInstance> _projectiles;
    private ReferenceTracker<PatternEditorInstance> _patterns;

    public override void _Ready()
    {
        e = GetNode<Editor>("/root/Editor");
		GetTree().Root.SizeChanged += OnWindowResized;
        _projectiles = new ReferenceTracker<ProjectileEditorInstance>(this);
        _patterns    = new ReferenceTracker<PatternEditorInstance>(this);
    }
    public void Load(LevelData data)
    {
        ClearInstances();
        foreach (var r in data.References)
            AddInstance(r);
        BackgroundImage.Texture = RenderingUtils.LoadTexture(e.LevelPath+"images/",data.BgImage);
        Fit(PlayingField.Resolutions[data.AspectRatio]);
    }
    public void Fit(Vector2I res)
    {
        var win    = GetTree().Root.GetVisibleRect().Size;
		resScale = Mathf.Min(win.X / res.X, win.Y / res.Y)*0.6f;
        PreviewVP.Size = (Vector2I)((Vector2)res*resScale);
        Scale = Vector2.One*resScale;
    }
    private void OnWindowResized() => Fit(PlayingField.Resolutions[e.levelData.AspectRatio]);
    public void OnBackgroundImageChanged(string to)
    {
        if (to == "none")
        {
            BackgroundImage.Texture = null;
            return;
        }
        var bg = RenderingUtils.LoadTexture(e.LevelPath+"images/",to);
        BackgroundImage.Texture = bg;
    }
    public Vector2 ToPreviewLocal(Vector2 screenPos) =>
        (screenPos - GlobalPosition) / resScale;
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed)
        {
            if ( mb.ButtonIndex == MouseButton.Left)
            {
                var (reference, offset) = GetNearestReference(mb.Position);
                if (reference == null)
                {
                    e.SelectedReference = null;
                    return;
                }
                switch (e.CurrentMode)
                {
                    case Editor.Mode.Place:
                        var local = ToPreviewLocal(mb.Position);
                        Reference newRef = new Reference
                        {
                            Id = e.SelectedModel.Id,
                            Type = e.SelectedModel is ProjectileModel v ? ModelType.Projectile : ModelType.Pattern,
                            T = e.CurrentTime,
                            X = local.X,
                            Y = local.Y
                        };
                        if (newRef != null)
                        {
                            e.SelectedReference = newRef;
                            _dragOffset = offset;
                            e.AddReference(newRef);
                        }
                        break;
                    case Editor.Mode.Select:
                        e.SelectedReference = reference;
                        _dragOffset         = offset;
                        break;
                    case Editor.Mode.Delete:
                        e.DeleteReference(reference);
                        e.SelectedReference = null; // just in case
                        break;
                }
            }
            else if (mb.ButtonIndex == MouseButton.Right && e.CurrentMode == Editor.Mode.Select)
                e.SelectedReference = null;
            else if (e.CurrentMode == Editor.Mode.Place)
                e.SelectedReference = null;
        }
        if (@event is InputEventMouseMotion mm && e.SelectedReference != null)
        {
            var local  = ToPreviewLocal(mm.Position);
            e.SelectedReference.X = local.X;
            e.SelectedReference.Y = local.Y;
        }
    }
    private (Reference reference, Vector2 offset) GetNearestReference(Vector2 pos)
    {
        var local = ToPreviewLocal(pos);
        var (projRef, projOffset) = _projectiles.GetNearestReference(local, e.CurrentTime);
        var (patRef, patOffset)   = _patterns.GetNearestReference(local, e.CurrentTime);
        // pick whichever is actually non-null and, if both found, the closer one
        if (projRef != null && patRef != null)
        {
            float projDist = local.DistanceTo(local - projOffset);
            float patDist  = local.DistanceTo(local - patOffset);
            return projDist <= patDist ? (projRef, projOffset) : (patRef, patOffset);
        }
        if (projRef != null) return (projRef, projOffset);
        if (patRef != null)  return (patRef, patOffset);
        return (null, Vector2.Zero);
    }
    public void OnModelUpdate(ProjectileModel model, string oldId) => _projectiles.OnModelUpdate(model.Id, oldId);
    public void OnModelUpdate(PatternModel model, string oldId) => _patterns.OnModelUpdate(model.Id, oldId);
    public void AddInstance(Reference r) 
    {
        if (r.Type == ModelType.Projectile)
            _projectiles.Add(r);
        else
            _patterns.Add(r);
    }
    public void RemoveInstance(Reference r) 
    {
        if (r.Type == ModelType.Projectile)
            _projectiles.Remove(r);
        else
            _patterns.Remove(r);
    }
    public void ClearInstances()
    {
        _projectiles.Clear();
        _patterns.Clear();
    }
}
