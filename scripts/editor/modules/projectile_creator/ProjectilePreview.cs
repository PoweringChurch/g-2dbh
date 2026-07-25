using System;
using Godot;

public partial class ProjectilePreview : Control
{
    private struct SpawnInstance
	{
		public Vector2 Pos;
		public float F;
		public double T;
		public bool Alive;
		public ModelType Type;
		public int Id;
	}
	private SpawnInstance[] instances = [];
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
        DrawOn.Draw += DrawGizmos;
        DrawOn.Draw += DrawChildShapes;
        DrawOn.Draw += DrawChildHitboxes;
        DrawOn.Draw += DrawProjectileShape;
        DrawOn.Draw += DrawHitbox;
        PlaybackToggle.Pressed += () => { playing = !playing; PlaybackToggle.Text = playing ? "❚❚" : "▶"; };
        TimeSpin.ValueChanged += (v) => { ctx.T = v; MarkDirty(); };
        HomeButton.Pressed += Home;
    }
    public void Load(ProjectileModel m) { Model = m; Home(); }
    public void MarkDirty() =>
        dirty = true;
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

        SyncChildren();

        DrawOn.QueueRedraw();
    }
    private void SyncChildren()
    {
        int count = Model.Spawns?.Count ?? 0;
        if (instances.Length != count)
            instances = new SpawnInstance[count];

        for (int i = 0; i < count; i++)
        {
            var childRef = Model.Spawns[i];
            // parent (this projectile) context at the moment the child spawns
            var pctx = new EvalContext { T = childRef.T, L = Model.Lifetime };
            double parentF = reference.SpawnF + MathSafe.Sanitize(Model.fnf(pctx));
            var parentPos = LevelDirector.CalculatePosition(Model.fnx, Model.fny, parentF, pctx) + reference.SpawnPos;
            Vector2 childSpawnPos = parentPos + new Vector2(childRef.SpawnX, childRef.SpawnY);
            double childT = MathSafe.Sanitize(childRef.T);
            double childF = parentF + childRef.SpawnF;

            bool isProj = childRef.Type == ModelType.Projectile;
            double lifetime = isProj
                ? Editor.Instance.ProjectileModels[childRef.Id].Lifetime
                : Editor.Instance.PatternModels[childRef.Id].lifetime;

            double localTime = ctx.T - childT;
            float f = (float)childF;
            Vector2 movement = Vector2.Zero;
            double resolvedT;

            if (isProj)
            {
                var cproj = Editor.Instance.ProjectileModels[childRef.Id];
                var lctx = new EvalContext { T = Math.Clamp(localTime, 0, lifetime), L = lifetime };
                movement = LevelDirector.CalculatePosition(cproj.fnx, cproj.fny, f, lctx);
                resolvedT = lctx.T;
            }
            else
            {
                resolvedT = localTime;
            }

            bool alive = localTime > 0 && localTime < lifetime;
            instances[i] = new SpawnInstance
            {
                Pos = childSpawnPos + movement,
                F = f,
                T = resolvedT,
                Alive = alive,
                Type = childRef.Type,
                Id = childRef.Id
            };
        }
    }
    private void DrawChildShapes()
    {
        if (Model == null || Model.Spawns == null)
            return;
        foreach (var inst in instances)
        {
            if (inst.Type == ModelType.Projectile)
            {
                var proj = Editor.Instance.ProjectileModels[inst.Id];
                if (proj == null) continue;
                var texture = RenderingUtils.LoadTexture(LevelCompiler.ProjectileTextures[proj.TextureId].TexturePath);
                var color = Colors.White;
                if (!inst.Alive) color.A *= 0.5f;
                var forward = proj.LockRotation ? 0 : inst.F;
                DrawOn.DrawSetTransform(WorldToScreen(inst.Pos), forward, Vector2.One * proj.RenderScale * zoom);
                if (texture != null)
                {
                    DrawOn.DrawTexture(texture, -texture.GetSize() / 2, color);
                    continue;
                }
                if (proj.UseShape && proj.Shape != null)
                    DrawOn.DrawColoredPolygon([.. proj.Shape, proj.Shape[0]], color);
                else
                    DrawOn.DrawCircle(Vector2.Zero, proj.Radius, color);
            }
            else
            {
                var patt = Editor.Instance.PatternModels[inst.Id];
                if (patt == null) continue;
                var color = RenderingUtils.ColorFromString(patt.Name); color.A = 0.8f;
                if (!inst.Alive) color.A *= 0.5f;
                DrawOn.DrawSetTransform(WorldToScreen(inst.Pos), inst.F, Vector2.One * zoom);
                DrawOn.DrawCircle(Vector2.Zero, 10, color);
                DrawOn.DrawDashedLine(Vector2.Zero, Vector2.Up, color);
            }
        }
    }
    private void DrawChildHitboxes()
    {
        if (Model == null || Model.Spawns == null)
            return;
        foreach (var inst in instances)
        {
            if (inst.Type != ModelType.Projectile)
                continue;
            var pm = Editor.Instance.ProjectileModels[inst.Id];
            if (pm == null || !pm.CanCollide)
                continue;
            bool show = inst.Alive && (inst.T > pm.TelegraphTime);
            if (!show) continue;
            var forward = pm.LockRotation ? 0 : inst.F;
            DrawOn.DrawSetTransform(WorldToScreen(inst.Pos), forward, Vector2.One * zoom);
            if (pm.UseShape && pm.Shape != null)
                DrawOn.DrawPolyline([.. pm.Shape, pm.Shape[0]], Colors.Red);
            else
                DrawOn.DrawCircle(Vector2.Zero, pm.Radius, Colors.Red, false);
        }
    }
    private void DrawHitbox()
    {
        if (Model == null)
            return;
        bool alive = ctx.T >= 0 && ctx.T < ctx.L;
        bool show = Model.CanCollide && (ctx.T >= Model.TelegraphTime) && alive;
        if (!show) return;
        var forward = Model.LockRotation ? 0 : (float)reference.F;
        DrawOn.DrawSetTransform(WorldToScreen(reference.Pos), forward, Vector2.One * zoom);
        if (Model.UseShape && Model.Shape != null)
        {
            DrawOn.DrawPolyline([.. Model.Shape, Model.Shape[0]], Colors.Red);
        }
        else
            DrawOn.DrawCircle(Vector2.Zero, Model.Radius, Colors.Red, false);
    }
    private void DrawProjectileShape()
    {
        if (Model == null)
            return;
        bool alive = ctx.T >= 0 && ctx.T < ctx.L;
        var color = Colors.White;
        if (!alive) color.A *= 0.5f;
        var forward = Model.LockRotation ? 0 : (float)reference.F;
        DrawOn.DrawSetTransform(WorldToScreen(reference.Pos), forward, Vector2.One * Model.RenderScale * zoom);
        var texture = RenderingUtils.LoadTexture(LevelCompiler.ProjectileTextures[Model.TextureId].TexturePath);
        if (texture != null)
        {
            DrawOn.DrawTexture(texture, -texture.GetSize() / 2, color);
            return;
        }
        if (Model.UseShape && Model.Shape != null)
        {
            DrawOn.DrawColoredPolygon([.. Model.Shape, Model.Shape[0]], color);
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