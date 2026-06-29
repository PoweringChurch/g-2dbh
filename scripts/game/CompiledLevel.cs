using System.Collections.Generic;
using Godot;

public class CompiledLevel
{
    public List<Bullet> Queue = new();
    public List<RenderGroup> RenderGroups = new();
    public Projectile[] Projectiles = new Projectile[Editor.MaxModelCount];
    public Pattern[] Patterns = new Pattern[Editor.MaxModelCount];
    public Node2D GameRoot;
    public double Duration;
}