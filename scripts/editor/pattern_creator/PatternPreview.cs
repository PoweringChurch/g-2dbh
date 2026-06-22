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
    private Dictionary<string, double> ctx = new() { ["t"] = 0 };
    public double T
    {
        get => ctx["t"];
        set
        {
            ctx["t"] = Math.Max(value, 0);
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
        Dictionary<string, double> lctx = new() { ["n"] = count }; // live ctx, stores i, n, projectile t
        // loop through count and draw a projectile for i in count
        for (int i = 0; i < Count; i++)
        {
            lctx["i"] = i;
            // generate values
            double genFwd = fnFwd != null ? fnFwd.Eval(lctx) : 0;
            double genT = fnT != null ? fnT.Eval(lctx) : 0;
            var startPos = Projectile.CalculatePositionAt(Vector2.Zero, 0, fnX, fnY, lctx);
            double rawT = ctx["t"] - genT;
            bool alive = rawT >= 0 && rawT < projModel.Lifetime;
            lctx["t"] = Math.Clamp(rawT, 0, projModel.Lifetime);
            var pos = Projectile.CalculatePositionAt(startPos, (float)genFwd, projModelFnX, projModelFnY, lctx);
            // draw
            var texture = projModel.Texture != "default" ?
                RenderingUtils.LoadTexture(e.LevelPath + "images/", projModel.Texture)
                : null;
            DrawProjectileShape(projModel, texture, pos, (float)genFwd, alive ? Colors.White : deadColor);
            DrawSetTransform(Vector2.Zero, 0, Vector2.One);
            DrawPath(startPos, projModelFnX, projModelFnY, lctx, "t", projModel.Lifetime, genFwd, deadColor, 16);
        }
        DrawPath(Vector2.Zero, fnX, fnY, lctx, "i", lctx["n"], 0,  Colors.Green, 32);
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
    private void DrawPath(Vector2 pos,
    Expr fnx, Expr fny,
    Dictionary<string, double> pctx, string input,
    double len, double fwd,
    Color color, int steps = 16)
    {
        Vector2[] points = new Vector2[steps];
        for (int j = 0; j < steps; j++)
        {
            pctx[input] = len / steps * j;
            points[j] = Projectile.CalculatePositionAt(pos, (float)fwd, fnx, fny, pctx);
        }
        DrawPolyline(points, color, 1.5f, true);
        DrawCircle(points[0], 3f, color);
    }
}