using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Projectile : Hitbox
{
    public Vector2 SpawnPosition { get; set; }
    public float Forward { get; set; }
    public bool Persistant { get; set; }
    public float Lifetime { get; set; }
    public Expr MotionFnX { get; set; }
    public Expr MotionFnY { get; set; }
    public Texture2D Texture { get; set; }
    public bool UseShape { get; set; }
    public float ColRadius { get; set; }
    public float[][] ColShape { get; set; }
    private Dictionary<string, double> ctx = new() { ["t"] = 0 };
    public override void _Ready()
    {
        base._Ready();
        Position = CalculatePositionAt(SpawnPosition, Forward, MotionFnX, MotionFnY, ctx);
        HitType = HurtType.Friendly;
        QueueRedraw(); // once
    }
    public override void _PhysicsProcess(double dt)
    {
        ctx["t"] += dt;
        if (ctx["t"] > Lifetime)
            QueueFree();
        Position = CalculatePositionAt(SpawnPosition, Forward, MotionFnX, MotionFnY, ctx);
    }
    public override void _Draw()
    {
        if (Texture != null)
        {
            DrawTexture(Texture, -Texture.GetSize() / 2);
            return;
        }
        if (UseShape && ColShape != null)
        {
            var points = ColShape.Select(p => new Vector2(p[0], p[1])).ToArray();
            DrawPolyline(points, Colors.White, 1.5f, true);
            // close the shape
            if (points.Length > 1)
                DrawLine(points[^1], points[0], Colors.White, 1.5f);
        }
        else
            DrawCircle(Vector2.Zero, ColRadius, Colors.White);
    }
    public static Vector2 CalculatePositionAt(Vector2 pos, float fwdRad, Expr fnx, Expr fny, Dictionary<string, double> ctx)
    {
        var forward = Vector2.FromAngle(fwdRad);
        var perp = new Vector2(-forward.Y, forward.X);
        float fwd = fnx != null ? (float)fnx.Eval(ctx) : 0f;
        float lateral = fny != null ? (float)fny.Eval(ctx) : 0f;
        return pos
            + forward * fwd
            + perp * lateral;
    }
}