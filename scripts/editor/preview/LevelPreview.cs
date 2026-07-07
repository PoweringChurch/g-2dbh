using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Godot;

public partial class LevelPreview : Control
{
	[Export] Node2D PreviewRoot;
	[Export] TextureRect BackgroundImage;
	[Export] SubViewport PreviewVP;
	private Editor e;
	private float resScale = 1;
	private List<RenderGroup> _renderGroups = new();
	private float[][] _groupBuffers = new float[Editor.MaxModelCount][];
	public override void _Ready()
	{
		e = GetNode<Editor>("/root/Editor");
		GetTree().Root.SizeChanged += OnWindowResized;
		PreviewRoot.Draw += () => DrawGizmos(SelectedReference);
	}
	public EditorReference selectedReference;
	public EditorReference SelectedReference
	{
		get => selectedReference;
		set
		{
			selectedReference = value;
			_inGroup = groupSelection.Find((s)=> s == SelectedReference) != null;
			e.UpdateInspector();
		}
	}
	private int selectedIndex;
	private bool _inGroup = false;
    private bool lmbDragging = false;
    private bool rmbDragging = false;
	private bool snap => e.Snap;
	private Vector2 currentMpos = Vector2.Zero;
	private Vector2 mousePoint0;
	private Vector2 mousePoint1;
	private List<EditorReference> groupSelection = new();
	private List<int> groupSelectionIndices = new();
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
					case Editor.Mode.Select: HandleSelectLMBRelease(mb); break;
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
			currentMpos = mm.Position;
			switch (e.CurrentMode)
			{
				case Editor.Mode.Place: HandlePlaceMM(mm); break;
				case Editor.Mode.Select: HandleSelectMM(mm); break;
				case Editor.Mode.Delete: HandleDeleteMM(mm); break;
			}
			PreviewRoot.QueueRedraw();
		}
    }
	private void HandlePlacePress(InputEventMouseButton mb)
	{
		if (e.SelectedModel == null)
			return;
		var local = ToPreviewLocal(mb.Position);
		var pos = snap ? local.Snapped(ConfigHelper.Current.GridSnapCellSize): local;
		var newRef = new EditorReference
		{
			Id = e.SelectedModel.Id,
			Type = e.SelectedModel is ProjectileModel ? ModelType.Projectile : ModelType.Pattern,
			T = e.CurrentTime,
			SpawnX = pos.X,
			SpawnY = pos.Y,
			Pos = pos
		};
		lmbDragging = true;
		SelectedReference = newRef;
		e.AddReference(newRef);
		selectedIndex = e.levelData.References.Count - 1;
		groupSelection.Clear();
		UpdateReferenceInEditor(SelectedReference, selectedIndex);
	}
	private void HandleSelectRMBPress(InputEventMouseButton mb)
	{
		rmbDragging = true;
		if (SelectedReference != null) // dont have a selected reference
			return;
		else if (groupSelection.Count > 0) // but do have a group
		{
			groupSelection.Clear();
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
		var (r, i) = GetNearestReference(mb.Position);
		SelectedReference = r;
		selectedIndex = i;
		if (!_inGroup)
		{
			mousePoint0 = Vector2.Zero;
			mousePoint1 = Vector2.Zero;
			groupSelection.Clear();
		}
		PreviewRoot.QueueRedraw();
		UpdateReferenceInEditor(SelectedReference, selectedIndex);
	}
	private void HandleDeletePress(InputEventMouseButton mb)
	{
		var (r, i) = GetNearestReference(mb.Position);
		SelectedReference = r;
		if (SelectedReference != null) // if we got a reference
		{
			if (_inGroup) // if the reference is in the selection group
			{
				for (int j = 0; j < groupSelection.Count; j++)
				{
					var sr = groupSelection[j];
					var si = groupSelectionIndices[j];
					e.DeleteReference(sr);
					UpdateReferenceInEditor(null, si);
				}
			}
			else
			{
				int idToRemove = selectedIndex;
				e.DeleteReference(SelectedReference);
				UpdateReferenceInEditor(null, idToRemove);
			}
		}
		lmbDragging = true;
		SelectedReference = null;
		selectedIndex = -1;
	}
	private void HandlePlaceRelease(InputEventMouseButton mb)
	{
		if (e.SelectedModel == null)
			return;
		var local = ToPreviewLocal(mb.Position);
		var rPos = new Vector2(SelectedReference.SpawnX, SelectedReference.SpawnY);
		float f = (rPos - local).Angle()+(Mathf.Pi/2);
		if (snap)
		{
			float step = Mathf.Pi / ConfigHelper.Current.AngleSnapDivision;
			f = Mathf.Round(f / step) * step;
		}
		SelectedReference.F = f;
		UpdateReferenceInEditor(SelectedReference, selectedIndex);
		lmbDragging = false;
		SelectedReference = null;
		Sync();
		PreviewRoot.QueueRedraw();
	}
	private void HandleSelectRMBRelease(InputEventMouseButton mb)
	{
		rmbDragging = false;
		if (_inGroup) // if we are grabbing something inside of the group
		{	
			var local = ToPreviewLocal(mb.Position);
			var rPos = new Vector2(SelectedReference.SpawnX, SelectedReference.SpawnY);
			float f = (rPos - local).Angle()+(Mathf.Pi/2);
			if (snap)
			{
				float step = Mathf.Pi / ConfigHelper.Current.AngleSnapDivision;
				f = Mathf.Round(f / step) * step;
			}
			SelectedReference.F = f;
			UpdateReferenceInEditor(SelectedReference, selectedIndex);
			Sync();
			PreviewRoot.QueueRedraw();
			return;
		}
		if (SelectedReference != null) return;
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
				groupSelection.Add(r);
				groupSelectionIndices.Add(i);
			}
		}
	}
	private void HandleSelectLMBRelease(InputEventMouseButton mb)
	{
		lmbDragging = false;
		if (SelectedReference != null && !rmbDragging) // have a selected reference and holding lmb but not holding rmb
		{
			var local = ToPreviewLocal(mb.Position);
			var pos = snap ? local.Snapped(ConfigHelper.Current.GridSnapCellSize) : local;
			var oldAnchorPos = new Vector2(SelectedReference.SpawnX, SelectedReference.SpawnY);
			SelectedReference.SpawnX = pos.X;
			SelectedReference.SpawnY = pos.Y;
			if (_inGroup) // and in group
			{
				var delta = pos - oldAnchorPos;
				foreach (var r in groupSelection)
				{
					if (r == SelectedReference) continue; 
					r.SpawnX += delta.X;
					r.SpawnY += delta.Y;
				}
			}
			UpdateReferenceInEditor(SelectedReference, selectedIndex);
			Sync();
		}
	}
	private void HandleDeleteRelease()
	{
		lmbDragging = false;
	}
	private void HandlePlaceMM(InputEventMouseMotion mm)
	{
		if (SelectedReference != null && lmbDragging)
		{
			var local = ToPreviewLocal(mm.Position);
			var rPos = new Vector2(SelectedReference.SpawnX, SelectedReference.SpawnY);
			float f = (rPos - local).Angle()+(Mathf.Pi/2);
			if (snap)
			{
				float step = Mathf.Pi / ConfigHelper.Current.AngleSnapDivision;
				f = Mathf.Round(f / step) * step;
			}
			SelectedReference.F = f;

			UpdateReferenceInEditor(SelectedReference, selectedIndex);
			Sync();
		}
	}
	private void HandleSelectMM(InputEventMouseMotion mm)
	{
		if (SelectedReference != null && lmbDragging && !rmbDragging) // have a selected reference and holding lmb but not holding rmb
		{
			var local = ToPreviewLocal(mm.Position);
			var pos = snap ? local.Snapped(ConfigHelper.Current.GridSnapCellSize) : local;
			var oldAnchorPos = new Vector2(SelectedReference.SpawnX, SelectedReference.SpawnY);
			SelectedReference.SpawnX = pos.X;
			SelectedReference.SpawnY = pos.Y;
			if (_inGroup)
			{
				var delta = pos - oldAnchorPos;
				for (int i = 0; i < groupSelection.Count; i++)
				{
					var r = groupSelection[i];
					if (r == SelectedReference) continue; 
					r.SpawnX += delta.X;
					r.SpawnY += delta.Y;
					UpdateReferenceInEditor(r, groupSelectionIndices[i]);
				}
			}
			UpdateReferenceInEditor(SelectedReference, selectedIndex);
			Sync();
		}
		else if (SelectedReference != null && rmbDragging) // have a selected reference and holding rmb
		{
			var local = ToPreviewLocal(mm.Position);
			var selectedPos = new Vector2(SelectedReference.SpawnX, SelectedReference.SpawnY);
			float f = (selectedPos - local).Angle()+(Mathf.Pi/2);
			if (snap)
			{
				float step = Mathf.Pi / ConfigHelper.Current.AngleSnapDivision;
				f = Mathf.Round(f / step) * step;
			}
			SelectedReference.F = f;
			if (_inGroup) // and in group
			{
				for (int i = 0; i < groupSelection.Count; i++)
				{
					var r = groupSelection[i];
					if (r == SelectedReference) continue;
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
						UpdateReferenceInEditor(r, groupSelectionIndices[i]);
					}
					else // otherwise make the follow whatever is being set
					{
						r.F = f;
						UpdateReferenceInEditor(r, groupSelectionIndices[i]);
					}
				}
			}
			UpdateReferenceInEditor(SelectedReference, selectedIndex);
			Sync();
		}
		else if (SelectedReference == null && rmbDragging) // dont have a reference and holding rmb
		{
			mousePoint1 = ToPreviewLocal(mm.Position);
		}
	}
	private void HandleDeleteMM(InputEventMouseMotion mm)
	{
		if (lmbDragging)
		{
			var (r,i) = GetNearestReference(mm.Position);
			if (r != null)
			e.DeleteReference(r);
			UpdateReferenceInEditor(null, i);
			lmbDragging = true;
			SelectedReference = null;
		}
	}
	public (EditorReference, int i) GetNearestReference(Vector2 mpos)
	{
		var local = ToPreviewLocal(mpos);
		float nearDist = float.MaxValue;
		EditorReference nearRef = null;
		int foundAt = -1;
		for (int i = 0; i < e.levelData.References.Count; i++)
		{
			var r = e.levelData.References[i];
			if (r.Type == ModelType.Pattern)
			{
				var patt = e.PatternModels[r.Id];
				if (patt == null)
					continue;
				var lt = e.CurrentTime - r.T;
				bool alive = lt >= 0 && lt <= patt.lifetime;
				if (!alive)
                	continue;
				float dist = new Vector2(r.SpawnX, r.SpawnY).DistanceTo(local);
				if (dist < nearDist)
				{
					nearDist = dist;
					nearRef = r;
					foundAt = i;
				}
			}
			else
			{
				var proj = e.ProjectileModels[r.Id];
				if (proj == null)
					continue;
				double lt = e.CurrentTime - r.T;
				bool alive = lt >= 0 && lt <= proj.Lifetime;
				if (!alive)
					continue;
				float dist = r.Pos.DistanceTo(local);
				if (dist < nearDist)
				{
					nearDist = dist;
					nearRef = r;
					foundAt = i;
				}
			}
		}
        if (nearRef != null && nearDist < 20)
            return (nearRef, foundAt);
        return (null, -1);
	}
	public EditorReference[] GetNearestReferences(Vector2 mpos, int count)
	{
		if (e.levelData == null) return [];
		var local = ToPreviewLocal(mpos);
		var validRefs = new List<(EditorReference r, float d)>();
		foreach (var r in e.levelData.References)
		{
			float dist;
			if (r.Type == ModelType.Pattern)
			{
				var patt = e.PatternModels[r.Id];
				var lt = e.CurrentTime - r.T;
				bool alive = lt >= 0 && lt <= patt.lifetime;
				if (!alive)
					continue;
				dist = new Vector2(r.SpawnX, r.SpawnY).DistanceTo(local);
			}
			else
			{
				double lt = e.CurrentTime - r.T;
				var proj = e.ProjectileModels[r.Id];
				bool alive = lt >= 0 && lt <= proj.Lifetime;
				if (!alive)
					continue;
				dist = r.Pos.DistanceTo(local);
			}
			if (dist < 60)
			{
				validRefs.Add((r, dist));
			}
		}

		// sort by distance
		return [.. validRefs
			.OrderBy(x => x.d)
			.Take(count)
			.Select(x => x.r)];
	}
	private void DrawGizmos(EditorReference r)
    {
		// grid
		if (snap)
		{
			var local = ToPreviewLocal(currentMpos);
			var cellsize = ConfigHelper.Current.GridSnapCellSize;
			float centerGridX = Mathf.Round(local.X / cellsize) * cellsize;
			float centerGridY = Mathf.Round(local.Y / cellsize) * cellsize;
			
			int gridRadiusCells = 3;
			float lineLength = gridRadiusCells * cellsize; 
			Color gridColor = new(1.0f, 1.0f, 1.0f, 0.1f);
			float lineWidth = 1.5f;
			for (int i = -gridRadiusCells; i <= gridRadiusCells; i++)
			{
				float currentX = centerGridX + (i * cellsize);
				
				Vector2 start = new Vector2(currentX, centerGridY - lineLength - 10);
				Vector2 end = new Vector2(currentX, centerGridY + lineLength + 10);
				PreviewRoot.DrawLine(start, end, gridColor, lineWidth);
			}
			for (int j = -gridRadiusCells; j <= gridRadiusCells; j++)
			{
				float currentY = centerGridY + (j * cellsize);
				
				Vector2 start = new Vector2(centerGridX - lineLength - 10, currentY);
				Vector2 end = new Vector2(centerGridX + lineLength + 10, currentY);
				PreviewRoot.DrawLine(start, end, gridColor, lineWidth);
			}
		}
		// selection box
		var c0 = new Vector2(mousePoint0.X, mousePoint1.Y);
		var c1 = new Vector2(mousePoint1.X, mousePoint0.Y);
		Vector2[] grabbox = [mousePoint0, c0, mousePoint1, c1, mousePoint0];
		PreviewRoot.DrawPolyline(grabbox, Colors.DarkRed, 3);
		// nearby references
		var nearest = GetNearestReferences(currentMpos, 5);
		foreach (var nr in nearest)
		{
			var name = nr.Type == ModelType.Pattern ? e.PatternModels[nr.Id].Name : e.ProjectileModels[nr.Id].Name;
			PreviewRoot.DrawCircle(nr.Pos, 4, RenderingUtils.ColorFromString(name), false);
		}
		if (r == null)
			return;
		// label for selected
		var labelPos = r.Pos + new Vector2(15, -15);
		string infoText = $"{r.Type} ID : {r.Id}";
		Font defaultFont = GetThemeDefaultFont();
		int fontSize = 12;
		PreviewRoot.DrawString(defaultFont, labelPos, infoText, HorizontalAlignment.Left, -1, fontSize, Colors.MediumVioletRed);
		// path for projectiles
        int steps = ConfigHelper.Current.PathFidelity;
		if (r.Type == ModelType.Projectile)
		{
			var proj = e.ProjectileModels[r.Id];
			if (proj == null)
				return;
			Vector2[] points = new Vector2[steps];
			var lctx = new EvalContext();
			for (int i = 0; i < steps; i++)
			{
				lctx.T = Math.Min(proj.Lifetime,ConfigHelper.Current.MaxPathLength) / steps * i;
				lctx.L = proj.Lifetime;
				var (x, y) = CalculatePosDelta(proj.fnx, proj.fny, lctx, r.F);
				points[i] = new(r.SpawnX+x,r.SpawnY+y);
			}
			PreviewRoot.DrawPolyline(points, RenderingUtils.ColorFromString(proj.Name), ConfigHelper.Current.PathThickness, true);
			PreviewRoot.DrawCircle(points[0], ConfigHelper.Current.PathThickness*1.5f, RenderingUtils.ColorFromString(proj.Name));
			PreviewRoot.DrawDashedLine(r.Pos, r.Pos+(Vector2.FromAngle((float)r.F+(Mathf.Pi/2))*50), Colors.DarkRed, 4f);
		}
		// path for patterns
		else if (r.Type == ModelType.Pattern)
		{
			var patt = e.PatternModels[r.Id];
			if (patt == null)
				return;
			Vector2[] points = new Vector2[steps];
			var lctx = new EvalContext() {N = patt.Count};
			for (int i = 0; i < steps; i++)
			{
				lctx.I = lctx.N / steps * i;
				var (x, y) = CalculatePosDelta(patt.fnx, patt.fny, lctx, r.F);
				points[i] = new(r.SpawnX+x,r.SpawnY+y);
				PreviewRoot.DrawCircle(points[i], ConfigHelper.Current.PathThickness*1.5f, RenderingUtils.ColorFromString(e.ProjectileModels[patt.ProjectileId].Name));
			}
			PreviewRoot.DrawPolyline(points, RenderingUtils.ColorFromString(patt.Name), ConfigHelper.Current.PathThickness, true);
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
		Console.Inst.Log("[LevelPreview] Attempting to compile");
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
		for (int i = 0; i < e.levelData.References.Count; i++)
		{
			var r = e.levelData.References[i];
			r.RootId = i;
			UpdateReferenceInEditor(r,i);
		}
		Sync();
		Console.Inst.Log("[LevelPreview] Completed compile all");
	}
	public void Sync(IEditorModel _) =>
		Sync();
	List<EditorReference> _bakedTimeline = new();
	private EvalContext _ctx = new();
	public void Sync()
	{
		// clear indices
		for (int g = 0; g < _renderGroups.Count; g++)
		{
			_renderGroups[g].BakeIndices.Clear();
		}
		// tick references
		for (int i = 0; i < _bakedTimeline.Count; i++)
		{
			var r = _bakedTimeline[i];
			if (ProcessProjReference(r)) 
			{
				var proj = e.ProjectileModels[r.Id];
				_renderGroups[proj.RenderGroupId].BakeIndices.Add(i);
			}
		}
		// draw references
		const int floatsPerInstance = 8;
		for (int g = 0; g < _renderGroups.Count; g++)
		{
			var group = _renderGroups[g];
			int count = group.BakeIndices.Count;
			if (count == 0) continue;
			group.MultiMesh.InstanceCount = count;
			int required = count * floatsPerInstance;
			if (_groupBuffers[g].Length != required)
				_groupBuffers[g] = new float[required];
			ref float[] buffer = ref _groupBuffers[g];

			// draw references
			for (int n = 0; n < count; n++)
			{
				int idx = group.BakeIndices[n];
				EditorReference r = _bakedTimeline[idx];
				var proj = e.ProjectileModels[r.Id];
				float scale = proj.RenderScale;
				int o = n * floatsPerInstance;
				float drawForward = (float)r.F-Mathf.Pi; // rads

				float cos = Mathf.Cos(drawForward);
				float sin = Mathf.Sin(drawForward);
				buffer[o + 0] = -scale * cos; // shear x
				buffer[o + 1] = -scale * sin;
				buffer[o + 2] = 0;
				buffer[o + 3] = r.Pos.X;

				buffer[o + 4] = -scale * sin;
				buffer[o + 5] = scale * cos; // shear y
				buffer[o + 6] = 0;
				buffer[o + 7] = r.Pos.Y;
			}
			RenderingServer.MultimeshSetBuffer(group.MultiMesh.GetRid(), buffer);
		}
	}
	private bool ProcessProjReference(EditorReference r)
	{
		var proj = e.ProjectileModels[r.Id];
		if (proj == null) return false;
		_ctx.T = e.CurrentTime - r.T;
		_ctx.L = proj.Lifetime;
		bool alive = _ctx.T >= 0 && _ctx.T <= proj.Lifetime;
		if (!alive) return false;
		var pos = CalculatePosDelta(proj.fnx, proj.fny, _ctx, r.F);
		r.Pos = new Vector2(r.SpawnX, r.SpawnY) + pos;
		return true;
	}
	public void UpdateReferenceInEditor(EditorReference modifiedRef, int modifiedId)
	{
		_bakedTimeline.RemoveAll(x => x.RootId == modifiedId);
		if (modifiedRef == null) return;
		Queue<EditorReference> processingQueue = new();
		processingQueue.Enqueue(modifiedRef);

		while (processingQueue.Count > 0)
		{
			var current = processingQueue.Dequeue();
			current.RootId = modifiedId;
			if (current.Type == ModelType.Projectile)
			{
				_bakedTimeline.Add(current);
				var proj = e.ProjectileModels[current.Id];
				if (proj.SpawnModelOnDeath && current.Depth < proj.MaxDepth)
				{
					var childRef = new EditorReference()
					{
						SpawnX = current.Pos.X,
						SpawnY = current.Pos.Y,
						F = current.F,
						T = e.CurrentTime,
						Type = proj.SpawnOnDeathType,
						RootId = modifiedId,
						Id = proj.SpawnOnDeathId,
						Depth = current.Depth + 1,
					};
					processingQueue.Enqueue(childRef);
				}
			}
			else if (current.Type == ModelType.Pattern)
			{
				UnpackPatternIntoQueue(current, processingQueue);
			}
		}
	}
	private void UnpackPatternIntoQueue(EditorReference patternRef, Queue<EditorReference> queue)
	{
		var patt = e.PatternModels[patternRef.Id];
		if (patt == null) return;
		var proj = e.ProjectileModels[patt.ProjectileId];
		if (proj == null) return;

		var lctx = new EvalContext { N = patt.Count > 1 ? patt.Count - 1 : 1, L = proj.Lifetime };
		Vector2 basePos = new Vector2(patternRef.SpawnX, patternRef.SpawnY);

		for (int j = 0; j < patt.Count; j++)
		{
			lctx.I = j;
			double spawnDelay = patt.fnt(lctx);
			double childAbsoluteSpawnTime = patternRef.T + spawnDelay;

			lctx.T = 0;
			double fwdOffset = patt.fnf(lctx);
			Vector2 spawnOffset = CalculatePosDelta(patt.fnx, patt.fny, lctx, patternRef.F);

			Vector2 childAbsoluteSpawnPos = basePos + spawnOffset;

			var subBulletRef = new EditorReference
			{
				Id = patt.ProjectileId,
				Type = ModelType.Projectile,
				RootId = patternRef.RootId, // yes it is meant to be like this
				T = childAbsoluteSpawnTime,
				SpawnX = childAbsoluteSpawnPos.X,
				SpawnY = childAbsoluteSpawnPos.Y,
				F = patternRef.F + fwdOffset
			};
			queue.Enqueue(subBulletRef);
		}
	}
	public void CompileProjectile(ProjectileModel model)
	{
		if (model == null)
			return;
        _renderGroups.Add(LevelCompiler.RenderGroupFromProjectile(model, $"user://data/levels/{e.levelData.LevelId}/images/",  PreviewRoot));
        int renderGroupId = _renderGroups.Count - 1;
		_groupBuffers[renderGroupId] = [];
        // create render group
        // set everything
        LevelCompiler.CompileProjectile(model, renderGroupId);
		PreviewRoot.QueueRedraw();
	}
	public void CompilePattern(PatternModel model)
	{
		if (model == null)
			return;
		LevelCompiler.CompilePattern(model);
		var proj = e.ProjectileModels[model.ProjectileId];
		var lctx = new EvalContext { N = model.Count };
		double maxSpawnT = 0;
		for (int j = 0; j < model.Count; j++)
		{
			lctx.I = j;
			double t = model.fnt(lctx);
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
}
