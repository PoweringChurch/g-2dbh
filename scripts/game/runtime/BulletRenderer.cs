// BulletRenderer.cs
using Godot;

public class BulletRenderer
{
    private readonly CompiledLevel level;
    private float[][] groupBuffers;

    public BulletRenderer(CompiledLevel level)
    {
        this.level = level;
        groupBuffers = new float[level.RenderGroups.Count][];
        for (int i = 0; i < groupBuffers.Length; i++)
            groupBuffers[i] = [];
    }

    public void Sync(ref SpatialReference[] active, int activeCount, double elapsed)
    {
        var groups = level.RenderGroups;
        for (int g = 0; g < groups.Count; g++)
            groups[g].BakeIndices.Clear();

        for (int i = 0; i < activeCount; i++)
        {
            ref SpatialReference r = ref active[i];
            if (r.Type != ModelType.Projectile) continue;
            var model = level.Projectiles[r.Id];
            groups[model.RenderGroupId].BakeIndices.Add(i);
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
                float dim = (t <= proj.TelegraphTime) 
					? (proj.TelegraphTime > 0 ? 0.4f + (float)t / proj.TelegraphTime * 0.4f : 0.8f) 
					: 1.0f;
                float drawForward = (float)r.F-Mathf.Pi; // rads

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

                buffer[o + 8] = dim; buffer[o + 9] = dim; buffer[o + 10] = dim; buffer[o + 11] = 1;
            }
            RenderingServer.MultimeshSetBuffer(group.MultiMesh.GetRid(), buffer);
        }
    }
}