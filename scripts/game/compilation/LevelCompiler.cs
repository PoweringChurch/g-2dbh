using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class LevelCompiler
{
    public static CompiledLevel CompileLevel(LevelData level, Node2D gameRoot)
    {
        CompiledLevel compiled = new();
        // set custom variables
        CustomVariableExpr.Definitions.Clear();
        foreach (var kvp in level.CustomVariables)
            CustomVariableExpr.Definitions[kvp.Key] = ExpressionHandler.Parse(kvp.Value);
        // compile background
        var backgroundroot = new Node2D();
        backgroundroot.Position = PlayingField.Resolutions[level.AspectRatio]/2;
        gameRoot.AddChild(backgroundroot);
        for (int i = 0; i < level.BackgroundLayers.Count; i++)
        {
            var layer = level.BackgroundLayers[i];
            var instance = new BackgroundLayerInstance();
            var sprite = new Sprite2D();
            instance.AddChild(sprite);
            instance.Sprite = sprite;
            instance.Layer = layer;
            compiled.BackgroundInstances.Add(instance);
            instance.ApplyLayerParams();
            backgroundroot.AddChild(instance);
        }
        // compile projectiles
        for (int i = 0; i < level.ProjectileModels.Length; i++)
        {
            // create projectile
            var m = level.ProjectileModels[i];
            if (m == null)
                continue;
            compiled.RenderGroups.Add(RenderGroupFromProjectile(m, gameRoot));
            // set up render group
            int renderGroupId = compiled.RenderGroups.Count - 1;
            CompileProjectile(m, renderGroupId);
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
        compiled.AspectRatio = level.AspectRatio;
        Console.Inst.Log($"[Level Compiler] Level compiled");
        return compiled;
    }
    public static void CompileProjectile(ProjectileModel model, int renderGroupId)
    {
        if (model == null)
			return;
        model.fnx = ExpressionHandler.Compile(ExpressionHandler.Parse(model.FunctionX));
		model.fny = ExpressionHandler.Compile(ExpressionHandler.Parse(model.FunctionY));
        model.fnf = ExpressionHandler.Compile(ExpressionHandler.Parse(model.FunctionF));
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
    public static RenderGroup RenderGroupFromProjectile(ProjectileModel model, Node2D root)
    {
        RenderGroup renderGroup;
        var tex = model.Texture != "default" ?
                RenderingUtils.LoadTexture(model.Texture) 
                : null;
        
        if (tex != null) // if theres a texture
		{
			var mesh = new QuadMesh { Size = tex.GetSize() }; // build a texture mesh
			renderGroup = CreateRenderGroup(mesh, GetBoundsFromProjectile(model, tex), tex, root); 
		}
		else if (model.UseShape && model.Shape != null) // else if there is a shape
		{
			Vector2[] shape = [.. model.Shape.Select(p => new Vector2(p[0], p[1]))]; // build a shape mesh
			var mesh = RenderingUtils.BuildPolygonMesh(shape);
			renderGroup = CreateRenderGroup(mesh, GetBoundsFromProjectile(model), null, root);
		}
		else // otherwise just use radius
		{
			var mesh = RenderingUtils.BuildCircleMesh(model.Radius); // build circle mesh
			renderGroup = CreateRenderGroup(mesh, GetBoundsFromProjectile(model), null, root);
		}
        return renderGroup;
    }
    public static RenderGroup CreateRenderGroup(Mesh mesh, Vector2 bounds, Texture2D tex, Node2D parent)
    {
        var multiMesh = new MultiMesh
        {
            Mesh = mesh,
            TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
            InstanceCount = 0,
            UseColors = true
        };
        var node = new MultiMeshInstance2D { Multimesh = multiMesh, Texture = tex, ShowBehindParent = true };
        parent.AddChild(node);
        return new RenderGroup { Mesh = mesh, Texture = tex, Node = node, MultiMesh = multiMesh, Bounds = bounds};
    }
    public static Vector2 GetBoundsFromProjectile(ProjectileModel m, Texture2D tex = null)
    {
        if (m == null) return Vector2.Zero;
        if (tex != null)
            return tex.GetSize()*m.RenderScale;
        if (!m.UseShape || m.ShapeVect2s == null)
            return Vector2.One*m.Radius;
        var shape = m.ShapeVect2s;
        float minX = shape[0].X;
        float maxX = shape[0].X;
        float minY = shape[0].Y;
        float maxY = shape[0].Y;
        for (int i = 1; i < shape.Count; i++)
        {
            Vector2 p = shape[i];
            if (p.X < minX) minX = p.X;
            if (p.X > maxX) maxX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.Y > maxY) maxY = p.Y;
        }
        float width = maxX - minX;
        float height = maxY - minY;
        return new Vector2(width, height) * m.RenderScale;
    }
}