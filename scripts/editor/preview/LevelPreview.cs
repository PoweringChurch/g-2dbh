using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class LevelPreview : Control
{
	[Export] Node2D PreviewRoot;
	[Export] TextureRect BackgroundImage;
	[Export] SubViewport PreviewVP;
	private Editor e;
	private float resScale = 1;
	private List<EditorRenderGroup> _renderGroups;
	private Dictionary<Texture2D, int> textMap = [];
	private float[][] _groupBuffers;
	public override void _Ready()
	{
		e = GetNode<Editor>("/root/Editor");
		GetTree().Root.SizeChanged += OnWindowResized;
	}
    /*
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed)
        {
            if ( mb.ButtonIndex == MouseButton.Left)
            {
                var (reference, offset) = GetNearestReference(mb.Position);
                if (reference == null)
                {
                    e.SelectedReference = null;
                    return;
                }
                switch (e.CurrentMode)
                {
                    case Editor.Mode.Place:
                        var local = ToPreviewLocal(mb.Position);
                        Reference newRef = new Reference
                        {
                            Id = e.SelectedModel.Id,
                            Type = e.SelectedModel is ProjectileModel v ? ModelType.Projectile : ModelType.Pattern,
                            T = e.CurrentTime,
                            X = local.X,
                            Y = local.Y
                        };
                        if (newRef != null)
                        {
                            e.SelectedReference = newRef;
                            _dragOffset = offset;
                            e.AddReference(newRef);
                        }
                        break;
                    case Editor.Mode.Select:
                        e.SelectedReference = reference;
                        _dragOffset         = offset;
                        break;
                    case Editor.Mode.Delete:
                        e.DeleteReference(reference);
                        e.SelectedReference = null; // just in case
                        break;
                }
            }
            else if (mb.ButtonIndex == MouseButton.Right && e.CurrentMode == Editor.Mode.Select)
                e.SelectedReference = null;
            else if (e.CurrentMode == Editor.Mode.Place)
                e.SelectedReference = null;
        }
        if (@event is InputEventMouseMotion mm && e.SelectedReference != null)
        {
            var local  = ToPreviewLocal(mm.Position);
            e.SelectedReference.X = local.X;
            e.SelectedReference.Y = local.Y;
        }
    }
    */
	public void Fit(Vector2I res)
	{
		var win = GetTree().Root.GetVisibleRect().Size;
		resScale = Mathf.Min(win.X / res.X, win.Y / res.Y) * 0.6f;
		PreviewVP.Size = (Vector2I)((Vector2)res * resScale);
		PreviewRoot.Scale = Vector2.One * resScale;
	}
	private void OnWindowResized() => Fit(PlayingField.Resolutions[e.levelData.AspectRatio]);
	public Vector2 ToPreviewLocal(Vector2 screenPos) =>
		(screenPos - GlobalPosition) / resScale;
	public void ChangeBackgroundImage(string to)
	{
		if (to == "none")
		{
			BackgroundImage.Texture = null;
			return;
		}
		var bg = RenderingUtils.LoadTexture(e.LevelPath+"images/",to);
		BackgroundImage.Texture = bg;
	}
	public void Sync(IEditorModel _) =>
		Sync();
	private EvalContext _ctx = new();
	public void Sync()
	{
		List<EditorReference> patternReferences = new();
		// clear indices
		for (int g = 0; g < _renderGroups.Count; g++)
			_renderGroups[g].BulletIndices.Clear();
		// tick references
		for (int i = 0; i < e.levelData.References.Count; i++)
		{
			var r = e.levelData.References[i];
			if (r.Type == ModelType.Projectile)
			{
				_ctx.T = e.CurrentTime - r.T;
				var proj = e.ProjectileModels[r.Id];
				bool alive = _ctx.T < 0 || _ctx.T >= proj.Lifetime;
				if (!alive) continue;
				var pos = CalculatePosDelta(proj, _ctx, r.F);
				r.Pos = r.SpawnPos + pos;
				_renderGroups[proj.RenderGroupId].BulletIndices.Add(i);
			}
			else if (r.Type == ModelType.Pattern)
			{
				r.Alive = true;
				var patt = e.PatternModels[r.Id];
				var lctx = new EvalContext() { N = patt.Count };
				for (int j = 0; j < patt.Count; j++)
				{
					lctx.I = j;
					double t = patt.efnt(lctx);
					var proj = e.ProjectileModels[r.Id];
					_ctx.T = e.CurrentTime - r.T + t;
					bool alive = _ctx.T < 0 || _ctx.T >= proj.Lifetime;
					if (!alive) continue;
					double fwd = patt.efnfwd(lctx);
					var spawnPos = CalculatePosDelta(e.ProjectileModels[patt.ProjectileId], lctx, r.F);
					var movement = CalculatePosDelta(e.ProjectileModels[patt.ProjectileId], lctx, r.F);
					var b = new EditorReference()
					{
						Pos = r.SpawnPos + spawnPos + movement,
						T = r.T + t,
						F = r.F + fwd,
						Type = ModelType.Projectile
					};
					patternReferences.Add(b);
					_renderGroups[patt.RenderGroupId].PatternBulletIndices.Add(patternReferences.Count - 1);
				}
			}
		}
		// draw references
		const int floatsPerInstance = 8;
		for (int g = 0; g < _renderGroups.Count; g++)
		{
			var group = _renderGroups[g];
			int count = group.BulletIndices.Count;
			int patternCount = group.PatternBulletIndices.Count;
			group.MultiMesh.InstanceCount = count + patternCount;
			if (count == 0) continue;

			int required = count * floatsPerInstance;
			if (_groupBuffers[g].Length != required)
				_groupBuffers[g] = new float[required];
			ref float[] buffer = ref _groupBuffers[g];
			// draw references
			for (int n = 0; n < count; n++)
			{
				int idx = group.BulletIndices[n];
				EditorReference r = e.levelData.References[idx];
				var model = e.ProjectileModels[r.Id];
				float scale = model.RenderScale;
				int o = n * floatsPerInstance;
				buffer[o + 0] = 0; // shear x
				buffer[o + 1] = scale; // scale x
				buffer[o + 2] = 0; // dont know dont care x
				buffer[o + 3] = r.Pos.X; // x
				buffer[o + 4] = scale; // scale y
				buffer[o + 5] = 0; // shear y
				buffer[o + 6] = 0; // dont know dont care y
				buffer[o + 7] = r.Pos.Y; // y
			}
			// draw references in pattern projectiles
			for (int n = count; n < count + patternCount; n++)
			{
				int idx = group.PatternBulletIndices[n];
				EditorReference r = patternReferences[idx];
				var model = e.ProjectileModels[r.Id];
				float scale = model.RenderScale;
				int o = n * floatsPerInstance;
				buffer[o + 0] = 0; // shear x
				buffer[o + 1] = scale; // scale x
				buffer[o + 2] = 0; // dont know dont care x
				buffer[o + 3] = r.Pos.X; // x
				buffer[o + 4] = scale; // scale y
				buffer[o + 5] = 0; // shear y
				buffer[o + 6] = 0; // dont know dont care y
				buffer[o + 7] = r.Pos.Y; // y
			}
			RenderingServer.MultimeshSetBuffer(group.MultiMesh.GetRid(), buffer);
		}
	}

	public void CompileProjectile(ProjectileModel model)
	{
		model.efnx = ExpressionHandler.Compile(ExpressionHandler.Parse(model.FunctionX));
		model.efny = ExpressionHandler.Compile(ExpressionHandler.Parse(model.FunctionY));
		// create render group
		Texture2D tex = RenderingUtils.LoadTexture($"user://data/levels/{e.levelData.LevelId}/images/", model.Texture);
		int renderGroupId = 0;
		if (tex != null)
		{
			if (textMap.TryGetValue(tex, out int existing))
				renderGroupId = existing;
			else
			{
				var mesh = new QuadMesh { Size = tex.GetSize() };
				mesh.Orientation = PlaneMesh.OrientationEnum.Z;
				var rendergroup = LevelCompiler.CreateRenderGroup(mesh, tex, PreviewRoot);
				_renderGroups.Add((EditorRenderGroup)rendergroup);
				textMap[tex] = _renderGroups.Count - 1;
			}
		}
		else if (model.UseShape && model.Shape != null)
		{
			Vector2[] shape = [.. model.Shape.Select(p => new Vector2(p[0], p[1]))];
			var mesh = RenderingUtils.BuildPolygonMesh(shape);
			var rendergroup = LevelCompiler.CreateRenderGroup(mesh, null, PreviewRoot);
			_renderGroups.Add((EditorRenderGroup)rendergroup);
			renderGroupId = _renderGroups.Count - 1;
		}
		else
		{
			var mesh = RenderingUtils.BuildCircleMesh(model.Radius);
			var rendergroup = LevelCompiler.CreateRenderGroup(mesh, null, PreviewRoot);
			_renderGroups.Add((EditorRenderGroup)rendergroup);
			renderGroupId = _renderGroups.Count - 1;
		}
		model.RenderGroupId = renderGroupId;
	}
	public static void CompilePattern(PatternModel model)
	{
		model.efnx = ExpressionHandler.Compile(ExpressionHandler.Parse(model.FunctionX));
		model.efny = ExpressionHandler.Compile(ExpressionHandler.Parse(model.FunctionY));
		model.efnt = ExpressionHandler.Compile(ExpressionHandler.Parse(model.FunctionT));
		model.efnfwd = ExpressionHandler.Compile(ExpressionHandler.Parse(model.FunctionFwd));
	}
	private static Vector2 CalculatePosDelta(ProjectileModel proj, EvalContext ctx, double f = 0)
	{
		double xTravel = proj.efnx(ctx);
		double yTravel = proj.efny(ctx);
		double cos = Math.Cos(f);
		double sin = Math.Sin(f);
		float x = (float)(cos * xTravel - sin * yTravel);
		float y = (float)(sin * xTravel + cos * yTravel);
		var pos = new Vector2(x, y);
		return pos;
	}
}
