using System;
using Godot;
public class Pattern
{
    public Func<EvalContext, double> fnx; // points to a compiled function in level data
    public Func<EvalContext, double> fny;
    public Func<EvalContext, double> fnt;
    public Func<EvalContext, double> fnfwd;
    public int Count = 0;
    public int ProjectileId = -1; // what projectiles to spawn
}