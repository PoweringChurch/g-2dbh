using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class LevelCompiler
{
    public static CompiledLevel CompileLevel(LevelData level, Node2D gameRoot)
    {
        CompiledLevel compiled = new();
        Dictionary<string, int> projMap = [];
        Dictionary<string, int> pattMap = [];
        Dictionary<Texture2D, int> textMap = [];
        // compile projectiles
        int sharedCircleGroupId = -1;
        for (int i = 0; i < level.ProjectileModels.Count; i++)
        {
            // create projectile
            var model = level.ProjectileModels[i];
            var newProjectile = new Projectile
            {
                Lifetime = model.Lifetime,
                Persistant = model.Persistant,
                fnx = ExpressionHandler.Compile(ExpressionHandler.Parse(model.FunctionX)),
                fny = ExpressionHandler.Compile(ExpressionHandler.Parse(model.FunctionY)),
            };
            // create collision shape
            if (model.UseShape)
                newProjectile.Shape = [.. model.Shape.Select(p => new Vector2(p[0], p[1]))];
            else
                newProjectile.Radius = model.Radius;
            // set up render group
            int renderGroupId;
            float renderScale = 1f;

            Texture2D tex = level.IsMainLevel ? 
            ResourceLoader.Load<Texture2D>($"res://data/levels/{level.LevelId}/images/{model.Texture}")  
            : RenderingUtils.LoadTexture($"user://data/levels/{level.LevelId}/images/", model.Texture);
            if (tex != null)
                renderGroupId = GetOrCreateTexturedGroup(tex, textMap, gameRoot, compiled);
            else if (model.UseShape && model.Shape != null)
                renderGroupId = CreatePolygonGroup(newProjectile.Shape, gameRoot, compiled);
            else
            {
                if (sharedCircleGroupId < 0)
                    sharedCircleGroupId = CreateSharedCircleGroup(gameRoot, compiled);
                renderGroupId = sharedCircleGroupId;
                renderScale = (float)model.Radius; // radius baked per-instance via transform scale
            }
            newProjectile.RenderGroupId = renderGroupId;
            newProjectile.RenderScale = renderScale;
            compiled.Projectiles.Add(newProjectile);
            projMap[model.Id] = i;
        }
        // compile patterns
        for (int i = 0; i < level.PatternModels.Count; i++)
        {
            var model = level.PatternModels[i];
            var newPattern = new Pattern
            {
                fnx = ExpressionHandler.Compile(ExpressionHandler.Parse(model.FunctionX)),
                fny = ExpressionHandler.Compile(ExpressionHandler.Parse(model.FunctionY)),
                fnt = ExpressionHandler.Compile(ExpressionHandler.Parse(model.FunctionT)),
                fnfwd = ExpressionHandler.Compile(ExpressionHandler.Parse(model.FunctionFwd)),
                Count = model.Count,
                ProjectileId = projMap[model.ProjectileId]
            };
            compiled.Patterns.Add(newPattern);
            pattMap[model.Id] = i;
        }
        // convert references into bullets
        for (int i = 0; i < level.References.Count; i++)
        {
            var r = level.References[i];
            switch (r.Type)
            {
                case ModelType.Projectile:
                {
                    var b = new Bullet()
                    {
                        SpawnPos = new (r.X,r.Y),
                        T = r.T,
                        F = r.Forward,
                        ProjectileId = projMap[r.Id]
                    };
                    compiled.Queue.Add(b);
                    break;
                }
                case ModelType.Pattern:
                {
                    var patt = compiled.Patterns[pattMap[r.Id]];
                    var ctx = new EvalContext() {N = patt.Count};
                    for (int j = 0; j < patt.Count; j++)
                    {
                        ctx.I = j;
                        double fwd = patt.fnfwd(ctx);
                        double t = patt.fnt(ctx);
                        double xTravel = patt.fnx(ctx);
                        double yTravel = patt.fny(ctx);
                        double cos = Math.Cos(fwd + r.Forward);
                        double sin = Math.Sin(fwd + r.Forward);
                        float x = (float)(cos * xTravel - sin * yTravel);
                        float y = (float)(sin * xTravel + cos * yTravel);
                        var b = new Bullet()
                        {
                            SpawnPos = new (r.X+x,r.Y+y),
                            T = r.T+t,
                            F = r.Forward + fwd,
                            ProjectileId = patt.ProjectileId
                        };
                        compiled.Queue.Add(b);
                    }
                    break;
                }
            }
        }
        // sort references into a queue
        compiled.Queue = [.. compiled.Queue.OrderBy(b => b.T)];
        return compiled;
    }
    private static int GetOrCreateTexturedGroup(
    Texture2D tex, Dictionary<Texture2D, int> lookup, Node2D parent, CompiledLevel compiled)
    {
        if (lookup.TryGetValue(tex, out int existing))
            return existing;

        var mesh = new QuadMesh { Size = tex.GetSize() };
        int id = CreateRenderGroup(mesh, tex, parent, compiled);
        lookup[tex] = id;
        return id;
    }

    private static int CreatePolygonGroup(Vector2[] points, Node2D parent, CompiledLevel compiled)
    {
        var mesh = RenderingUtils.BuildPolygonMesh(points);
        return CreateRenderGroup(mesh, null, parent, compiled); // one group per unique shape — no dedup attempted
    }

    private static int CreateSharedCircleGroup(Node2D parent, CompiledLevel compiled)
    {
        var mesh = RenderingUtils.BuildUnitCircleMesh();
        return CreateRenderGroup(mesh, null, parent, compiled);
    }

    private static int CreateRenderGroup(Mesh mesh, Texture2D tex, Node2D parent, CompiledLevel compiled)
    {
        var multiMesh = new MultiMesh
        {
            Mesh = mesh,
            TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
            UseColors = true, // every group supports per-instance tint, textured included (white = no tint)
            InstanceCount = 0
        };
        var node = new MultiMeshInstance2D { Multimesh = multiMesh, Texture = tex };
        parent.AddChild(node);

        compiled.RenderGroups.Add(new RenderGroup { Mesh = mesh, Texture = tex, Node = node, MultiMesh = multiMesh });
        return compiled.RenderGroups.Count - 1;
    }
}