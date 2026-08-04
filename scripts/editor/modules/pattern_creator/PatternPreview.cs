using System;
using System.Collections.Generic;
using Godot;

public partial class PatternPreview : Control
{
    private struct PendingSpawn
    {
        public Vector2 SpawnPos;
        public double SpawnF;
        public double SpawnT; // spawn time, relative to the preview's own T=0
        public ModelType Type;
        public int Id;
        public int Depth;
        public int PatternDepth; // pattern->pattern chain depth (safety valve only)
    }
    private struct BakedSpawn
    {
        public Vector2 SpawnPos;
        public double SpawnF;
        public double SpawnT;
        public int Id;
    }
    private struct ResolvedSpawn
    {
        public Vector2 Pos;
        public float F;
        public double LocalT;
        public bool Alive;
    }

    private const int MaxBakedSpawns = 1<<12;
    private const int MaxPatternDepth = 32;

    public PatternModel Model;
    [Export] private Control DrawOn;
    [Export] private Button PlaybackToggle;
    [Export] private Button HomeButton;
    [Export] private SpinBox TimeSpin;

    private bool dirty = false;
    private SpatialReference reference;
    private bool panning = false;
    private bool playing = false;

    private float zoom = 1f;
    private Vector2 zoomPan = Vector2.Zero;
    private const float ZoomMin = 0.25f;
    private const float ZoomMax = 4f;
    private const float ZoomStep = 1.1f;

    private double time = 0;

    private readonly List<BakedSpawn> bakedSpawns = new();
    private ResolvedSpawn[] resolved = [];

    private MultiMesh multiMesh;
    private MultiMeshInstance2D mmInst;
    private float[] buffer = [];
    private int written;
    private const int floatsPerInstance = 16; // 8 transform + 4 color + 4 uv

    public override void _Ready()
    {
        time = 0;

        multiMesh = new MultiMesh
        {
            Mesh = new QuadMesh(),
            TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
            UseColors = true,
            UseCustomData = true,
            InstanceCount = 0,
        };
        var shader = GD.Load<Shader>("res://shaders/projectile_atlas.gdshader");
        var shaderMaterial = new ShaderMaterial { Shader = shader };
        mmInst = new MultiMeshInstance2D
        {
            Multimesh = multiMesh,
            Texture = RenderingUtils.GetProjectileAtlas(),
            Material = shaderMaterial,
            ZIndex = -1, // keep sprites behind the hitbox/path overlay drawn on DrawOn
        };
        DrawOn.AddChild(mmInst);

        DrawOn.Draw += DrawPath;
        DrawOn.Draw += DrawHitboxes;
        PlaybackToggle.Pressed += () => { playing = !playing; PlaybackToggle.Text = playing ? "❚❚" : "▶"; };
        TimeSpin.ValueChanged += (v) => { time = v; MarkDirty(); };
        HomeButton.Pressed += Home;
    }

    public void Load(PatternModel m)
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
        else if (@event is InputEventMouseMotion mm && panning)
        {
			reference.SpawnPos += mm.Relative / zoom;
			MarkDirty();
        }
    }

    public override void _Process(double dt)
    {
        if (playing)
        {
            TimeSpin.Value += dt;
        }
        if (dirty)
        {
            Sync();
            dirty = false;
        }
    }

    private void Sync()
    {
        if (Model == null)
            return;

        RebuildSpawnTree();
        ResolveSpawns();
        ApplyViewTransform();
        WriteSpriteInstances();

        DrawOn.QueueRedraw();
    }

    private void ApplyViewTransform()
    {
        mmInst.Position = ViewCenter + zoomPan - ViewCenter * zoom;
        mmInst.Scale = Vector2.One * zoom;
    }

    private void RebuildSpawnTree()
    {
        bakedSpawns.Clear();
        if (Model == null)
            return;

        var queue = new Queue<PendingSpawn>();
        ExpandPatternIntoQueue(Model, reference.SpawnPos, reference.SpawnF, 0, 0, 0, queue);

        while (queue.Count > 0 && bakedSpawns.Count < MaxBakedSpawns)
        {
            var cur = queue.Dequeue();

            if (cur.Type == ModelType.Projectile)
            {
                var pm = Editor.Instance.ProjectileModels[cur.Id];
                if (pm == null) continue;

                bakedSpawns.Add(new BakedSpawn { SpawnPos = cur.SpawnPos, SpawnF = cur.SpawnF, SpawnT = cur.SpawnT, Id = cur.Id });

                if (cur.Depth < pm.MaxDepth && pm.Spawns != null)
                {
                    foreach (var childRef in pm.Spawns)
                    {
                        var pctx = new EvalContext { T = childRef.T, L = pm.Lifetime };
                        double parentF = cur.SpawnF + MathSafe.Sanitize(pm.fnf(pctx));
                        var parentPos = LevelDirector.CalculatePosition(pm.fnx, pm.fny, parentF, pctx) + cur.SpawnPos;
                        Vector2 childSpawnPos = parentPos + new Vector2(childRef.SpawnX, childRef.SpawnY);
                        double childT = cur.SpawnT + MathSafe.Sanitize(childRef.T);
                        double childF = parentF + childRef.SpawnF;

                        queue.Enqueue(new PendingSpawn
                        {
                            SpawnPos = childSpawnPos,
                            SpawnF = childF,
                            SpawnT = childT,
                            Type = childRef.Type,
                            Id = childRef.Id,
                            Depth = cur.Depth + 1,
                            PatternDepth = cur.PatternDepth,
                        });
                    }
                }
            }
            else // Pattern
            {
                if (cur.PatternDepth >= MaxPatternDepth) continue;
                var patt = Editor.Instance.PatternModels[cur.Id];
                if (patt == null) continue;
                ExpandPatternIntoQueue(patt, cur.SpawnPos, cur.SpawnF, cur.SpawnT, cur.Depth, cur.PatternDepth + 1, queue);
            }
        }
    }

    private void ExpandPatternIntoQueue(PatternModel patt, Vector2 basePos, double baseF, double baseT, int depth, int patternDepth, Queue<PendingSpawn> queue)
    {
        int count = Math.Max(patt.Count, 1);
        var lctx = new EvalContext { N = count > 1 ? count - 1 : 1 };
        for (int i = 0; i < count; i++)
        {
            lctx.I = i;
            double genFwd = MathSafe.Sanitize(patt.fnf(lctx));
            double genT = MathSafe.Sanitize(patt.fnt(lctx));
            Vector2 spawnOffset = LevelDirector.CalculatePosition(patt.fnx, patt.fny, (float)baseF, lctx);
            Vector2 spawnPos = basePos + spawnOffset;
            double f = baseF + genFwd;
            double spawnT = baseT + genT;

            queue.Enqueue(new PendingSpawn
            {
                SpawnPos = spawnPos,
                SpawnF = f,
                SpawnT = spawnT,
                Type = patt.SpawningType,
                Id = patt.SpawningId,
                Depth = depth,
                PatternDepth = patternDepth,
            });
        }
    }

    private void ResolveSpawns()
    {
        if (resolved.Length != bakedSpawns.Count)
            resolved = new ResolvedSpawn[bakedSpawns.Count];

        for (int i = 0; i < bakedSpawns.Count; i++)
        {
            var b = bakedSpawns[i];
            var pm = Editor.Instance.ProjectileModels[b.Id];
            if (pm == null)
            {
                resolved[i] = default;
                continue;
            }

            double localTime = time - b.SpawnT;
            double clampedT = Math.Clamp(localTime, 0, pm.Lifetime);
            var lctx = new EvalContext { T = clampedT, L = pm.Lifetime };

            double f = b.SpawnF + MathSafe.Sanitize(pm.fnf(lctx));
            var movement = LevelDirector.CalculatePosition(pm.fnx, pm.fny, (float)f, lctx);

            resolved[i] = new ResolvedSpawn
            {
                Pos = b.SpawnPos + movement,
                F = (float)f,
                LocalT = clampedT,
                Alive = localTime >= 0 && localTime < pm.Lifetime,
            };
        }
    }

    private void WriteSpriteInstances()
    {
        written = 0;

        for (int i = 0; i < bakedSpawns.Count; i++)
        {
            var pm = Editor.Instance.ProjectileModels[bakedSpawns[i].Id];
            if (pm == null) continue;
            var r = resolved[i];
            WriteBodyInstance(pm, r.Pos, r.F, r.Alive, r.LocalT);
        }

        multiMesh.InstanceCount = written;
        if (written > 0)
            RenderingServer.MultimeshSetBuffer(multiMesh.GetRid(), buffer.AsSpan(0, written * floatsPerInstance).ToArray());
        mmInst.QueueRedraw();
    }

    private void WriteBodyInstance(ProjectileModel pm, Vector2 pos, float forward, bool alive, double localT)
    {
        float drawForward = pm.LockRotation ? 0 : forward;

        Color color = pm.Tint;
        float alpha = 1;
        if (pm.TelegraphTime != 0 && alive)
            alpha = (float)Math.Min(localT / pm.TelegraphTime, 1);
        color.A *= alpha;
        if (!alive) color.A *= 0.5f;

        WriteIntoBuffer(pos, pm.RenderScale, drawForward, color, pm.TextureName);
        written++;
    }

    private void WriteIntoBuffer(Vector2 pos, Vector2 scale, float forward, Color color, string textureId)
    {
        int o = written * floatsPerInstance;
        int required = o + floatsPerInstance;
        if (buffer.Length < required)
        {
            int newSize = Math.Max(required, Math.Max(buffer.Length * 2, floatsPerInstance * 16));
            Array.Resize(ref buffer, newSize);
        }
        float cos = Mathf.Cos(forward);
        float sin = Mathf.Sin(forward);

        var rect = RenderingUtils.Rects[textureId];
        var pixelSize = rect.Size * RenderingUtils.AtlasSize;
        buffer[o + 0] = scale.X * cos * pixelSize.X;
        buffer[o + 1] = scale.X * sin * pixelSize.X;
        buffer[o + 2] = 0;
        buffer[o + 3] = pos.X;

        buffer[o + 4] = scale.Y * sin * pixelSize.Y;
        buffer[o + 5] = -scale.Y * cos * pixelSize.Y;
        buffer[o + 6] = 0;
        buffer[o + 7] = pos.Y;

        buffer[o + 8] = color.R;
        buffer[o + 9] = color.G;
        buffer[o + 10] = color.B;
        buffer[o + 11] = color.A;

        buffer[o + 12] = rect.Position.X;
        buffer[o + 13] = rect.Position.Y;
        buffer[o + 14] = rect.Size.X;
        buffer[o + 15] = rect.Size.Y;
    }

    private void DrawPath()
    {
        if (Model == null)
            return;
        int steps = ConfigHelper.Current.PathFidelity;
        if (steps <= 0)
            return;
        Vector2[] pathPoints = new Vector2[steps];
        var lctx = new EvalContext() { N = Model.Count > 1 ? Model.Count - 1 : 1 };
        for (int j = 0; j < steps; j++)
        {
            lctx.I = Math.Min(lctx.N, ConfigHelper.Current.MaxPathLength) / steps * j;
            var (x, y) = LevelDirector.CalculatePosition(Model.fnx, Model.fny, 0, lctx);
            pathPoints[j] = WorldToScreen(reference.SpawnPos + new Vector2(x, y));
        }
        DrawOn.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        DrawOn.DrawPolyline(pathPoints, Colors.Green, ConfigHelper.Current.PathThickness, true);
        DrawOn.DrawCircle(pathPoints[0], ConfigHelper.Current.PathThickness * 1.5f, Colors.Green);
    }

    private void DrawHitboxes()
    {
        for (int i = 0; i < bakedSpawns.Count; i++)
        {
            var pm = Editor.Instance.ProjectileModels[bakedSpawns[i].Id];
            if (pm == null || !pm.CanCollide) continue;

            var r = resolved[i];
            bool show = r.Alive && (r.LocalT >= pm.TelegraphTime);
            if (!show) continue;

            var forward = pm.LockRotation ? 0 : r.F;
            DrawOn.DrawSetTransform(WorldToScreen(r.Pos), forward, Vector2.One * zoom);
            if (pm.UseShape && pm.Shape != null)
                DrawOn.DrawPolyline([.. pm.Shape, pm.Shape[0]], Colors.Red);
            else
                DrawOn.DrawCircle(Vector2.Zero, pm.Radius, Colors.Red, false);
        }
    }
}