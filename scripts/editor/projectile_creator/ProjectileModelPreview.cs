using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
public partial class ProjectileModelPreview : Node2D
{
    public Editor e;
    private string textureName;
    public string TextureName
    {
        get => textureName;
        set
        {
            textureName = value;
            _dirty = true;
        }
    }
    private bool useShape;
    public bool UseShape
    {
        get => useShape;
        set
        {
            useShape = value;
            _dirty = true;
        }
    }
    private float[][] shape;
    public float[][] Shape
    {
        get => shape;
        set
        {
            shape = value;
            _dirty = true;
        }
    }
    private float radius;
    public float Radius
    {
        get => radius;
        set
        {
            radius = value;
            _dirty = true;
        }
    }
    private float lifetime;
    public float Lifetime
    {
        get => lifetime;
        set
        {
            lifetime = value;
            _dirty = true;
        }
    }
    private Expr fnY;
    public Expr FnY
    {
        get => fnY;
        set
        {
            fnY = value;
            _dirty = true;
        }
    }
    private Expr fnX;
    public Expr FnX
    {
        get => fnX;
        set
        {
            fnX = value;
            _dirty = true;
        }
    }
    private bool _dirty = false;
    private EvalContext ctx = new() {T = 0};
    public double T
    {
        get => ctx.T;
        set
        {
            ctx.T = Math.Max(value, 0);
            _dirty = true;
        }
    }
    private Vector2 pos = Vector2.Zero;
    public Vector2 PreviewPosition => pos;
    public override void _Ready()
    {
        e = GetNode<Editor>("/root/Editor");
    }
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
        var (x, y) = Projectile.CalculatePositionAt(0, fnX, fnY, ctx);
        pos = new(x,y);
        // draw projectile
        var texture = textureName != "default" ? 
                RenderingUtils.LoadTexture(e.LevelPath + "images/", textureName) 
                : null;
        if (texture != null)
            DrawTexture(texture, (-texture.GetSize() / 2)+pos);
        else
        {
            if (useShape && shape != null)
            {
                var points = shape.Select(p => new Vector2(p[0], p[1]) + pos).ToArray();
                DrawPolyline(points, Colors.White, 1.5f, true);
                if (points.Length > 1)
                    DrawLine(points[^1], points[0], Colors.White, 1.5f);
            }
            else
                DrawCircle(pos, radius, Colors.White);
        }
        DrawPath();
    }
    private void DrawPath(int steps = 32)
    {
        Vector2[] points = new Vector2[steps];
        var lctx = new EvalContext();
        for (int i = 0; i < steps; i++)
        {
            lctx.T = lifetime / steps * i;
            var (x, y) = Projectile.CalculatePositionAt(0, fnX, fnY, lctx);
            points[i] = new(x,y);
        }
        DrawPolyline(points, Colors.Yellow, 1.5f, true);
        DrawCircle(points[0], 3f, Colors.Yellow);
    }
}