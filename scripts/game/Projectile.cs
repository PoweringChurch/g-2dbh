using System;
using Godot;

public class Projectile
{
    public Func<EvalContext, double> fnx;
    public Func<EvalContext, double> fny;
    public double Lifetime = 0;
    public float Radius = 1;
    public bool Persistant = false; 
    public int RenderGroupId = -1;
    public float RenderScale = 1f;
    public Vector2[] Shape = null;
    public bool LockRotation = false;
    public static (float x, float y) CalculatePositionAt(float fwd, Expr fnx, Expr fny, EvalContext ctx)
    {
        float fwdTravel = fnx != null ? (float)fnx.Eval(ctx) : 0;
        float perpTravel = fny != null ? (float)fny.Eval(ctx) : 0;

        float cos = MathF.Cos(fwd);
        float sin = MathF.Sin(fwd);

        float x = cos * fwdTravel - sin * perpTravel;
        float y = sin * fwdTravel + cos * perpTravel;

        return (x, y);
    }
}