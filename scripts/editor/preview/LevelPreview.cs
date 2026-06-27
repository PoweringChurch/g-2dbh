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
	private List<EditorRenderGroup> _renderGroups = new();
	private Dictionary<Texture2D, int> textMap = [];
	private float[][] _groupBuffers = new float[Editor.MaxModelCount][];
	public override void _Ready()
	{
		e = GetNode<Editor>("/root/Editor");
		GetTree().Root.SizeChanged += OnWindowResized;
		PreviewRoot.Draw += () => DrawGizmos(_selectedReference);
		_renderGroups.Add(CreateRenderGroup(RenderingUtils.BuildCircleMesh(15), null, PreviewRoot));
	}
	private EditorReference _selectedReference;
	private bool _inGroup = false;
    private bool lmbDragging = false;
    private bool rmbDragging = false;
	private bool snap => e.Snap;
	private Vector2 mousePoint0;
	private Vector2 mousePoint1;
	private List<EditorReference> _groupSelection = new();
    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.Left && mb.Pressed)
            {
				if (e.levelData.References == null) return;
                switch (e.CurrentMode)
				{
					case Editor.Mode.Place: HandlePlacePress(mb); break;
					case Editor.Mode.Select: HandleSelectLMBPress(mb); break;
					case Editor.Mode.Delete: HandleDeletePress(mb); break;
				}
            }
			else if (mb.ButtonIndex == MouseButton.Left && !mb.Pressed)
			{
				switch (e.CurrentMode)
				{
					case Editor.Mode.Place: HandlePlaceRelease(mb); break;
					case Editor.Mode.Select: HandleSelectLMBRelease(); break;
					case Editor.Mode.Delete: HandleDeleteRelease(); break;
				}
			}
			else if (mb.ButtonIndex == MouseButton.Right && mb.Pressed)
			{
				switch (e.CurrentMode)
				{
					case Editor.Mode.Select: HandleSelectRMBPress(mb); break;
				}
			}
			else if (mb.ButtonIndex == MouseButton.Right && !mb.Pressed)
			{
				switch (e.CurrentMode)
				{
					case Editor.Mode.Select: HandleSelectRMBRelease(mb); break;
				}
			}
        }
		else if (@event is InputEventMouseMotion mm )
		{
			switch (e.CurrentMode)
			{
				case Editor.Mode.Place: HandlePlaceMM(mm); break;
				case Editor.Mode.Select: HandleSelectMM(mm); break;
				case Editor.Mode.Delete: HandleDeleteMM(mm); break;
			}
		}
    }
	private void HandlePlacePress(InputEventMouseButton mb)
	{
		if (e.SelectedModel == null)
			return;
		var local = ToPreviewLocal(mb.Position);
		var newRef = new EditorReference
		{
			Id = e.SelectedModel.Id,
			Type = e.SelectedModel is ProjectileModel ? ModelType.Projectile : ModelType.Pattern,
			T = e.CurrentTime,
			SpawnX = local.X,
			SpawnY = local.Y,
			Pos = new(local.X, local.Y)
		};
		lmbDragging = true;
		_selectedReference = newRef;
		_groupSelection.Clear();
		e.AddReference(newRef);
	}
	private void HandleSelectRMBPress(InputEventMouseButton mb)
	{
		rmbDragging = true;
		if (_selectedReference != null) // dont have a selected reference
			return;
		else if (_groupSelection.Count > 0) // but do have a group
		{
			_groupSelection.Clear();
		}
		mousePoint0 = ToPreviewLocal(mb.Position);
	}
	private void HandleSelectLMBPress(InputEventMouseButton mb)
	{
		lmbDragging = true;
		if (rmbDragging == true && _inGroup)
		{
			return;
		}
		var r = GetNearestReference(mb.Position);
		_selectedReference = r;
		_inGroup = _groupSelection.Find((s)=> s == _selectedReference) != null; // if the newly selected object is not in the group or doesnt exist
		if (!_inGroup)
		{
			mousePoint0 = Vector2.Zero;
			mousePoint1 = Vector2.Zero;
			_groupSelection.Clear();
		}
		PreviewRoot.QueueRedraw();
	}
	private void HandleDeletePress(InputEventMouseButton mb)
	{
		var r = GetNearestReference(mb.Position);
		if (r != null) // if we got a reference
		{
			e.DeleteReference(r);
			if (_inGroup) // if the reference is in the selection group
			{
				foreach (var sr in _groupSelection)
				{
					e.DeleteReference(sr);
				}
			}
		}
		lmbDragging = true;
		_selectedReference = null;
	}
	private void HandlePlaceRelease(InputEventMouseButton mb)
	{
		if (e.SelectedModel == null)
			return;
		var local = ToPreviewLocal(mb.Position);
		var rPos = new Vector2(_selectedReference.SpawnX, _selectedReference.SpawnY);
		float f = (rPos - local).Angle()+(Mathf.Pi/2);
		if (snap)
		{
			float step = Mathf.Pi / ConfigHelper.Current.AngleSnapDivision;
			f = Mathf.Round(f / step) * step;
		}
		_selectedReference.F = f;
		lmbDragging = false;
		_selectedReference = null;
		e.SyncPreview();
		PreviewRoot.QueueRedraw();
	}
	private void HandleSelectRMBRelease(InputEventMouseButton mb)
	{
		rmbDragging = false;
		if (_inGroup) // if we are grabbing something inside of the group
		{	
			var local = ToPreviewLocal(mb.Position);
			var rPos = new Vector2(_selectedReference.SpawnX, _selectedReference.SpawnY);
			float f = (rPos - local).Angle()+(Mathf.Pi/2);
			if (snap)
			{
				float step = Mathf.Pi / ConfigHelper.Current.AngleSnapDivision;
				f = Mathf.Round(f / step) * step;
			}
			_selectedReference.F = f;
			e.SyncPreview();
			PreviewRoot.QueueRedraw();
			return;
		}
		mousePoint1 = ToPreviewLocal(mb.Position);
		var min = new Vector2(
			Math.Min(mousePoint0.X, mousePoint1.X),
			Math.Min(mousePoint0.Y, mousePoint1.Y)
		);
		var max = new Vector2(
			Math.Max(mousePoint0.X, mousePoint1.X),
			Math.Max(mousePoint0.Y, mousePoint1.Y)
		);
		for (int i = 0; i < e.levelData.References.Count; i++)
		{
			var r = e.levelData.References[i];
			bool alive = false;
			if (r.Type == ModelType.Projectile)
			{
				var proj = e.ProjectileModels[r.Id];
				alive = _ctx.T >= 0 && _ctx.T <= proj.Lifetime;
			}
			else if (r.Type == ModelType.Pattern)
			{
				var patt = e.PatternModels[r.Id];
				var lt = e.CurrentTime - r.T;
				alive = lt >= 0 && lt <= patt.lifetime;
			}
			if (!alive) 
				continue;
			var pos = r.Type == ModelType.Pattern ? new(r.SpawnX, r.SpawnY) : r.Pos;
			if (pos.X >= min.X && pos.X <= max.X &&
				pos.Y >= min.Y && pos.Y <= max.Y)
			{
				_groupSelection.Add(r);
			}
		}
	}
	private void HandleSelectLMBRelease()
	{
		lmbDragging = false;
	}
	private void HandleDeleteRelease()
	{
		lmbDragging = false;
		_inGroup = false;
	}
	private void HandlePlaceMM(InputEventMouseMotion mm)
	{
		if (_selectedReference != null && lmbDragging)
		{
			var local = ToPreviewLocal(mm.Position);
			var rPos = new Vector2(_selectedReference.SpawnX, _selectedReference.SpawnY);
			float f = (rPos - local).Angle()+(Mathf.Pi/2);
			if (snap)
			{
				float step = Mathf.Pi / ConfigHelper.Current.AngleSnapDivision;
				f = Mathf.Round(f / step) * step;
			}
			_selectedReference.F = f;
			
			e.SyncPreview();
			PreviewRoot.QueueRedraw();
		}
	}
	private void HandleSelectMM(InputEventMouseMotion mm)
	{
		if (_selectedReference != null && lmbDragging && !rmbDragging) // have a selected reference and holding lmb but not holding rmb
		{
			var local = ToPreviewLocal(mm.Position);
			_selectedReference.SpawnX = local.X;
			_selectedReference.SpawnY = local.Y;
			if (_inGroup) // and in group
			{
				foreach (var r in _groupSelection)
				{
					var rPos = new Vector2(r.SpawnX, r.SpawnY);
					var offset = rPos - local; // local is mouse pos
					r.SpawnX = (local+offset).X;
					r.SpawnY = (local+offset).Y;
				}
			}
			e.SyncPreview();
			PreviewRoot.QueueRedraw();
		}
		else if (_selectedReference != null && rmbDragging) // have a selected reference and holding rmb
		{
			var local = ToPreviewLocal(mm.Position);
			var selectedPos = new Vector2(_selectedReference.SpawnX, _selectedReference.SpawnY);
			float f = (selectedPos - local).Angle()+(Mathf.Pi/2);
			if (snap)
			{
				float step = Mathf.Pi / ConfigHelper.Current.AngleSnapDivision;
				f = Mathf.Round(f / step) * step;
			}
			_selectedReference.F = f;
			if (_inGroup) // and in group
			{
				foreach (var r in _groupSelection)
				{
					if (r == _selectedReference) continue;
					if (lmbDragging) // and holding mb, then point every selected projectile at cursor
					{
						var rPos = new Vector2(r.SpawnX, r.SpawnY);
						float rf = (rPos - local).Angle()+(Mathf.Pi/2);
						if (snap)
						{
							float step = Mathf.Pi / ConfigHelper.Current.AngleSnapDivision;
							rf = Mathf.Round(rf / step) * step;
						}
						r.F = rf;
					}
					else // otherwise make the follow whatever is being set
					{
						r.F = f;
					}
				}
			}
			e.SyncPreview();
			PreviewRoot.QueueRedraw();
		}
		else if (_selectedReference == null && rmbDragging) // dont have a reference and holding rmb
		{
			mousePoint1 = ToPreviewLocal(mm.Position);
			PreviewRoot.QueueRedraw();
		}
	}
	private void HandleDeleteMM(InputEventMouseMotion mm)
	{
		if (lmbDragging)
		{
			var r = GetNearestReference(mm.Position);
			if (r != null)
			e.DeleteReference(r);
				lmbDragging = true;
				_selectedReference = null;
		}
	}
	public EditorReference GetNearestReference(Vector2 mpos)
	{
		var local = ToPreviewLocal(mpos);
		float nearDist = float.MaxValue;
		EditorReference nearRef = null;
        foreach (var r in e.levelData.References)
        {
			if (r.Type == ModelType.Pattern)
			{
				var patt = e.PatternModels[r.Id];
				var lt = e.CurrentTime - r.T;
				bool alive = lt >= 0 && lt <= patt.lifetime;
				if (!alive)
                	continue;
				float dist = new Vector2(r.SpawnX, r.SpawnY).DistanceTo(local);
				if (dist < nearDist)
				{
					nearDist = dist;
					nearRef = r;
				}
			}
			else
			{
				double lt = e.CurrentTime - r.T;
				var proj = e.ProjectileModels[r.Id];
				bool alive = lt >= 0 && lt <= proj.Lifetime;
				if (!alive)
					continue;
				float dist = r.Pos.DistanceTo(local);
				if (dist < nearDist)
				{
					nearDist = dist;
					nearRef = r;
				}
			}
			
        }
        if (nearRef != null && nearDist < 20)
            return (nearRef);
        return (null);
	}
	private void DrawGizmos(EditorReference r)
    {
		var c0 = new Vector2(mousePoint0.X, mousePoint1.Y);
		var c1 = new Vector2(mousePoint1.X, mousePoint0.Y);
		Vector2[] grabbox = [mousePoint0, c0, mousePoint1, c1, mousePoint0];
		PreviewRoot.DrawPolyline(grabbox, Colors.DarkRed, 3);
		if (r == null)
		{
			return;
		}
        int steps = ConfigHelper.Current.PathFidelity;
		if (r.Type == ModelType.Projectile)
		{
			var proj = e.ProjectileModels[r.Id];
			Vector2[] points = new Vector2[steps];
			var lctx = new EvalContext();
			for (int i = 0; i < steps; i++)
			{
				lctx.T = Math.Min(proj.Lifetime,ConfigHelper.Current.MaxPathLength) / steps * i;
				var (x, y) = CalculatePosDelta(proj.efnx, proj.efny, lctx, r.F);
				points[i] = new(r.SpawnX+x,r.SpawnY+y);
			}
			PreviewRoot.DrawPolyline(points, RenderingUtils.ColorFromString(proj.Name), 2f, true);
			PreviewRoot.DrawCircle(points[0], 4f, RenderingUtils.ColorFromString(proj.Name));
			PreviewRoot.DrawDashedLine(r.Pos, r.Pos+(Vector2.FromAngle((float)r.F+(Mathf.Pi/2))*50), Colors.DarkRed, 4f);
		}
		else if (r.Type == ModelType.Pattern)
		{
			var patt = e.PatternModels[r.Id];
			Vector2[] points = new Vector2[steps];
			var lctx = new EvalContext() {N = patt.Count};
			for (int i = 0; i < steps; i++)
			{
				lctx.I = lctx.N / steps * i;
				var (x, y) = CalculatePosDelta(patt.efnx, patt.efny, lctx, r.F);
				points[i] = new(r.SpawnX+x,r.SpawnY+y);
				PreviewRoot.DrawCircle(points[i], 4f, RenderingUtils.ColorFromString(e.ProjectileModels[patt.ProjectileId].Name));
			}
			PreviewRoot.DrawPolyline(points, RenderingUtils.ColorFromString(patt.Name), 2f, true);
			var spawnPos = new Vector2(r.SpawnX, r.SpawnY);
			PreviewRoot.DrawDashedLine(spawnPos, spawnPos+(Vector2.FromAngle((float)r.F+(Mathf.Pi/2))*50), Colors.DarkRed, 4f);
		}
    }
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
	// only call on load
	public void CompileAll()
	{
		Console.Instance.Log("[LevelPreview] Attempting to compile");
		for (int i = 0; i < Editor.MaxModelCount; i++)
        {
            var m = e.ProjectileModels[i];
			CompileProjectile(m);
        }
        for (int i = 0; i < Editor.MaxModelCount; i++)
        {
            var m = e.PatternModels[i];
			CompilePattern(m);
        }
		Console.Instance.Log("[LevelPreview] Completed compile all");
	}
	public void Sync(IEditorModel _) =>
		Sync();
	private EvalContext _ctx = new();
	public void Sync()
	{
		List<EditorReference> patternReferences = new();
		// clear indices
		for (int g = 0; g < _renderGroups.Count; g++)
		{
			_renderGroups[g].PatternBulletIndices.Clear();
			_renderGroups[g].BulletIndices.Clear();
		}
		// tick references
		for (int i = 0; i < e.levelData.References.Count; i++)
		{
			var r = e.levelData.References[i];
			if (r.Type == ModelType.Projectile)
			{
				_ctx.T = e.CurrentTime - r.T;
				var proj = e.ProjectileModels[r.Id];
				bool alive = _ctx.T >= 0 && _ctx.T <= proj.Lifetime;
				if (!alive) continue;
				var pos = CalculatePosDelta(proj.efnx, proj.efny, _ctx, r.F);
				r.Pos = new Vector2(r.SpawnX, r.SpawnY) + pos;
				_renderGroups[proj.RenderGroupId].BulletIndices.Add(i);
			}
			else if (r.Type == ModelType.Pattern)
			{
				var patt = e.PatternModels[r.Id];
				var proj = e.ProjectileModels[patt.ProjectileId];
				var lctx = new EvalContext() { N = patt.Count };
				for (int j = 0; j < patt.Count; j++)
				{
					lctx.I = j;
					double t = patt.efnt(lctx);
					lctx.T = e.CurrentTime - r.T + t;
					bool alive = lctx.T >= 0 && lctx.T <= proj.Lifetime;
					if (!alive) continue;
					double fwd = patt.efnfwd(lctx);
					var spawnPos = CalculatePosDelta(patt.efnx, patt.efny, lctx, r.F);
					var movement = CalculatePosDelta(proj.efnx, proj.efny, lctx, r.F+fwd);
					var pr = new EditorReference()
					{
						Pos = new Vector2(r.SpawnX, r.SpawnY) + spawnPos + movement,
						T = r.T + t,
						F = r.F + fwd,
						Type = ModelType.Projectile
					};
					patternReferences.Add(pr);
					_renderGroups[proj.RenderGroupId].PatternBulletIndices.Add(patternReferences.Count - 1);
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
			int total = count + patternCount;
			group.MultiMesh.InstanceCount = total;
			if (total == 0) continue;

			int required = total * floatsPerInstance;
			if (_groupBuffers[g].Length != required)
				_groupBuffers[g] = new float[required];
			ref float[] buffer = ref _groupBuffers[g];

			// draw references (this is fine)
			for (int n = 0; n < count; n++)
			{
				int idx = group.BulletIndices[n];
				EditorReference r = e.levelData.References[idx];
				var proj = e.ProjectileModels[r.Id];
				float scale = proj.RenderScale;
				int oj = n * floatsPerInstance;
				buffer[oj + 0] = 0;
				buffer[oj + 1] = scale;
				buffer[oj + 2] = 0;
				buffer[oj + 3] = r.Pos.X;
				buffer[oj + 4] = scale;
				buffer[oj + 5] = 0;
				buffer[oj + 6] = 0;
				buffer[oj + 7] = r.Pos.Y;
			}
			// draw references in pattern projectiles
			for (int n = count; n < total; n++)
			{
				int idx = group.PatternBulletIndices[n - count]; // fixed index
				EditorReference r = patternReferences[idx];
				var model = e.ProjectileModels[r.Id];
				float scale = model.RenderScale;
				int o = n * floatsPerInstance;
				buffer[o + 0] = 0;
				buffer[o + 1] = scale;
				buffer[o + 2] = 0;
				buffer[o + 3] = r.Pos.X;
				buffer[o + 4] = scale;
				buffer[o + 5] = 0;
				buffer[o + 6] = 0;
				buffer[o + 7] = r.Pos.Y;
			}

			RenderingServer.MultimeshSetBuffer(group.MultiMesh.GetRid(), buffer);
		}
	}
	public void CompileProjectile(ProjectileModel model)
	{
		if (model == null)
			return;
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
				var rendergroup = CreateRenderGroup(mesh, tex, PreviewRoot);
				_renderGroups.Add(rendergroup);
				_groupBuffers[_renderGroups.Count - 1] = [];
				textMap[tex] = _renderGroups.Count - 1;
			}
		}
		else if (model.UseShape && model.Shape != null)
		{
			Vector2[] shape = [.. model.Shape.Select(p => new Vector2(p[0], p[1]))];
			var mesh = RenderingUtils.BuildPolygonMesh(shape);
			var rendergroup = CreateRenderGroup(mesh, null, PreviewRoot);
			_renderGroups.Add(rendergroup);
			_groupBuffers[_renderGroups.Count - 1] = [];
			renderGroupId = _renderGroups.Count - 1;
		}
		else
		{
			var mesh = RenderingUtils.BuildCircleMesh(model.Radius);
			var rendergroup = CreateRenderGroup(mesh, null, PreviewRoot);
			_renderGroups.Add(rendergroup);
			_groupBuffers[_renderGroups.Count - 1] = [];
			renderGroupId = _renderGroups.Count - 1;
		}
		model.RenderGroupId = renderGroupId;
		PreviewRoot.QueueRedraw();
	}
	public void CompilePattern(PatternModel model)
	{
		if (model == null)
			return;
		model.efnx = ExpressionHandler.Compile(ExpressionHandler.Parse(model.FunctionX));
		model.efny = ExpressionHandler.Compile(ExpressionHandler.Parse(model.FunctionY));
		model.efnt = ExpressionHandler.Compile(ExpressionHandler.Parse(model.FunctionT));
		model.efnfwd = ExpressionHandler.Compile(ExpressionHandler.Parse(model.FunctionFwd));
		var proj = e.ProjectileModels[model.ProjectileId];
		var lctx = new EvalContext { N = model.Count };
		double maxSpawnT = 0;
		for (int j = 0; j < model.Count; j++)
		{
			lctx.I = j;
			double t = model.efnt(lctx);
			if (t > maxSpawnT)
				maxSpawnT = t;
		}
		model.lifetime = (float)(maxSpawnT + proj.Lifetime);
		PreviewRoot.QueueRedraw();
	}
	private static Vector2 CalculatePosDelta(Func<EvalContext, double> efnx, Func<EvalContext, double> efny, EvalContext ctx, double f = 0)
	{
		double xTravel = efnx(ctx);
		double yTravel = efny(ctx);
		double cos = Math.Cos(f);
		double sin = Math.Sin(f);
		float x = (float)(cos * xTravel - sin * yTravel);
		float y = (float)(sin * xTravel + cos * yTravel);
		var pos = new Vector2(x, y);
		return pos;
	}
	private static EditorRenderGroup CreateRenderGroup(Mesh mesh, Texture2D tex, Node2D root)
	{
		var multiMesh = new MultiMesh
        {
            Mesh = mesh,
            TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
            InstanceCount = 0,
        };
        var node = new MultiMeshInstance2D { Multimesh = multiMesh, Texture = tex };
        root.AddChild(node);
        return new EditorRenderGroup { Mesh = mesh, Texture = tex, Node = node, MultiMesh = multiMesh};
	}
}
