using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class LevelCompiler
{
    public static CompiledLevel CompileLevel(LevelData level, Node2D gameRoot)
    {
        CompiledLevel compiled = new();
        Dictionary<int, int> projMap = [];
        Dictionary<int, int> pattMap = [];
        Dictionary<Texture2D, int> textMap = [];
        // compile projectiles
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
            int renderGroupId = 0;
            float renderScale = model.RenderScale;
            GD.Print("user://data/levels/{level.LevelId}/images/"+model.Texture);
            Texture2D tex = RenderingUtils.LoadTexture($"user://data/levels/{level.LevelId}/images/", model.Texture);
            if (tex != null)
            {
                if (textMap.TryGetValue(tex, out int existing))
                    renderGroupId = existing;
                else
                {
                    GD.Print("projectile is a texture mesh");
                    var mesh = new QuadMesh { Size = tex.GetSize() };
                    mesh.Orientation = PlaneMesh.OrientationEnum.Z;
                    var rendergroup = CreateRenderGroup(mesh, tex, gameRoot);
                    compiled.RenderGroups.Add(rendergroup);
                    textMap[tex] = compiled.RenderGroups.Count - 1;
                }
            }
            else if (model.UseShape && model.Shape != null)
            {
                var mesh = RenderingUtils.BuildPolygonMesh(newProjectile.Shape);
                var rendergroup = CreateRenderGroup(mesh, null, gameRoot);
                compiled.RenderGroups.Add(rendergroup);
                renderGroupId = compiled.RenderGroups.Count - 1;
            }
            else
            {
                var mesh = RenderingUtils.BuildCircleMesh(model.Radius);
                var rendergroup = CreateRenderGroup(mesh, null, gameRoot);
                compiled.RenderGroups.Add(rendergroup);
                renderGroupId = compiled.RenderGroups.Count - 1;
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
                        SpawnPos = r.SpawnPos,
                        T = r.T,
                        F = r.F,
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
                        double cos = Math.Cos(r.F);
                        double sin = Math.Sin(r.F);
                        float x = (float)(cos * xTravel - sin * yTravel);
                        float y = (float)(sin * xTravel + cos * yTravel);
                        var b = new Bullet()
                        {
                            SpawnPos = r.SpawnPos+new Vector2(x,y),
                            T = r.T+t,
                            F = r.F + fwd,
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
        // set duration
        compiled.Duration = level.Duration;
        return compiled;
    }
    public static RenderGroup CreateRenderGroup(Mesh mesh, Texture2D tex, Node2D parent)
    {
        var multiMesh = new MultiMesh
        {
            Mesh = mesh,
            TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
            InstanceCount = 0
        };
        var node = new MultiMeshInstance2D { Multimesh = multiMesh, Texture = tex };
        parent.AddChild(node);
        return new RenderGroup { Mesh = mesh, Texture = tex, Node = node, MultiMesh = multiMesh};
    }
}