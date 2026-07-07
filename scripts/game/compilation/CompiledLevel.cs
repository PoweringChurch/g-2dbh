using System.Collections.Generic;
using Godot;

public class CompiledLevel
{
    public List<SpatialReference> Queued = new();
    public List<RenderGroup> RenderGroups = new();
    public ProjectileModel[] Projectiles = new ProjectileModel[Editor.MaxModelCount];
    public PatternModel[] Patterns = new PatternModel[Editor.MaxModelCount];
    public double Duration;
}