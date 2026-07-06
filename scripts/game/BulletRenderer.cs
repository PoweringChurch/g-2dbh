// BulletRenderer.cs
using Godot;

public class BulletRenderer
{
    private CompiledLevel level;
    private float[][] groupBuffers;

    public BulletRenderer(CompiledLevel level)
    {
        this.level = level;
        groupBuffers = new float[level.RenderGroups.Count][];
        for (int i = 0; i < groupBuffers.Length; i++)
            groupBuffers[i] = [];
    }

    public void Sync(ref Bullet[] bullets, int bulletCount)
    {
        var groups = level.RenderGroups;

        for (int g = 0; g < groups.Count; g++)
            groups[g].BulletIndices.Clear();

        for (int i = 0; i < bulletCount; i++)
        {
            ref Bullet b = ref bullets[i];
            var model = level.Projectiles[b.Id];
            groups[model.RenderGroupId].BulletIndices.Add(i);
        }
        const int floatsPerInstance = 8;
        for (int g = 0; g < groups.Count; g++)
        {
            var group = groups[g];
            int count = group.BulletIndices.Count;
            group.MultiMesh.InstanceCount = count;
            if (count == 0) continue;
            int required = count * floatsPerInstance;
            if (groupBuffers[g].Length != required)
                groupBuffers[g] = new float[required];
            ref float[] buffer = ref groupBuffers[g];
            for (int n = 0; n < count; n++)
            {
                int idx = group.BulletIndices[n];
                ref Bullet b = ref bullets[idx];
                var model = level.Projectiles[b.Id];
                float scale = model.RenderScale;
                int o = n * floatsPerInstance;

                float drawForward = (float)b.F-Mathf.Pi; // rads

				float cos = Mathf.Cos(drawForward);
				float sin = Mathf.Sin(drawForward);

                buffer[o + 0] = -scale * cos; // shear x
                buffer[o + 1] = -scale * sin; // scale x
                buffer[o + 2] = 0; // dont know dont care x
                buffer[o + 3] = b.Pos.X; // x

                buffer[o + 4] = -scale * sin; // scale y
                buffer[o + 5] = scale * cos; // shear y
                buffer[o + 6] = 0; // dont know dont care y
                buffer[o + 7] = b.Pos.Y; // y
            }
            RenderingServer.MultimeshSetBuffer(group.MultiMesh.GetRid(), buffer);
        }
    }
}