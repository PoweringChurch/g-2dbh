// BulletRenderer.cs
using System;
using Godot;

public class BulletRenderer
{
    public const float DrawnForwardOffset = -Mathf.Pi;
    private readonly CompiledLevel Level;
    private readonly MultiMesh multiMesh;
    private GameSession gs => GameSession.Instance;
    private int __activeCount;
    private double __elapsed;
    private SpatialReference[] __active;
    public BulletRenderer(CompiledLevel level)
    {
        Level = level;
        multiMesh = new MultiMesh
        {
            Mesh = new QuadMesh(),
            TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
            UseColors = true,
            UseCustomData = true,
            InstanceCount = 0,
        };
        var shader = GD.Load<Shader>("res://shaders/projectile_atlas.gdshader");
        var shaderMaterial = new ShaderMaterial();
        shaderMaterial.Shader = shader;
        var node = new MultiMeshInstance2D { 
            Multimesh = multiMesh, 
            Texture = RenderingUtils.GetProjectileAtlas(),
            ShowBehindParent = true,
            Material = shaderMaterial
        };
        gs.GameRoot.AddChild(node);
        gs.GameRoot.Draw += __DrawHitboxes;
    }
    private bool IsOutOfBounds(ref SpatialReference r)
    {
        var proj = Level.Projectiles[r.Id];
        if (proj.Persistant) return false;
        var resolution = PlayingField.Resolutions[Level.AspectRatio];
        var bounds = RenderingUtils.Rects[proj.TextureId].Size*RenderingUtils.AtlasSize;
        bool isOutOfBounds = r.Pos.X < -bounds.X * proj.RenderScale.X
        || r.Pos.X > resolution.X + bounds.X * proj.RenderScale.X
        || r.Pos.Y < -bounds.Y * proj.RenderScale.Y
        || r.Pos.Y > resolution.Y + bounds.Y * proj.RenderScale.Y;
        return isOutOfBounds;
    }
    private float[] buffer = [];
    private const int floatsPerInstance = 16; // 8 transform + 4 color + 4 uv
    public void Sync(ref SpatialReference[] active, int activeCount, double elapsed)
    {
        int culled = 0;
        if (activeCount == 0)
        {
            multiMesh.InstanceCount = 0;
            return;
        }
        int required = activeCount * floatsPerInstance; // upper bound
        if (buffer.Length < required)
            buffer = new float[required];
        int written = 0;
        for (int i = 0; i < activeCount; i++)
        {
            SpatialReference r = active[i];
            if (IsOutOfBounds(ref r)) { culled++; continue; }
            var proj = Level.Projectiles[r.Id];
            var scale = proj.RenderScale;
            double t = elapsed - r.T;
            float alpha = (t < proj.TelegraphTime)
                ? (proj.TelegraphTime > 0 ? 0.4f + (float)t / proj.TelegraphTime * 0.4f : 0.8f)
                : 1.0f;
            float drawForward = !proj.LockRotation ? (float)r.F + DrawnForwardOffset : DrawnForwardOffset;
            float cos = Mathf.Cos(drawForward);
            float sin = Mathf.Sin(drawForward);
            var rect = RenderingUtils.Rects[proj.TextureId];
            var pixelSize = rect.Size*RenderingUtils.AtlasSize;
            int o = written * floatsPerInstance;
            buffer[o + 0] = -scale.X * cos * pixelSize.X;
            buffer[o + 1] = -scale.X * sin * pixelSize.X;
            buffer[o + 2] = 0;
            buffer[o + 3] = r.Pos.X;

            buffer[o + 4] = -scale.Y * sin * pixelSize.Y;
            buffer[o + 5] = scale.Y * cos * pixelSize.Y;
            buffer[o + 6] = 0;
            buffer[o + 7] = r.Pos.Y;

            buffer[o + 8] = 1; buffer[o + 9] = 1; buffer[o + 10] = 1; buffer[o + 11] = alpha;

            
            buffer[o + 12] = rect.Position.X;
            buffer[o + 13] = rect.Position.Y;
            buffer[o + 14] = rect.Size.X;
            buffer[o + 15] = rect.Size.Y;

            written++;
        }
        // safe
        multiMesh.InstanceCount = written;
        if (written > 0)
            RenderingServer.MultimeshSetBuffer(multiMesh.GetRid(), buffer.AsSpan(0, written * floatsPerInstance).ToArray());
        if (Overlay.ShowHitboxes)
        {
            __active = active;
            __elapsed = elapsed;
            __activeCount = activeCount;
            gs.GameRoot.QueueRedraw();
        }
        else if (activeCount > 0)
        {
            __activeCount = 0;
            gs.GameRoot.QueueRedraw();
        }
        Overlay.Inst.SyncInfo(-1, -1, -1, culled);
    }
    const int MaxDisplays = 1<<9;
    private void __DrawHitboxes()
    {
        for (int i = 0; i < Math.Min(__activeCount, MaxDisplays); i++)
        {
            ref SpatialReference r = ref __active[i];
            if (r.Type != ModelType.Projectile) continue;
            var proj = Level.Projectiles[r.Id];
            double t = __elapsed - r.T;
            bool show = (t > proj.TelegraphTime) && proj.CanCollide;
            if (!show) continue;
            float drawForward = (float)(!proj.LockRotation ? r.F + DrawnForwardOffset: DrawnForwardOffset);
            if (proj.UseShape && proj.Shape != null)
            {
                var rotated = CollisionUtils.TranslatePolygon([.. proj.Shape, proj.Shape[0]], r.Pos, drawForward, true, true);
                gs.GameRoot.DrawPolyline(rotated, Colors.Red, 2);
            }
            else gs.GameRoot.DrawCircle(r.Pos, proj.Radius, Colors.Red, false, 2);
        }
    }
}