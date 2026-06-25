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
            GD.Print("user://data/levels/{level.LevelId}/images/"+model.Texture);
            Texture2D tex = RenderingUtils.LoadTexture($"user://data/levels/{level.LevelId}/images/", model.Texture);
            /* ? 
            ResourceLoader.Load<Texture2D>($"res://data/levels/{level.LevelId}/images/{model.Texture}")  
            : ;*/
            GD.Print(tex);
            if (tex != null)
            {
                if (textMap.TryGetValue(tex, out int existing))
                    renderGroupId = existing;
                else
                {
                    GD.Print("projectile is a texture mesh");
                    var mesh = new QuadMesh { Size = tex.GetSize() };
                    mesh.Orientation = PlaneMesh.OrientationEnum.Z;
                    renderGroupId = CreateRenderGroup(mesh, tex, gameRoot, compiled);
                    textMap[tex] = renderGroupId;
                }
                GD.Print($"projectile {model.Id} is a polygon with radius {model.Radius}");
            }
            else if (model.UseShape && model.Shape != null)
            {
                var mesh = RenderingUtils.BuildPolygonMesh(newProjectile.Shape);
                renderGroupId = CreateRenderGroup(mesh, null, gameRoot, compiled);
                GD.Print($"projectile {model.Id} is a polygon");
            }
            else
            {
                GD.Print($"projectile {model.Id} is a circle with radius {model.Radius}");
                var mesh = RenderingUtils.BuildUnitCircleMesh();
                renderGroupId = CreateRenderGroup(mesh, null, gameRoot, compiled);
                renderScale = (float)model.Radius; // radius baked per-instance via transform scale
            }
            GD.Print($"assign projcetile {model.Id} to render group {renderGroupId}");
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
                        double cos = Math.Cos(r.Forward);
                        double sin = Math.Sin(r.Forward);
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
        // set duration
        compiled.Duration = level.Duration;
        return compiled;
    }
    private static int CreateRenderGroup(Mesh mesh, Texture2D tex, Node2D parent, CompiledLevel compiled)
    {
        var multiMesh = new MultiMesh
        {
            Mesh = mesh,
            TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
            InstanceCount = 0
        };
        var node = new MultiMeshInstance2D { Multimesh = multiMesh, Texture = tex };
        parent.AddChild(node);
        compiled.RenderGroups.Add(new RenderGroup { Mesh = mesh, Texture = tex, Node = node, MultiMesh = multiMesh});
        GD.Print($"added render group, count is now {compiled.RenderGroups.Count}");
        return compiled.RenderGroups.Count - 1;
    }
}