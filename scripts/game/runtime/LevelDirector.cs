using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Godot;
public partial class LevelDirector
{
    public delegate void LevelFinishedEventHandler();
    public event LevelFinishedEventHandler LevelFinished;
    private PlayerCharacter _character;
    private CompiledLevel level;
    private double elapsed;
    public double Elapsed => elapsed;
    EvalContext _ctx = new();
    private bool[] _hasGrazed = new bool[65536];
    private SpatialReference[] activeReferences = new SpatialReference[65536];
    public ref SpatialReference[] ActiveProjectiles => ref activeReferences;
    private int activeCount = 0;
    public int ActiveCount => activeCount;
    public void StartLevel(CompiledLevel level, PlayerCharacter c)
    {
        this.level = level;
        activeCount = 0;
        elapsed = 0;
        _ctx.T = 0;
        _character = c;
    }
    public void Tick(double dt)
    {
        elapsed += dt;
        if (elapsed >= level.Duration)
        {
            LevelFinished?.Invoke();
            return;
        }
        _ctx.T = elapsed;
        // spawn references
        for (int i = level.Queued.Count - 1; i >= 0; i--)
        {
            var r = level.Queued[i];
            if (r.T <= elapsed)
            {
                level.Queued.RemoveAt(i);
                activeReferences[activeCount++] = r;
                if (r.Type == ModelType.Projectile)
                    _hasGrazed[activeCount-1] = false;
            }
        }
        // update projectiles
        for (int i = 0; i < activeCount; i++)
        {
            ref var r = ref activeReferences[i];
            if (r.Type == ModelType.Projectile) TickProjectile(ref r, i);
            if (r.Type == ModelType.Pattern) TickPattern(ref r, i);
        }
    }
    private void TickProjectile(ref SpatialReference r, int i)
    {
        if (r.Type != ModelType.Projectile) return;
        var proj = level.Projectiles[r.Id];
        if (elapsed - r.T > proj.Lifetime)
            Kill(i--, proj);
        else
        {
            _ctx.T = elapsed - r.T;
            _ctx.L = proj.Lifetime;
            // move projectile
            double xTravel = proj.fnx(_ctx);
            double yTravel = proj.fny(_ctx);
            double cos = Math.Cos(r.F);
            double sin = Math.Sin(r.F);
            float x = (float)(cos * xTravel - sin * yTravel);
            float y = (float)(sin * xTravel + cos * yTravel);
            var pos = new Vector2(x,y);
            r.Pos = r.SpawnPos+pos;
            // collision w player
            float distSq = (r.Pos - _character.Position).LengthSquared();
            float rSumH = proj.Radius + PlayerCharacter.HurtRadius;
            float rSumG = proj.Radius + PlayerCharacter.GrazeRadius;
            bool ghit = proj.Shape == null ?
            distSq <= rSumG * rSumG 
            : CollisionUtils.PolygonVsCircle([.. proj.ShapeVect2s], r.Pos, _character.Position, PlayerCharacter.GrazeRadius, (float)r.F-Mathf.Pi);
            if (!_hasGrazed[i] && ghit && !ConfigHelper.Current.NoGraze)
            {
                _character.Graze();
                if (!ConfigHelper.Current.NoGrazeTracking)
                    _hasGrazed[i] = true;
            }
            bool hit = proj.Shape == null ? 
            distSq <= rSumH * rSumH 
            : CollisionUtils.PolygonVsCircle([.. proj.ShapeVect2s], r.Pos, _character.Position, PlayerCharacter.HurtRadius, (float)r.F-Mathf.Pi);
            if (hit && !ConfigHelper.Current.NoHit && _character.Hurt() )
            {
                if (!proj.Persistant)
                    Kill(i--, proj);
            }
        }
    }
    private void TickPattern(ref readonly SpatialReference r, int i)
    {
        if (r.Type != ModelType.Pattern) return;
        var patt = level.Patterns[r.Id];
        var proj = level.Projectiles[patt.ProjectileId];
        double localTime = elapsed - r.T;
        _ctx.L = proj.Lifetime;
        for (int j = 0; j < patt.Count; j++)
        {
            _ctx.I = j;
            // calculate spawn conditions of child
            double t = patt.fnt(_ctx);
            double fwd = patt.fnf(_ctx);
            double xTravel = patt.fnx(_ctx);
            double yTravel = patt.fny(_ctx);
            double cos = Math.Cos(r.F);
            double sin = Math.Sin(r.F);
            float x = (float)(cos * xTravel - sin * yTravel);
            float y = (float)(sin * xTravel + cos * yTravel);
            var pos = new Vector2(x, y);
            // apply them
            var jr = new SpatialReference()
            {
                SpawnPos = r.Pos + pos,
                T = r.T + t,
                F = r.F + fwd,
                Type = ModelType.Projectile,
                Id = patt.ProjectileId,
                Depth = 0
            };
            level.Queued.Add(jr);
        }
        Kill(i);
    }
    private void Kill(int index, ProjectileModel proj = null)
    {
        if (proj != null && proj.SpawnModelOnDeath)
        {
            int maxDepth = proj.MaxDepth;
            ref readonly var r = ref activeReferences[index];
            if (r.Depth < maxDepth)
            {
                var nr = new SpatialReference()
                {
                    SpawnPos = r.Pos,
                    T = elapsed,
                    F = r.F,
                    Type = proj.SpawnOnDeathType,
                    Id = proj.SpawnOnDeathId,
                    Depth = r.Depth+1
                };
                level.Queued.Add(nr);
            }
        }
        activeReferences[index] = activeReferences[activeCount-1];
        activeCount--;
    }
}