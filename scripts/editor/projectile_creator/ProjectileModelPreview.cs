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
    private List<Vector2> shape;
    public List<Vector2> Shape
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
    private float renderScale;
    public float RenderScale
    {
        get => renderScale;
        set
        {
            renderScale = value;
            _dirty = true;
        }
    }
    private float telegraphTime;
    public float TelegraphTime
    {
        get => telegraphTime;
        set
        {
            telegraphTime = value;
            _dirty = true;
        }
    }
    private bool canCollide;
    public bool CanCollide
    {
        get => canCollide;
        set
        {
            canCollide = value;
            _dirty = true;
        }
    }
    private bool _dirty = false;
    private EvalContext ctx = new() {T = 0, L = 1};
    public double T
    {
        get => ctx.T;
        set
        {
            ctx.T = Math.Max(value, 0);
            _dirty = true;
        }
    }
    public double L
    {
        get => ctx.L;
        set
        {
            ctx.L = value;
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
        var (x, y) = CalculatePositionAt(0, fnX, fnY, ctx);
        pos = new Vector2(x,y)/renderScale;
        // draw projectile
        var texture = textureName != "default" ? 
                RenderingUtils.LoadTexture(e.LevelPath + "images/", textureName) 
                : null;
        DrawSetTransform(Vector2.Zero,0,renderScale*Vector2.One);
        float alpha = (T <= telegraphTime) 
            ? (telegraphTime > 0 ? 0.2f + (float)T / telegraphTime * 0.6f : 0.8f)
            : 1.0f;
        if (texture != null)
        {
            DrawTexture(texture, (-texture.GetSize() / 2)+pos, new Color(1,1,1,alpha));
        }
        else if (useShape && shape != null)
        {
            List<Vector2> closed = [.. shape, shape[0]];
            var points = closed.Select(p => new Vector2(p[0], p[1]) + pos).ToArray();
            if (points.Length > 2) 
                DrawColoredPolygon(points, new Color(1,1,1,alpha));
        }
        else
        {
            DrawCircle(pos, radius, new Color(1,1,1,alpha));
        }
        // draw collision
        if (ConfigHelper.Current.ShowCollision 
        && textureName != "default" 
        && telegraphTime <= T
        && canCollide)
        {
            if (useShape && shape != null)
            {
                var points = shape.Select(p => new Vector2(p[0], p[1]) + pos).ToArray();
                DrawPolyline(points, new Color(1, 1, 0, 0.5f), 1.5f / renderScale, true);
                if (points.Length > 1)
                    DrawLine(points[^1], points[0], new Color(1, 1, 0, 0.5f), 1.5f / renderScale);
            }
            else
            {
                DrawCircle(pos, radius / renderScale, new Color(1, 1, 0, 0.5f), false); // semi-transparent red
            }
        }
        DrawSetTransform(Vector2.Zero,0,Vector2.One);
        DrawPath();
    }
    private void DrawPath()
    {
        int steps = ConfigHelper.Current.PathFidelity;
        Vector2[] points = new Vector2[steps];
        var lctx = new EvalContext();
        for (int i = 0; i < steps; i++)
        {
            lctx.T = Math.Min(ctx.L,ConfigHelper.Current.MaxPathLength) / steps * i;
            lctx.L = ctx.L;
            var (x, y) = CalculatePositionAt(0, fnX, fnY, lctx);
            points[i] = new(x,y);
        }
        DrawPolyline(points, Colors.Yellow, ConfigHelper.Current.PathThickness, true);
        DrawCircle(points[0], ConfigHelper.Current.PathThickness*1.5f, Colors.Yellow);
    }
    private static (float x, float y) CalculatePositionAt(float fwd, Expr fnx, Expr fny, EvalContext ctx)
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