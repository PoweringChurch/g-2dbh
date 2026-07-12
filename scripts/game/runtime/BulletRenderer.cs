// BulletRenderer.cs
using System;
using Godot;

public class BulletRenderer
{
    private readonly CompiledLevel level;
    private readonly float[][] groupBuffers;
    private readonly GameSession __gs;
    private int __activeCount;
    private double __elapsed;
    private int __culled = 0;
    private SpatialReference[] __active;
    public BulletRenderer(CompiledLevel level, GameSession gs)
    {
        __gs = gs;
        this.level = level;
        groupBuffers = new float[level.RenderGroups.Count][];
        for (int i = 0; i < groupBuffers.Length; i++)
            groupBuffers[i] = [];
        __gs.GameRoot.Draw += __DrawHitboxes;
    }
    private bool CullRef(ref SpatialReference r)
    {
        var proj = level.Projectiles[r.Id];
        if (proj.Persistant) return false;
        var resolution = PlayingField.Resolutions[level.AspectRatio];
        var bounds = level.RenderGroups[proj.RenderGroupId].Bounds;
        bool isOutOfBounds = r.Pos.X < -bounds.X 
        || r.Pos.X > resolution.X + bounds.X 
        || r.Pos.Y < -bounds.Y 
        || r.Pos.Y > resolution.Y + bounds.Y;
        if (isOutOfBounds) __culled++;
        return isOutOfBounds;
    }
    public void Sync(ref SpatialReference[] active, int activeCount, double elapsed)
    {
        __culled = 0;
        var groups = level.RenderGroups;
        for (int g = 0; g < groups.Count; g++)
            groups[g].BakeIndices.Clear();
        for (int i = 0; i < activeCount; i++)
        {
            ref SpatialReference r = ref active[i];
            if (r.Type != ModelType.Projectile) continue;
            var proj = level.Projectiles[r.Id];
            if (!CullRef(ref r))
                groups[proj.RenderGroupId].BakeIndices.Add(i);
        }
        const int floatsPerInstance = 12;
        for (int g = 0; g < groups.Count; g++)
        {
            var group = groups[g];
            int count = group.BakeIndices.Count;
            group.MultiMesh.InstanceCount = count;
            if (count == 0) continue;
            int required = count * floatsPerInstance;
            if (groupBuffers[g].Length != required)
                groupBuffers[g] = new float[required];
            ref float[] buffer = ref groupBuffers[g];
            for (int n = 0; n < count; n++)
            {
                int idx = group.BakeIndices[n];
                ref SpatialReference r = ref active[idx];
                var proj = level.Projectiles[r.Id];
                float scale = proj.RenderScale;
                int o = n * floatsPerInstance;
                
                double t = elapsed - r.T;
                float alpha = (t < proj.TelegraphTime) 
					? (proj.TelegraphTime > 0 ? 0.4f + (float)t / proj.TelegraphTime * 0.4f : 0.8f) 
					: 1.0f;
                float drawForward = !proj.LockRotation ? (float)r.F-Mathf.Pi : -Mathf.Pi; // rads

				float cos = Mathf.Cos(drawForward);
				float sin = Mathf.Sin(drawForward);

                buffer[o + 0] = -scale * cos; // shear x
                buffer[o + 1] = -scale * sin; // scale x
                buffer[o + 2] = 0; // dont know dont care x
                buffer[o + 3] = r.Pos.X; // x

                buffer[o + 4] = -scale * sin; // scale y
                buffer[o + 5] = scale * cos; // shear y
                buffer[o + 6] = 0; // dont know dont care y
                buffer[o + 7] = r.Pos.Y; // y

                buffer[o + 8] = 1; buffer[o + 9] = 1; buffer[o + 10] = 1; buffer[o + 11] = alpha;
            }
            RenderingServer.MultimeshSetBuffer(group.MultiMesh.GetRid(), buffer);
        }
        if (Overlay.ShowHitboxes)
        {
            __active = active;
            __elapsed = elapsed;
            __activeCount = activeCount;
            __gs.GameRoot.QueueRedraw();
        }
        else if (activeCount > 0)
        {
            __activeCount = 0;
            __gs.GameRoot.QueueRedraw();
        }
        Overlay.Inst.SyncInfo(-1, -1, -1, __culled);
    }
    const int MaxDisplays = 1<<9;
    private void __DrawHitboxes()
    {
        for (int i = 0; i < Math.Min(__activeCount, MaxDisplays); i++)
        {
            ref SpatialReference r = ref __active[i];
            if (r.Type != ModelType.Projectile) continue;
            var proj = level.Projectiles[r.Id];
            double t = __elapsed - r.T;
            bool show = (t > proj.TelegraphTime) && proj.CanCollide;
            if (!show) continue;
            if (proj.UseShape && proj.Shape != null)
            {
                var rotated = CollisionUtils.TranslatePolygon([.. proj.ShapeVect2s, proj.ShapeVect2s[0]], r.Pos, (float)r.F);
                __gs.GameRoot.DrawPolyline(rotated, Colors.Red, 2);
            }
            else __gs.GameRoot.DrawCircle(r.Pos, proj.Radius, Colors.Red, false, 2);
        }
    }
}