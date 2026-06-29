using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class PatternPreview : Node2D
{
    // display baked
    static Color deadColor = new(1, 1, 1, 0.3f);
    private ProjectileModel projModel;
    public ProjectileModel ProjModel
    {
        get => projModel;
        set { projModel = value; _dirty = true; }
    }
    private Expr fnX;
    public Expr FnX
    {
        get => fnX;
        set { fnX = value; _dirty = true; }
    }
    private Expr fnY;
    public Expr FnY
    {
        get => fnY;
        set { fnY = value; _dirty = true; }
    }
    private Expr fnT;
    public Expr FnT
    {
        get => fnT;
        set { fnT = value; _dirty = true; }
    }
    private Expr fnFwd;
    public Expr FnFwd
    {
        get => fnFwd;
        set { fnFwd = value; _dirty = true; }
    }
    private Expr projModelFnX;
    public Expr ProjModelFnX
    {
        get => projModelFnX;
        set { projModelFnX = value; _dirty = true; }
    }
    private Expr projModelFnY;
    public Expr ProjModelFnY
    {
        get => projModelFnY;
        set { projModelFnY = value; _dirty = true; }
    }
    private int count = 1;
    public int Count
    {
        get => count;
        set
        {
            count = value;
            _dirty = true;
        }
    }
    private EvalContext ctx = new() { T = 0 };
    public double T
    {
        get => ctx.T;
        set
        {
            ctx.T = Math.Max(value, 0);
            _dirty = true;
        }
    }
    private Editor e;
    public override void _Ready() =>
        e = GetNode<Editor>("/root/Editor");
    private bool _dirty = true;
    public override void _Process(double delta)
    {
        if (_dirty)
        {
            QueueRedraw();
            _dirty = false;
        }
    }
    public override void _Draw()
    {
        if (projModel == null)
            return;
        EvalContext lctx = new() { N = count > 1 ? count-1 : 1 }; // live ctx, stores i, n, projectile t
        // loop through count and draw a projectile for i in count
        for (int i = 0; i < Count; i++)
        {
            lctx.I = i;
            // generate values
            double genFwd = fnFwd != null ? fnFwd.Eval(lctx) : 0;
            double genT = fnT != null ? fnT.Eval(lctx) : 0;
            var startxy = Projectile.CalculatePositionAt(0, fnX, fnY, lctx);
            double rawT = ctx.T - genT;
            bool alive = rawT >= 0 && rawT < projModel.Lifetime;
            lctx.T = Math.Clamp(rawT, 0, projModel.Lifetime);
            var (x, y) = Projectile.CalculatePositionAt((float)genFwd, projModelFnX, projModelFnY, lctx);
            // draw
            var texture = projModel.Texture != "default" ?
                RenderingUtils.LoadTexture(e.LevelPath + "images/", projModel.Texture)
                : null;
            DrawProjectileShape(projModel, texture, new Vector2(startxy.x + x, startxy.y+y), (float)genFwd, alive ? Colors.White : deadColor);
        }
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        DrawPath(fnX, fnY, lctx, 0,  Colors.Green);
    }
    private void DrawProjectileShape(ProjectileModel model,
    Texture2D texture, Vector2 pos,
    float forward, Color color)
    {
        DrawSetTransform(pos, forward, Vector2.One);
        if (texture != null)
        {
            DrawTexture(texture, -texture.GetSize() / 2, color);
            return;
        }
        if (model.UseShape && model.Shape != null)
        {
            var points = model.Shape.Select(p => new Vector2(p[0], p[1])).ToArray();
            DrawPolyline(points, color, 1.5f, true);
            if (points.Length > 1)
                DrawLine(points[^1], points[0], color, 1.5f);
        }
        else
            DrawCircle(Vector2.Zero, model.Radius, color);
    }
    private void DrawPath(Expr fnx, Expr fny,
    EvalContext pctx, double fwd,
    Color color)
    {
        int steps = ConfigHelper.Current.PathFidelity;
        Vector2[] points = new Vector2[steps];
        for (int j = 0; j < steps; j++)
        {
            pctx.I = Math.Min(pctx.N, ConfigHelper.Current.MaxPathLength) / steps * j;
            var (x, y) = Projectile.CalculatePositionAt((float)fwd, fnx, fny, pctx);
            points[j] = new Vector2(x,y);
        }
        DrawPolyline(points, color, ConfigHelper.Current.PathThickness, true);
        DrawCircle(points[0], ConfigHelper.Current.PathThickness*1.5f, color);
    }
}