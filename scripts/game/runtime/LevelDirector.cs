using System;
using Godot;
public partial class LevelDirector
{
    public const int MaxBulletCount = 1 << 16; // 2^16
    public delegate void LevelFinishedEventHandler();
    public event LevelFinishedEventHandler LevelFinished;
    private PlayerCharacter _character;
    private CompiledLevel level;
    private float speedMultiplier;
    private double elapsed;
    public double Elapsed => elapsed;
    EvalContext _ctx = new();
    private bool[] _hasGrazed = new bool[MaxBulletCount];
    private SpatialReference[] activeReferences = new SpatialReference[MaxBulletCount];
    public ref SpatialReference[] ActiveProjectiles => ref activeReferences;
    private int activeCount = 0;
    public int ActiveCount => activeCount;
    public int QueuedCount => (level != null) ? level.Queued.Count : 0;
    public void StartLevel(CompiledLevel level, PlayerCharacter c, float mult, float startDelay = 0)
    {
        this.level = level;
        activeCount = 0;
        elapsed = -startDelay;
        _ctx.T = elapsed;
        _character = c;
        speedMultiplier = mult;
    }
    public void Tick(double dt)
    {
        elapsed += dt*speedMultiplier;
        if (elapsed >= level.Duration)
        {
            activeCount = 0;
            LevelFinished?.Invoke();
            return;
        }
        _ctx.T = elapsed;
        // tick background
        for (int i = 0; i < level.BackgroundInstances.Count; i++)
        {
            level.BackgroundInstances[i].Tick(_ctx);
        }
        // spawn references
        for (int i = level.Queued.Count - 1; i >= 0; i--)
        {
            var r = level.Queued[i];
            if (r.T <= elapsed)
            {
                level.Queued.RemoveAt(i);
                if (r.Type == ModelType.Pattern)
                {
                    TickPattern(ref r);
                    i = level.Queued.Count;
                }
                else if (r.Type == ModelType.Projectile)
                {
                    if (level.Projectiles[r.Id].FacePlayer)
                        r.F = Math.Atan2(_character.Position.Y - r.SpawnPos.Y, _character.Position.X - r.SpawnPos.X)-Mathf.Pi/2;
                    if (activeCount == MaxBulletCount)
                        continue;
                    activeReferences[activeCount++] = r;
                    _hasGrazed[activeCount-1] = false;
                }
                
            }
        }
        // update projectiles
        for (int i = 0; i < activeCount; i++)
        {
            ref var r = ref activeReferences[i];
            if (r.Type == ModelType.Projectile) TickProjectile(ref r, i);
        }
    }
    private void TickProjectile(ref SpatialReference r, int i)
    {
        if (r.Type != ModelType.Projectile) return;
        var proj = level.Projectiles[r.Id];
        if (elapsed - r.T > proj.Lifetime)
        {
            Kill(i--, proj);
        }
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
            if (_ctx.T <= proj.TelegraphTime || !proj.CanCollide) // check if in telegraph
                return;
            float distSq = (r.Pos - _character.Position).LengthSquared();
            float rSumH = proj.Radius + PlayerCharacter.HurtRadius;
            float rSumG = proj.Radius + PlayerCharacter.GrazeRadius;
            bool ghit = proj.ShapeVect2s == null ?
            distSq <= rSumG * rSumG 
            : CollisionUtils.PolygonVsCircle([.. proj.ShapeVect2s], r.Pos, _character.Position, PlayerCharacter.GrazeRadius, (float)r.F-Mathf.Pi, true);
            if (!_hasGrazed[i] && ghit && !ConfigHelper.Current.NoGraze)
            {
                _character.Graze();
                if (!ConfigHelper.Current.NoGrazeTracking)
                    _hasGrazed[i] = true;
            }
            bool hit = proj.ShapeVect2s == null ? 
            distSq <= rSumH * rSumH 
            : CollisionUtils.PolygonVsCircle([.. proj.ShapeVect2s], r.Pos, _character.Position, PlayerCharacter.HurtRadius, (float)r.F-Mathf.Pi, true);
            if (hit && !ConfigHelper.Current.NoHit && _character.Hurt() )
            {
                if (!proj.Persistant)
                    Kill(i--, proj);
            }
        }
    }
    private void TickPattern(ref SpatialReference r)
    {
        var patt = level.Patterns[r.Id];
        var proj = level.Projectiles[patt.ProjectileId];
        _ctx.L = proj.Lifetime;
        _ctx.N = patt.Count > 1 ? patt.Count - 1 : 1;
        if (patt.FacePlayer)
            r.F = Math.Atan2(_character.Position.Y - r.SpawnPos.Y, _character.Position.X - r.SpawnPos.X)-Mathf.Pi/2;
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
            // add new projectile to queue
            level.Queued.Add(new()
            {
                SpawnPos = r.SpawnPos + pos,
                Pos = r.SpawnPos + pos,
                T = r.T + t,
                F = r.F + fwd,
                Type = ModelType.Projectile,
                Id = patt.ProjectileId,
                Depth = r.Depth
            });
        }
    }
    private void Kill(int index, ProjectileModel proj = null)
    {
        if (proj != null && proj.SpawnModelOnDeath)
        {
            ref readonly var r = ref activeReferences[index];
            if (r.Depth < proj.MaxDepth)
            {
                var nr = new SpatialReference()
                {
                    SpawnPos = r.Pos,
                    Pos = r.Pos,
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
        _hasGrazed[index] = _hasGrazed[activeCount-1];
        activeCount--;
    }
}