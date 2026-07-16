using System;
using Godot;

public partial class ProjectilePreview : Control
{
    public ProjectileModel Model;
    [Export] private Control DrawOn;
    [Export] private Button PlaybackToggle;
    [Export] private Button HomeButton;
    [Export] private SpinBox TimeSpin;
    private bool dirty = false;
    private EvalContext ctx = new();
    private SpatialReference reference;

    private bool dragging = false;
    private bool panning = false;
    private bool playing = false;

    private float zoom = 1f;
    private Vector2 zoomPan = Vector2.Zero;
    private const float ZoomMin = 0.25f;
    private const float ZoomMax = 4f;
    private const float ZoomStep = 1.1f;

    public override void _Ready()
    {
        ctx.T = 0;
        DrawOn.Draw += DrawProjectileShape;
        DrawOn.Draw += DrawHitbox;
        DrawOn.Draw += DrawGizmos;
        PlaybackToggle.Pressed += () => { playing = !playing; PlaybackToggle.Text = playing ? "❚❚" : "▶"; };
        TimeSpin.ValueChanged += (v) => { ctx.T = v; MarkDirty(); };
        HomeButton.Pressed += Home;
    }
    public void Load(ProjectileModel m)
    {
        Model = m;
        Home();
    }
    public void MarkDirty()
    {
        dirty = true;
    }
    private void Home()
    {
        reference.SpawnPos = new Vector2(918, 694) / 2;
        TimeSpin.Value = 0;
        zoom = 1f;
        zoomPan = Vector2.Zero;
        MarkDirty();
    }

    private Vector2 ViewCenter => DrawOn.Size / 2;
    private Vector2 WorldToScreen(Vector2 world) => ViewCenter + zoomPan + (world - ViewCenter) * zoom;
    private Vector2 ScreenToWorld(Vector2 screen) => ViewCenter + (screen - ViewCenter - zoomPan) / zoom;

    private void ApplyZoom(float newZoom, Vector2 screenPos)
    {
        newZoom = Mathf.Clamp(newZoom, ZoomMin, ZoomMax);
        if (Mathf.IsEqualApprox(newZoom, zoom))
            return;
        var worldUnderCursor = ScreenToWorld(screenPos);
        zoom = newZoom;
        zoomPan = screenPos - ViewCenter - (worldUnderCursor - ViewCenter) * zoom;
        MarkDirty();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.Left)
            {
                if (mb.Pressed)
                {
                    var dist = (mb.Position - WorldToScreen(reference.Pos)).Length();
                    dragging = dist < 20;
                }
                else
                    dragging = false;
            }
            else if (mb.ButtonIndex == MouseButton.Middle)
            {
                panning = mb.Pressed;
            }
            else if (mb.ButtonIndex == MouseButton.WheelUp && mb.Pressed)
            {
                ApplyZoom(zoom * ZoomStep, mb.Position - DrawOn.Position);
            }
            else if (mb.ButtonIndex == MouseButton.WheelDown && mb.Pressed)
            {
                ApplyZoom(zoom / ZoomStep, mb.Position - DrawOn.Position);
            }
        }
        else if (@event is InputEventMouseMotion mm)
        {
            if (dragging)
            {
                reference.SpawnPos = ScreenToWorld(mm.Position - DrawOn.Position);
                MarkDirty();
            }
            else if (panning)
            {
                reference.SpawnPos += mm.Relative / zoom;
                MarkDirty();
            }
        }
    }
    public override void _Process(double dt)
    {
        if (dirty)
        {
            Sync();
            dirty = false;
        }
        if (playing)
        {
            TimeSpin.Value += dt;
            Sync();
        }
    }
    private void Sync()
    {
        if (Model == null)
            return;
        ctx.L = Model.Lifetime;
        var f = Model.fnf(ctx) + reference.SpawnF;
        var pos = LevelDirector.CalculatePosition(Model.fnx, Model.fny, f, ctx);
        reference.Pos = reference.SpawnPos + pos;
        reference.F = f;
        DrawOn.QueueRedraw();
    }
    private void DrawHitbox()
    {
        if (Model == null)
            return;
        bool alive = ctx.T >= 0 && ctx.T <= ctx.L;
        bool show = Model.CanCollide && (ctx.T > Model.TelegraphTime) && alive;
        if (!show) return;
        var forward = Model.LockRotation ? 0 : (float)reference.F;
        DrawOn.DrawSetTransform(WorldToScreen(reference.Pos), forward, Vector2.One * zoom);
        if (Model.UseShape && Model.Shape != null)
        {
            DrawOn.DrawPolyline([.. Model.ShapeVect2s, Model.ShapeVect2s[0]], Colors.Red);
        }
        else
            DrawOn.DrawCircle(Vector2.Zero, Model.Radius, Colors.Red, false);
    }
    private void DrawProjectileShape()
    {
        if (Model == null)
            return;
        bool alive = ctx.T >= 0 && ctx.T <= ctx.L;
        var color = Colors.White;
        if (!alive) color.A *= 0.5f;
        var forward = Model.LockRotation ? 0 : (float)reference.F;
        DrawOn.DrawSetTransform(WorldToScreen(reference.Pos), forward, Vector2.One * Model.RenderScale * zoom);
        var texture = RenderingUtils.LoadTexture(Model.Texture);
        if (texture != null)
        {
            DrawOn.DrawTexture(texture, -texture.GetSize() / 2, color);
            return;
        }
        if (Model.UseShape && Model.Shape != null)
        {
            DrawOn.DrawColoredPolygon([.. Model.ShapeVect2s, Model.ShapeVect2s[0]], color);
        }
        else
            DrawOn.DrawCircle(Vector2.Zero, Model.Radius, color);
    }
    private void DrawGizmos()
    {
        int steps = ConfigHelper.Current.PathFidelity;
        Vector2[] points = new Vector2[steps];
        var lctx = new EvalContext();
        for (int i = 0; i < steps; i++)
        {
            lctx.T = Math.Min(Model.Lifetime, ConfigHelper.Current.MaxPathLength) / steps * i;
            lctx.L = Model.Lifetime;
            var (x, y) = LevelDirector.CalculatePosition(Model.fnx, Model.fny, Model.fnf(lctx) + reference.SpawnF, lctx);
            points[i] = WorldToScreen(new(reference.SpawnPos.X + x, reference.SpawnPos.Y + y));
        }
        DrawOn.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        DrawOn.DrawPolyline(points, RenderingUtils.ColorFromString(Model.Name), ConfigHelper.Current.PathThickness, true);
        DrawOn.DrawCircle(points[0], ConfigHelper.Current.PathThickness * 1.5f, RenderingUtils.ColorFromString(Model.Name));
        DrawOn.DrawDashedLine(WorldToScreen(reference.Pos), WorldToScreen(reference.Pos + (Vector2.FromAngle((float)reference.F - (BulletRenderer.DrawnForwardOffset / 2)) * 50)), Colors.DarkRed, 4f);
    }
}