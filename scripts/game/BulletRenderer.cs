// BulletRenderer.cs
using System;
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

    public void Sync(Bullet[] bullets, int bulletCount)
    {
        var groups = level.RenderGroups;

        for (int g = 0; g < groups.Count; g++)
            groups[g].BulletIndices.Clear();

        for (int i = 0; i < bulletCount; i++)
        {
            ref Bullet b = ref bullets[i];
            //if (!b.Alive) continue;
            var model = level.Projectiles[b.ProjectileId];
            groups[model.RenderGroupId].BulletIndices.Add(i);
        }
    
        const int floatsPerInstance = 12; // 8 transform + 4 color (RGBA)

        for (int g = 0; g < groups.Count; g++)
        {
            var group = groups[g];
            int count = group.BulletIndices.Count;
            group.MultiMesh.InstanceCount = count;
            if (count == 0) continue;

            int required = count * floatsPerInstance;
            if (groupBuffers[g].Length != required)
                groupBuffers[g] = new float[required];
            var buffer = groupBuffers[g];
                GD.Print($"[Group {g}] Buffer Size: {buffer.Length} floats for {count} instances.");
                // Print the raw float block of the very first bullet to check alignment
                GD.Print($" -> First Bullet Raw Data: " +
                        $"Transform[{buffer[0]}, {buffer[1]}, {buffer[2]}, {buffer[3]}, {buffer[4]}, {buffer[5]}, {buffer[6]}, {buffer[7]}] " +
                        $"Color[{buffer[8]}, {buffer[0]}, {buffer[10]}, {buffer[11]}]");
            for (int n = 0; n < count; n++)
            {
                int idx = group.BulletIndices[n];
                ref Bullet b = ref bullets[idx];
                var model = level.Projectiles[b.ProjectileId];
                float scale = model.RenderScale;
                GD.Print(b.Pos);
                int o = n * floatsPerInstance;
                buffer[o + 0] = scale; // X.x
                buffer[o + 1] = 0;     // X.y
                buffer[o + 2] = 0;     // Y.x
                buffer[o + 3] = scale; // Y.y
                buffer[o + 4] = b.Pos.X; // Origin.x
                buffer[o + 5] = b.Pos.Y; // Origin.y

                buffer[o + 6] = 0; 
                buffer[o + 7] = 0;

                buffer[o + 8] = 1; buffer[o + 9] = 1; buffer[o + 10] = 1; buffer[o + 11] = 1;
                /*
                buffer[o + 0] = scale; buffer[o + 1] = 0; buffer[o + 2] = 0;
                buffer[o + 3] = 0; buffer[o + 4] = scale; buffer[o + 5] = 0;
                buffer[o + 6] = b.Pos.X; buffer[o + 7] = b.Pos.Y;
                buffer[o + 8] = c.R; buffer[o + 9] = c.G; buffer[o + 10] = c.B; buffer[o + 11] = c.A;
                */
            }

            RenderingServer.MultimeshSetBuffer(group.MultiMesh.GetRid(), buffer);
        }
    }
}