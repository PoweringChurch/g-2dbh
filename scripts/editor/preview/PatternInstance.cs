using System;
using System.Collections.Generic;
using Godot;

public partial class PatternEditorInstance : ReferenceInstance
{
    private PatternReference _reference;
    public override ISpatialReference Reference => _reference;
    private PatternModel _model => e.PatternRegistry.GetModel(_reference.Id);
    private Editor e;
    private float _projLifetime => e.ProjectileRegistry.GetModel(_model.ProjectileId).Lifetime;
    private float _maxGenT;
    private Expr _fnFwd;
    private Expr _fnX;
    private Expr _fnY;
    private Expr _fnT;

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
        _fnFwd = ExpressionParser.Parse(_model.FunctionFwd);
        _fnX   = ExpressionParser.Parse(_model.FunctionX);
        _fnY   = ExpressionParser.Parse(_model.FunctionY);
        _fnT   = ExpressionParser.Parse(_model.FunctionT);
        ctx["n"] = _model.Count;

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
        return currentTime <= _reference.T + _maxGenT + _projLifetime;
    }
    public override float DistanceTo(Vector2 localPos)
    {
        return localPos.DistanceTo(Position);
    }

    public override void _Process(double _)
    {
        if (Invalid)
            return;
        bool alive = e.CurrentTime >= _reference.T;
        Visible = alive || _isSelected;
        if (Visible)
        {
            Position = new Vector2(_reference.X, _reference.Y);
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (_isSelected)
            DrawPreview();
        DrawCircle(Vector2.Zero, 4f, RenderingUtils.ColorFromString(_model.Id));
        DrawLine(Vector2.Zero, Vector2.Right.Rotated(Mathf.DegToRad(_reference.Forward)) * 16f,
            RenderingUtils.ColorFromString(_model.Id), 1.5f);
    }

    private void DrawPreview()
    {
        var color = RenderingUtils.ColorFromString(_model.Id);
        int count = _model.Count;

        for (int i = 0; i < count; i++)
        {
            ctx["i"] = i;

            float genFwd = (float)_fnFwd.Eval(ctx);
            float genX   = (float)_fnX.Eval(ctx);
            float genY   = (float)_fnY.Eval(ctx);
            float genT   = Math.Max((float)_fnT.Eval(ctx), 0);

            var pos = new Vector2(genX, genY);
            float t = e.CurrentTime - _reference.T + genT;
            bool alive =  t >= 0 && t <= _projLifetime ;
            
            DrawCircle(pos, 3f, alive ? color : color * new Color(1, 1, 1, 0.35f));
        }
    }
}