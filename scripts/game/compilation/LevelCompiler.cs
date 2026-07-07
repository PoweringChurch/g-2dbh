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
            compiled.RenderGroups.Add(RenderGroupFromProjectile(m, $"user://data/levels/{level.LevelId}/images/", gameRoot));
            // set up render group
            int renderGroupId = compiled.RenderGroups.Count - 1;
            CompileProjectile(m, renderGroupId);
            Console.Inst.Log($"just added projectile of id {i}");
            compiled.Projectiles[i] = m;
        }
        // compile patterns
        for (int i = 0; i < level.PatternModels.Length; i++)
        {
            var m = level.PatternModels[i];
            if (m == null)
                continue;
            CompilePattern(m);
            compiled.Patterns[m.Id] = m;
        }
        // convert editor references into spatial references
        for (int i = 0; i < level.References.Count; i++)
        {
            var r = level.References[i];
            var sr = new SpatialReference()
            {
                SpawnPos = new Vector2(r.SpawnX, r.SpawnY),
                Pos = new Vector2(r.SpawnX, r.SpawnY),
                T = r.T,
                F = r.F,
                Type = r.Type,
                Id = r.Id,
                Depth = 0,
            };
            compiled.Queued.Add(sr);
        }
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
    public static void CompileProjectile(ProjectileModel model, int renderGroupId)
    {
        if (model == null)
			return;
        model.fnx = ExpressionHandler.Compile(ExpressionHandler.Parse(model.FunctionX));
		model.fny = ExpressionHandler.Compile(ExpressionHandler.Parse(model.FunctionY));
        model.RenderGroupId = renderGroupId;
        if (model.UseShape)
            model.ShapeVect2s = CollisionUtils.FloatArrToVect2s(model.Shape);
    }
    public static void CompilePattern(PatternModel model)
    {
        if (model == null)
			return;
		model.fnx = ExpressionHandler.Compile(ExpressionHandler.Parse(model.FunctionX));
		model.fny = ExpressionHandler.Compile(ExpressionHandler.Parse(model.FunctionY));
		model.fnt = ExpressionHandler.Compile(ExpressionHandler.Parse(model.FunctionT));
		model.fnf = ExpressionHandler.Compile(ExpressionHandler.Parse(model.FunctionFwd));
    }
    public static RenderGroup RenderGroupFromProjectile(ProjectileModel model, string levelPath, Node2D root)
    {
        RenderGroup renderGroup;
        Texture2D tex = RenderingUtils.LoadTexture(levelPath, model.Texture);
        if (tex != null) // if theres a texture
		{
			var mesh = new QuadMesh { Size = tex.GetSize() }; // build a texture mesh
			renderGroup = CreateRenderGroup(mesh, tex, root); 
		}
		else if (model.UseShape && model.Shape != null) // else if there is a shape
		{
			Vector2[] shape = [.. model.Shape.Select(p => new Vector2(p[0], p[1]))]; // build a shape mesh
			var mesh = RenderingUtils.BuildPolygonMesh(shape);
			renderGroup = CreateRenderGroup(mesh, null, root);
		}
		else // otherwise just use radius
		{
			var mesh = RenderingUtils.BuildCircleMesh(model.Radius); // build circle mesh
			renderGroup = CreateRenderGroup(mesh, null, root);
		}
        return renderGroup;
    }
}