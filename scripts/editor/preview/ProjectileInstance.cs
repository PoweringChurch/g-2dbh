using System.Collections.Generic;
using System.Linq;
using Godot;
public partial class ProjectileEditorInstance : EditorInstance
{
    private Reference _reference;
    public override Reference Reference => _reference;
    private ProjectileModel _model => e.ProjectileRegistry.GetModel(_reference.Id);
    private Editor e;
    private Expr _motionFnX;
    private Expr _motionFnY;
    private Texture2D _texture;
    private EvalContext ctx = new () {T = 0};
    // calc
    Vector2 perp;
    public override bool Invalid => _model == null;
    private bool _isSelected => e.SelectedReference == _reference;
    public override void Init(Reference r)
    {
        e = GetNode<Editor>("/root/Editor");
        _reference = r;
        OnModelUpdate();
    }
    public override void OnModelUpdate()
    {
        if (Invalid)
            return;
        _motionFnX = ExpressionHandler.Parse(_model.FunctionX);
        _motionFnY = ExpressionHandler.Parse(_model.FunctionY);
        _texture = null;
        if (_model.Texture != "default")
            _texture   = RenderingUtils.LoadTexture(e.LevelPath+"images/", _model.Texture);
    }
    public override bool IsAlive(float time)
    {
        if (Invalid)
            return false;
        float t = time - _reference.T;
        return t >= 0 && t <= _model.Lifetime;
    }
    public override float DistanceTo(Vector2 localPos)
    {
        float dist = localPos.DistanceTo(Position);
        if (!_model.UseShape && dist <= _model.Radius)
            return 0f;
        return dist;
        // todo: bounding box / shape-center distance for UseShape case
    }
    public override void _Process(double _)
    {
        if (Invalid)
            return;
        float t = e.CurrentTime - _reference.T;
        bool alive = t >= 0 && t <= _model.Lifetime;
        Visible = alive || _isSelected;
        if (!Visible) return;
        ctx.T = Mathf.Clamp(t, 0, _model.Lifetime);
        var (x,y) = Projectile.CalculatePositionAt(_reference.Forward, _motionFnX, _motionFnY, ctx);
        Position = new Vector2(_reference.X,_reference.Y) + new Vector2(x,y);
        QueueRedraw();
    }
    public override void _Draw()
    {
        if (Invalid)
            return;
        // draw path
        if (_isSelected)
            DrawPath();
        // draw projectile
        if (_texture != null)
        {
            DrawTexture(_texture, -_texture.GetSize() / 2);
            return;
        }
        if (_model.UseShape && _model.Shape != null)
        {
            var points = _model.Shape.Select(p => new Vector2(p[0], p[1])).ToArray();
            DrawPolyline(points, Colors.White, 1.5f, true);
            if (points.Length > 1)
                DrawLine(points[^1], points[0], Colors.White, 1.5f);
        }
        else
            DrawCircle(Vector2.Zero, _model.Radius, Colors.White);
    }
    private void DrawPath()
    {
        int steps = 32;
        Vector2[] points = new Vector2[steps];
        var lctx = new EvalContext();
        for (int i = 0; i < steps; i++)
        {
            lctx.T = _model.Lifetime / steps * i;
            var (x,y) = Projectile.CalculatePositionAt(_reference.Forward, _motionFnX, _motionFnY, lctx);
            points[i] = new Vector2(x,y);
        }
        DrawPolyline(points, RenderingUtils.ColorFromString(_model.Id), 1.5f, true);
        DrawCircle(points[0], 3f, RenderingUtils.ColorFromString(_model.Id));
    }
}