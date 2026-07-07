using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class LevelCompiler
{
    public static CompiledLevel CompileLevel(LevelData level, Node2D gameRoot)
    {
        CompiledLevel compiled = new();
        // compile projectiles
        for (int i = 0; i < level.ProjectileModels.Length; i++)
        {
            // create projectile
            var m = level.ProjectileModels[i];
            if (m == null)
                continue;
            var newProjectile = new Projectile
            {
                Lifetime = m.Lifetime,
                Persistant = m.Persistant,
                fnx = ExpressionHandler.Compile(ExpressionHandler.Parse(m.FunctionX)),
                fny = ExpressionHandler.Compile(ExpressionHandler.Parse(m.FunctionY)),
                RenderScale = m.RenderScale
            };
            // create collision shape
            if (m.UseShape)
            {
                newProjectile.Shape = CollisionUtils.FloatArrToVect2s(m.Shape);
            }
            else
                newProjectile.Radius = m.Radius;
            // set up render group
            int renderGroupId = 0;
            RenderGroup renderGroup;
            Texture2D tex = RenderingUtils.LoadTexture($"user://data/levels/{level.LevelId}/images/", m.Texture);
            if (tex != null)
            {
                var mesh = new QuadMesh { Size = tex.GetSize() };
                //mesh.Orientation = PlaneMesh.OrientationEnum.Z;
                renderGroup = CreateRenderGroup(mesh, tex, gameRoot);
            }
            else if (m.UseShape && m.Shape != null)
            {
                Vector2[] shape = [.. m.Shape.Select(p => new Vector2(p[0], p[1]))]; // build a shape mesh
                var mesh = RenderingUtils.BuildPolygonMesh(shape);
                renderGroup = CreateRenderGroup(mesh, null, gameRoot);
            }
            else
            {
                var mesh = RenderingUtils.BuildCircleMesh(m.Radius);
                renderGroup = CreateRenderGroup(mesh, null, gameRoot);
            }
            compiled.RenderGroups.Add(renderGroup);
            renderGroupId = compiled.RenderGroups.Count - 1;
            newProjectile.RenderGroupId = renderGroupId;
            compiled.Projectiles[m.Id] = newProjectile;
        }
        // compile patterns
        for (int i = 0; i < level.PatternModels.Length; i++)
        {
            var m = level.PatternModels[i];
            if (m == null)
                continue;
            var newPattern = new Pattern
            {
                fnx = ExpressionHandler.Compile(ExpressionHandler.Parse(m.FunctionX)),
                fny = ExpressionHandler.Compile(ExpressionHandler.Parse(m.FunctionY)),
                fnt = ExpressionHandler.Compile(ExpressionHandler.Parse(m.FunctionT)),
                fnfwd = ExpressionHandler.Compile(ExpressionHandler.Parse(m.FunctionFwd)),
                Count = m.Count,
                ProjectileId = m.ProjectileId
            };
            compiled.Patterns[m.Id] = newPattern;
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
                            SpawnPos = new Vector2(r.SpawnX, r.SpawnY),
                            T = r.T,
                            F = r.F,
                            Id = r.Id
                        };
                        compiled.Queue.Add(b);
                        break;
                    }
                case ModelType.Pattern:
                    {
                        var patt = compiled.Patterns[r.Id];
                        var ctx = new EvalContext() {  N = patt.Count > 1 ? patt.Count - 1 : 1  };
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
                                SpawnPos = new Vector2(r.SpawnX, r.SpawnY) + new Vector2(x, y),
                                T = r.T + t,
                                F = r.F + fwd,
                                Id = patt.ProjectileId
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
        Console.Inst.Log($"[Level Compiler] Level compiled");
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
        return new RenderGroup { Mesh = mesh, Texture = tex, Node = node, MultiMesh = multiMesh };
    }
}