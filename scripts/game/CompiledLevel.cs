using System.Collections.Generic;
using Godot;

public class CompiledLevel
{
    public List<Bullet> Queue = new();
    public List<RenderGroup> RenderGroups = new();
    public List<Projectile> Projectiles = new();
    public List<Pattern> Patterns = new();
    public Node2D GameRoot;
    public double Duration;
}