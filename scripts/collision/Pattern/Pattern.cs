using System.Collections.Generic;
using Godot;

public partial class Pattern : Node2D
{
    public float Forward { get; set; }
    public int Count { get; set; }
    public string ProjectileModelId { get; set; }
    public Expr FnFwd { get; set; }
    public Expr FnX { get; set; }
    public Expr FnY { get; set; }
    public Expr FnT { get; set; }

    private Dictionary<string, double> ctx = new() { ["i"] = 0, ["n"] = 0 };
    private LevelLoader levelLoader;
    private List<ProjectileReference> pending = new();
    private int nextIndex;
    private float elapsed;
    public override void _Ready()
    {
        levelLoader = GetNode<LevelLoader>("/root/LevelLoader/");
    }
    public override void _Process(double delta)
    {
        if (nextIndex >= pending.Count)
            return;
        elapsed += (float)delta;
        while (nextIndex < pending.Count && elapsed >= pending[nextIndex].T)
        {
            levelLoader.SpawnProjectile(pending[nextIndex]);
            nextIndex++;
        }
    }
    public void Play()
    {
        ctx["n"] = Count;
        elapsed = 0;
        nextIndex = 0;
        pending.Clear();
        for (int i = 0; i < Count; i++)
        {
            ctx["i"] = i;

            float genFwd = FnFwd != null ? (float)FnFwd.Eval(ctx) : 0;
            float genX   = FnX   != null ? (float)FnX.Eval(ctx)   : 0;
            float genY   = FnY   != null ? (float)FnY.Eval(ctx)   : 0;
            float genT   = FnT   != null ? (float)FnT.Eval(ctx)   : 0;

            pending.Add(new ProjectileReference
            {
                Id = ProjectileModelId,
                X = GlobalPosition.X + genX,
                Y = GlobalPosition.Y + genY,
                Forward = Forward + genFwd,
                T = genT,
            });
        }
        pending.Sort((a, b) => a.T.CompareTo(b.T));
    }
}