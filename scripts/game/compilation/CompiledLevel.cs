using System.Collections.Generic;
using Godot;
public class CompiledLevel
{
    public List<SpatialReference> Queued = new();
    public List<BackgroundLayerInstance> BackgroundInstances = new();
    public ProjectileModel[] Projectiles = new ProjectileModel[Editor.MaxModelCount];
    public PatternModel[] Patterns = new PatternModel[Editor.MaxModelCount];
    public double Duration;
    public int AspectRatio;
}