using System;
using System.Diagnostics.Tracing;
using Godot;
public partial class LevelDirector
{
    public delegate void LevelFinishedEventHandler();
    public event LevelFinishedEventHandler LevelFinished;
    private PlayerCharacter _character;
    private CompiledLevel _level;
    private double elapsed;
    public double Elapsed => elapsed;
    EvalContext _ctx = new();
    private bool[] _hasGrazed = new bool[65536];
    private Bullet[] bullets = new Bullet[65536];
    public ref Bullet[] Bullets => ref bullets;
    private int bulletCount = 0;
    public int BulletCount => bulletCount;
    public void StartLevel(CompiledLevel level, PlayerCharacter c)
    {
        _level = level;
        bulletCount = 0;
        elapsed = 0;
        _ctx.T = 0;
        _character = c;
    }
    public void Tick(double dt)
    {
        elapsed += dt;
        if (elapsed >= _level.Duration)
        {
            LevelFinished?.Invoke();
            return;
        }
        _ctx.T = elapsed;
        // spawn queue
        while (_level.Queue.Count > 0 && _level.Queue[0].T <= elapsed)
        {
            bullets[bulletCount++] = _level.Queue[0];
            _hasGrazed[bulletCount] = false;
            _level.Queue.RemoveAt(0);
        }
        // update projectiles
        for (int i = 0; i < bulletCount; i++)
        {
            ref var b = ref bullets[i];
            var proj = _level.Projectiles[b.ProjectileId];
            if (elapsed - b.T > proj.Lifetime)
                Kill(i--);
            else
            {
                _ctx.T = elapsed - b.T;
                // move projectile
                double xTravel = proj.fnx(_ctx);
                double yTravel = proj.fny(_ctx);
                double cos = Math.Cos(b.F);
                double sin = Math.Sin(b.F);
                float x = (float)(cos * xTravel - sin * yTravel);
                float y = (float)(sin * xTravel + cos * yTravel);
                var pos = new Vector2(x,y);
                b.Pos = b.SpawnPos+pos;
                // collision w player
                float distSq = (b.Pos - _character.Position).LengthSquared();
                float rSumH = proj.Radius + PlayerCharacter.HurtRadius;
                float rSumG = proj.Radius + PlayerCharacter.GrazeRadius;
                if (!_hasGrazed[i] && distSq <= rSumG * rSumG)
                {
                    _character.Graze();
                    _hasGrazed[i] = true;
                }
                bool hit = proj.Shape == null ?  distSq <= rSumH * rSumH : CollisionUtils.PolygonVsCircle(proj.Shape, b.Pos, _character.Position, PlayerCharacter.HurtRadius);
                if (hit && _character.Hurt())
                {
                    {
                        if (!proj.Persistant)
                        Kill(i--);
                    }
                }
            }
        }
    }
    private void Kill(int index)
    {
        bullets[index] = bullets[bulletCount-1];
        bulletCount--;
    }
}