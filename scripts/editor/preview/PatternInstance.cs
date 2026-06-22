using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class PatternEditorInstance : ReferenceInstance
{
    static Color deadColor = new(1, 1, 1, 0.3f);
    private PatternReference _reference;
    public override ISpatialReference Reference => _reference;
    private PatternModel _model => e.PatternRegistry.GetModel(_reference.Id);
    private ProjectileModel _projModel => e.ProjectileRegistry.GetModel(_model.ProjectileId);
    private Editor e;
    private float _projLifetime => e.ProjectileRegistry.GetModel(_model.ProjectileId).Lifetime;
    private float _maxGenT;
    private Expr _fnFwd;
    private Expr _fnX;
    private Expr _fnY;
    private Expr _fnT;
    private Expr _projModelFnX;
    private Expr _projModelFnY;
    private Dictionary<string, double> ctx = new() { ["i"] = 0, ["n"] = 0 };

    public override bool Invalid => _model == null;
    private bool _isSelected => e.SelectedReference == _reference;

    public override void Init(ISpatialReference r)
    {
        e = GetNode<Editor>("/root/Editor");
        _reference = (PatternReference)r;
        OnModelUpdate();
    }

    public override void OnModelUpdate()
    {
        if (Invalid)
            return;
        ctx["n"] = _model.Count;
        _fnFwd = ExpressionParser.Parse(_model.FunctionFwd);
        _fnX   = ExpressionParser.Parse(_model.FunctionX);
        _fnY   = ExpressionParser.Parse(_model.FunctionY);
        _fnT   = ExpressionParser.Parse(_model.FunctionT);

        var projModel = _projModel;
        _projModelFnX = ExpressionParser.Parse(projModel.FunctionX);
        _projModelFnY = ExpressionParser.Parse(projModel.FunctionY);

        _maxGenT = float.MinValue;
        for (int i = 0; i < _model.Count; i++)
        {
            ctx["i"] = i;
            float genT = (float)_fnT.Eval(ctx);
            if (genT > _maxGenT)
                _maxGenT = genT;
        }
    }
    public override bool IsAlive(float currentTime)
    {
        if (Invalid)
            return false;
        float t = currentTime - _reference.T;
        return t >= 0 && t <= _maxGenT + _projLifetime;
    }
    public override float DistanceTo(Vector2 localPos)
    {
        var dist = localPos.DistanceTo(Position);
        if (dist <= 7)
            return 0;
        return dist;
    }
    public override void _Process(double _)
    {
        if (Invalid)
            return;
        float t = e.CurrentTime - _reference.T;
        bool alive =  t >= 0 && t <= _maxGenT + _projLifetime;
        Visible = alive || _isSelected;
        if (!Visible) return;
        Position = new Vector2(_reference.X, _reference.Y);
        QueueRedraw();
    }
    public override void _Draw()
    {
        if (Invalid)
            return;
        var projModel = _projModel;
        int count = _model.Count;
        double t = e.CurrentTime - _reference.T;
        DrawCircle(Vector2.Zero, 7, RenderingUtils.ColorFromString(_model.Id), false, 3);
        for (int i = 0; i < count; i++)
        {
            DrawSetTransform(Vector2.Zero, 0, Vector2.One);
            ctx["i"] = i;
            // generate values
            double genFwd = _fnFwd != null ? _fnFwd.Eval(ctx) : 0;
            double genT = _fnT != null ? _fnT.Eval(ctx) : 0;

            var startPos = Projectile.CalculatePositionAt(Vector2.Zero, 0, _fnX, _fnY, ctx);
            double rawT = t - genT;
            bool alive = rawT >= 0 && rawT < projModel.Lifetime;
            ctx["t"] = Math.Clamp(rawT, 0, projModel.Lifetime);
            var pos = Projectile.CalculatePositionAt(startPos, (float)genFwd, _projModelFnX, _projModelFnY, ctx);
            // draw
            var texture = projModel.Texture != "default" ?
                RenderingUtils.LoadTexture(e.LevelPath + "images/", projModel.Texture)
                : null;
            DrawProjectileShape(projModel, texture, pos, (float)genFwd, alive ? Colors.White : deadColor);
        }
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
}