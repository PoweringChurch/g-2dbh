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
        // tick background
        for (int i = 0; i < level.BackgroundInstances.Count; i++)
        {
            level.BackgroundInstances[i].Tick(elapsed);
        }
        // spawn references
        for (int i = level.Queued.Count - 1; i >= 0; i--)
        {
            var r = level.Queued[i];
            if (r.T <= elapsed)
            {
                level.Queued.RemoveAt(i);
                if (r.Type == ModelType.Pattern)
                    SpawnPattern(ref r);
                else if (r.Type == ModelType.Projectile)
                    SpawnProjectile(ref r);
                 i = level.Queued.Count;
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
            Kill(i--);
        }
        else
        {
            var lctx = new EvalContext { T = elapsed - r.T, L = proj.Lifetime };
            // move projectile
            var f = MathSafe.Sanitize(proj.fnf(lctx)) + r.SpawnF;
            var pos = CalculatePosition(proj.fnx, proj.fny, f, lctx) + r.SpawnPos;
            r.Pos = pos;
            r.F = f;
            // collision w player
            if (lctx.T <= proj.TelegraphTime || !proj.CanCollide) // check if in telegraph
                return;
            float distSq = (r.Pos - _character.Position).LengthSquared();
            float rSumH = proj.Radius + PlayerCharacter.HurtRadius;
            float rSumG = proj.Radius + PlayerCharacter.GrazeRadius;
            float forward = (float)(proj.LockRotation ? BulletRenderer.DrawnForwardOffset : r.F+BulletRenderer.DrawnForwardOffset);
            bool ghit = !proj.UseShape 
            ? distSq <= rSumG * rSumG 
            : CollisionUtils.PolygonVsCircle([.. proj.Shape], r.Pos, _character.Position, PlayerCharacter.GrazeRadius, forward, true);
            if (!_hasGrazed[i] && ghit && !ConfigHelper.Current.NoGraze)
            {
                _character.Graze();
                if (!ConfigHelper.Current.NoGrazeTracking)
                    _hasGrazed[i] = true;
            }
            bool hit = !proj.UseShape 
            ? distSq <= rSumH * rSumH 
            : CollisionUtils.PolygonVsCircle([.. proj.Shape], r.Pos, _character.Position, PlayerCharacter.HurtRadius, forward, true);
            if (hit && !ConfigHelper.Current.NoHit && _character.Hurt() )
            {
                if (!proj.Persistant)
                    Kill(i--);
            }
        }
    }
    private void SpawnProjectile(ref SpatialReference r)
    {
        // check if cap is hit
        if (activeCount == MaxBulletCount)
            return;
        // spawn children
        var proj = level.Projectiles[r.Id];
        
        for (int j = 0; j < proj.Spawns.Count; j++)
        {
            var childRef = proj.Spawns[j];
            var pctx = new EvalContext { T = childRef.T, L = proj.Lifetime }; // parent context at time of child spawning
            double parentF = r.SpawnF+MathSafe.Sanitize(proj.fnf(pctx));
            var parentPos = CalculatePosition(proj.fnx, proj.fny, parentF, pctx) + r.SpawnPos;

            Vector2 childSpawnPos = parentPos + new Vector2(childRef.SpawnX, childRef.SpawnY);
            double childF = parentF + childRef.SpawnF;
            double childT = MathSafe.Sanitize(childRef.T+r.T);
            level.Queued.Add(new SpatialReference()
            {
                SpawnPos = childSpawnPos,
                Pos = childSpawnPos,
                SpawnF = childF,
                F = childF,
                T = childT,
                Type = childRef.Type,
                Id = childRef.Id,
                Depth = r.Depth + 1
            });
        }
        // face player
        if (proj.FacePlayer)
            r.F = Math.Atan2(_character.Position.Y - r.SpawnPos.Y, _character.Position.X - r.SpawnPos.X)+BulletRenderer.DrawnForwardOffset/2;
        // add projectile
        activeReferences[activeCount++] = r;
        _hasGrazed[activeCount-1] = false;
    }
    private void SpawnPattern(ref SpatialReference r)
    {
        var patt = level.Patterns[r.Id];
        var lctx = new EvalContext {N = patt.Count > 1 ? patt.Count - 1 : 1 };
        if (patt.FacePlayer)
            r.F = Math.Atan2(_character.Position.Y - r.SpawnPos.Y, _character.Position.X - r.SpawnPos.X)+BulletRenderer.DrawnForwardOffset/2;
        for (int j = 0; j < patt.Count; j++)
        {
            lctx.I = j;
            // calculate spawn conditions of child
            double childT = MathSafe.Sanitize(patt.fnt(lctx)) + r.T;
            double childF = MathSafe.Sanitize(patt.fnf(lctx)) + r.F;
            var childPos = CalculatePosition(patt.fnx, patt.fny, r.F, lctx) + r.SpawnPos;
            // add new reference to queue
            level.Queued.Add(new()
            {
                SpawnPos =  childPos,
                Pos = childPos,
                SpawnF = childF,
                F = childF,
                T = childT,
                Type = patt.SpawningType,
                Id = patt.SpawningId,
                Depth = r.Depth
            });
        }
    }
    private void Kill(int index)
    {
        activeReferences[index] = activeReferences[activeCount-1];
        _hasGrazed[index] = _hasGrazed[activeCount-1];
        activeCount--;
    }
    public static Vector2 CalculatePosition(Func<EvalContext, double> efnx, Func<EvalContext, double> efny, double f, EvalContext ctx)
	{
		double xTravel = MathSafe.Sanitize(efnx(ctx));
		double yTravel = MathSafe.Sanitize(efny(ctx));
		double cos = Math.Cos(f);
		double sin = Math.Sin(f);
		float x = (float)(cos * xTravel - sin * yTravel);
		float y = (float)(sin * xTravel + cos * yTravel);
		var pos = new Vector2(x, y);
		return pos;
	}
}