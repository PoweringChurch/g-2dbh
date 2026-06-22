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
        Play();
    }
    public override void _PhysicsProcess(double dt)
    {
        if (nextIndex >= pending.Count)
            return;
        elapsed += (float)dt;
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
            float genT   = FnT   != null ? (float)FnT.Eval(ctx)   : 0;
            var startPos = Projectile.CalculatePositionAt(Vector2.Zero, Forward, FnX, FnY, ctx);

            pending.Add(new ProjectileReference
            {
                Id = ProjectileModelId,
                X = Position.X + startPos.X,
                Y = Position.Y + startPos.Y,
                Forward = Forward + genFwd,
                T = genT,
            });
        }
        pending.Sort((a, b) => a.T.CompareTo(b.T));
    }
}