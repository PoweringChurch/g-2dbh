using System;
using Godot;
public partial class LevelDirector
{
    private PlayerCharacter _character;
    private CompiledLevel _level;
    private Bullet[] bullets = new Bullet[4096];
    public Bullet[] Bullets => bullets;
    private double _elapsed;
    EvalContext _ctx = new();
    private int bulletCount = 0;
    public int BulletCount => bulletCount;
    public void StartLevel(CompiledLevel level, PlayerCharacter c)
    {
        _level = level;
        _elapsed = 0;
        _ctx.T = 0;
        _character = c;
    }
    public void Tick(double dt)
    {
        _elapsed += dt;
        _ctx.T = _elapsed;
        // spawn queue
        while (_level.Queue.Count > 0 && _level.Queue[0].T <= _elapsed)
        {
            GD.Print($"adding bullet @{_level.Queue[0].SpawnPos}");
            bullets[bulletCount++] = _level.Queue[0];
            _level.Queue.RemoveAt(0);
        }
        return; // remove soon
        // update projectiles
        for (int i = 0; i < bulletCount; i++)
        {
            ref var b = ref bullets[i];
            var proj = _level.Projectiles[b.ProjectileId];
            if (_elapsed - b.T > proj.Lifetime)
                Kill(i--);
            else
            {
                // move projectile
                double fwdTravel = proj.fnx(_ctx);
                double perpTravel = proj.fny(_ctx);
                double cos = Math.Cos(b.F);
                double sin = Math.Sin(b.F);
                float x = (float)(cos * fwdTravel - sin * perpTravel);
                float y = (float)(cos * fwdTravel - sin * perpTravel);
                var pos = new Vector2(x,y);
                b.Pos = b.SpawnPos+pos;
                // collision w player
                float distSq = (b.Pos - _character.Position).LengthSquared();
                float rSumH = proj.Radius + PlayerCharacter.HurtRadius;
                float rSumG = proj.Radius + PlayerCharacter.GrazeRadius;
                if (distSq <= rSumG * rSumG)
                    _character.Graze();
                bool hit = proj.Shape == null ?  distSq > rSumH * rSumH : CollisionUtils.PolygonVsCircle(proj.Shape, b.Pos, _character.Position, PlayerCharacter.HurtRadius);
                if (hit && _character.Hurt())
                {
                    GD.Print($"distSq : {distSq}, projRadius : {proj.Radius}, b.Pos : {b.Pos}");
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