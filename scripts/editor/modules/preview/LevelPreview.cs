using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class LevelPreview : Control
{
	public const float PreviewScale = 0.8f;
	private EvalContext gctx = new();
	private float resScale = 1;
	private EditorLevelManager levelManager;
	[Export] Node2D PreviewRoot;
	[Export] SubViewport PreviewVP;
	private Editor e => Editor.Instance;
	public List<BackgroundLayerInstance> BGInstances => levelManager.BGInstances;
	public override void _Ready()
	{
		GetTree().Root.SizeChanged += OnWindowResized;
		PreviewRoot.Draw += () => DrawGizmos(SelectedReference);
		levelManager = new(PreviewRoot);
		e.Copy += Copy;
		e.Paste += Paste;
		e.Cut += Cut;
		e.Delete += QuickDelete;
	}
	public EditorReference selectedReference;
	public EditorReference SelectedReference
	{
		get => selectedReference;
		set
		{
			if (selectedReference != null)
			{
				selectedReference.Selected = false;
				levelManager.UpdateReferenceInEditor(selectedReference, selectedReference.RootEditorId);
			}
			selectedReference = value;
			_inGroup = groupSelection.Find((s) => s == SelectedReference) != null;
			if (selectedReference != null)
			{
				selectedReference.Selected = true;
				levelManager.UpdateReferenceInEditor(selectedReference, selectedReference.RootEditorId);
			}
			e.UpdateInspector();
		}
	}
	private bool _inGroup = false;
	private bool lmbDragging = false;
	private bool rmbDragging = false;
	private Vector2 currentMpos = Vector2.Zero;
	private Vector2 mousePoint0;
	private Vector2 mousePoint1;
	private List<EditorReference> groupSelection = new();
	private int nextId = 0;
	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mb)
		{
			if (mb.ButtonIndex == MouseButton.Left && mb.Pressed)
			{
				if (e.levelData.References == null) return;
				switch (e.CurrentMode)
				{
					case Toolbar.Mode.Place: HandlePlacePress(mb); break;
					case Toolbar.Mode.Select: HandleSelectLMBPress(mb); break;
					case Toolbar.Mode.Delete: HandleDeleteLMBPress(mb); break;
				}
			}
			else if (mb.ButtonIndex == MouseButton.Left && !mb.Pressed)
			{
				switch (e.CurrentMode)
				{
					case Toolbar.Mode.Place: HandlePlaceRelease(mb); break;
					case Toolbar.Mode.Select: HandleSelectLMBRelease(mb); break;
					case Toolbar.Mode.Delete: HandleDeleteLMBRelease(); break;
				}
			}
			else if (mb.ButtonIndex == MouseButton.Right && mb.Pressed)
			{
				switch (e.CurrentMode)
				{
					case Toolbar.Mode.Select: HandleSelectRMBPress(mb); break;
				}
			}
			else if (mb.ButtonIndex == MouseButton.Right && !mb.Pressed)
			{
				switch (e.CurrentMode)
				{
					case Toolbar.Mode.Select: HandleSelectRMBRelease(mb); break;
				}
			}
		}
		else if (@event is InputEventMouseMotion mm)
		{
			currentMpos = mm.Position;
			switch (e.CurrentMode)
			{
				case Toolbar.Mode.Place: HandlePlaceMM(mm); break;
				case Toolbar.Mode.Select: HandleSelectMM(mm); break;
				case Toolbar.Mode.Delete: HandleDeleteMM(mm); break;
			}
			PreviewRoot.QueueRedraw();
		}
	}
	public Vector2 ToPreviewLocal(Vector2 screenPos) =>
		screenPos / resScale;

	private List<EditorReference> copied = new();
	private Vector2 copiedAtPos;
	private float copiedAtTime;
	private void Cut()
	{
		Copy();
		QuickDelete();
	}
	private void Copy()
	{
		copied.Clear();
		copiedAtPos = ToPreviewLocal(currentMpos);
		copiedAtTime = e.CurrentTime;
		// references have a T variable telling when it spawns and a SpawnPos variable telling where to spawn.
		foreach (var r in groupSelection)
		{
			copied.Add(r);
		}
	}
	private void Paste()
	{
		if (copied == null || copied.Count == 0) return;
		Vector2 currentPos = ToPreviewLocal(currentMpos);
		Vector2 posOffset = currentPos - copiedAtPos;
		float timeOffset = e.CurrentTime - copiedAtTime;
		_inGroup = copied.Count > 1;
		groupSelection.Clear();
		foreach (var original in copied)
		{
			EditorReference clone = new(original);
			clone.T += timeOffset;
			clone.SpawnX += posOffset.X;
			clone.SpawnY += posOffset.Y;
			clone.RootEditorId = nextId++;
			e.AddReference(clone);
			levelManager.UpdateReferenceInEditor(clone, clone.RootEditorId);
			if (_inGroup)
			{
				groupSelection.Add(clone);
			}
			else
			{
				selectedReference = clone;
			}
		}
		if (groupSelection.Count >= 1)
			selectedReference = groupSelection[0];
		PreviewRoot.QueueRedraw();
		levelManager.Sync();
	}
	private void QuickDelete()
	{
		e.DeleteReference(SelectedReference);
		foreach (var r in groupSelection)
		{
			e.DeleteReference(r);
			levelManager.UpdateReferenceInEditor(null, r.RootEditorId);
		}
		levelManager.Sync();
	}
	private void HandlePlacePress(InputEventMouseButton mb)
	{
		if (e.SelectedModel == null)
			return;
		var local = ToPreviewLocal(mb.Position);
		var pos = e.Snap ? local.Snapped(ConfigHelper.Current.GridSnapCellSize) : local;
		var newRef = new EditorReference
		{
			Id = e.SelectedModel.Id,
			Type = e.SelectedModel is ProjectileModel ? ModelType.Projectile : ModelType.Pattern,
			T = e.CurrentTime,
			SpawnX = pos.X,
			SpawnY = pos.Y,
			SpawnF = 0,
			RootEditorId = nextId++
		};
		lmbDragging = true;
		SelectedReference = newRef;
		e.AddReference(newRef);
		groupSelection.Clear();
		levelManager.UpdateReferenceInEditor(SelectedReference, SelectedReference.RootEditorId);
		levelManager.Sync();
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
		var r = GetNearestReference(ToPreviewLocal(mb.Position));
		SelectedReference = r;
		if (!_inGroup)
		{
			mousePoint0 = Vector2.Zero;
			mousePoint1 = Vector2.Zero;
			groupSelection.Clear();
		}
		PreviewRoot.QueueRedraw();
		int modified = -1;
		if (SelectedReference != null)
			modified = selectedReference.RootEditorId;
		levelManager.UpdateReferenceInEditor(SelectedReference, modified);
		levelManager.Sync();
	}
	private void HandleDeleteLMBPress(InputEventMouseButton mb)
	{
		lmbDragging = true;
		SelectedReference = null;
		var r = GetNearestReference(ToPreviewLocal(mb.Position));
		if (r != null) // if we got a reference
		{
			if (_inGroup) // if the reference is in the selection group
			{
				for (int j = 0; j < groupSelection.Count; j++)
				{
					var sr = groupSelection[j];
					e.DeleteReference(sr);
					levelManager.UpdateReferenceInEditor(null, sr.RootEditorId);
				}
			}
			else
			{
				e.DeleteReference(r);
				levelManager.UpdateReferenceInEditor(null, r.RootEditorId);
			}

		}
		levelManager.Sync();
	}
	private void HandlePlaceRelease(InputEventMouseButton mb)
	{
		if (e.SelectedModel == null)
			return;
		var local = ToPreviewLocal(mb.Position);
		var rPos = new Vector2(SelectedReference.SpawnX, SelectedReference.SpawnY);
		float f = (rPos - local).Angle() + (Mathf.Pi / 2);
		if (e.Snap)
		{
			float step = Mathf.Pi / ConfigHelper.Current.AngleSnapDivision;
			f = Mathf.Round(f / step) * step;
		}
		SelectedReference.SpawnF = f;
		levelManager.UpdateReferenceInEditor(SelectedReference, SelectedReference.RootEditorId);
		lmbDragging = false;
		SelectedReference = null;
		levelManager.Sync();
		PreviewRoot.QueueRedraw();
	}
	private void HandleSelectRMBRelease(InputEventMouseButton mb)
	{
		rmbDragging = false;
		if (SelectedReference != null) return;
		if (_inGroup) // if we are grabbing something inside of the group
		{
			var local = ToPreviewLocal(mb.Position);
			var rPos = new Vector2(SelectedReference.SpawnX, SelectedReference.SpawnY);
			float f = (rPos - local).Angle() + (Mathf.Pi / 2);
			if (e.Snap)
			{
				float step = Mathf.Pi / ConfigHelper.Current.AngleSnapDivision;
				f = Mathf.Round(f / step) * step;
			}
			SelectedReference.SpawnF = f;
			levelManager.UpdateReferenceInEditor(SelectedReference, SelectedReference.RootEditorId);
			levelManager.Sync();
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
				alive = gctx.T >= 0 && gctx.T <= proj.Lifetime;
			}
			else if (r.Type == ModelType.Pattern)
			{
				var patt = e.PatternModels[r.Id];
				var lt = e.CurrentTime - r.T;
				alive = lt >= 0 && lt <= patt.lifetime;
			}
			if (!alive)
				continue;
			var pos = r.Type == ModelType.Pattern ? new(r.SpawnX, r.SpawnY) : new Vector2(r.SpawnX, r.SpawnY);
			if (pos.X >= min.X && pos.X <= max.X &&
				pos.Y >= min.Y && pos.Y <= max.Y)
			{
				groupSelection.Add(r);
				r.Selected = true;
				levelManager.UpdateReferenceInEditor(r,r.RootEditorId);
			}
		}
		if (groupSelection.Count >= 1)
			selectedReference = groupSelection[0];
		PreviewRoot.QueueRedraw();
		levelManager.Sync();
	}
	private void HandleSelectLMBRelease(InputEventMouseButton mb)
	{
		lmbDragging = false;
		if (SelectedReference != null && !rmbDragging) // have a selected reference and holding lmb but not holding rmb
		{
			var local = ToPreviewLocal(mb.Position);
			var pos = e.Snap ? local.Snapped(ConfigHelper.Current.GridSnapCellSize) : local;
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
			levelManager.UpdateReferenceInEditor(SelectedReference, SelectedReference.RootEditorId);
			levelManager.Sync();
		}
	}
	private void HandleDeleteLMBRelease()
	{
		lmbDragging = false;
	}
	private void HandlePlaceMM(InputEventMouseMotion mm)
	{
		if (SelectedReference != null && lmbDragging)
		{
			var local = ToPreviewLocal(mm.Position);
			var rPos = new Vector2(SelectedReference.SpawnX, SelectedReference.SpawnY);
			float f = (rPos - local).Angle() + (Mathf.Pi / 2);
			if (e.Snap)
			{
				float step = Mathf.Pi / ConfigHelper.Current.AngleSnapDivision;
				f = Mathf.Round(f / step) * step;
			}
			SelectedReference.SpawnF = f;

			levelManager.UpdateReferenceInEditor(SelectedReference, SelectedReference.RootEditorId);
			levelManager.Sync();
		}
	}
	private void HandleSelectMM(InputEventMouseMotion mm)
	{
		if (SelectedReference != null && lmbDragging && !rmbDragging) // have a selected reference and holding lmb but not holding rmb
		{
			var local = ToPreviewLocal(mm.Position);
			var pos = e.Snap ? local.Snapped(ConfigHelper.Current.GridSnapCellSize) : local;
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
					levelManager.UpdateReferenceInEditor(r, r.RootEditorId);
				}
			}
			levelManager.UpdateReferenceInEditor(SelectedReference, SelectedReference.RootEditorId);
			levelManager.Sync();
		}
		else if (SelectedReference != null && rmbDragging) // have a selected reference and holding rmb
		{
			var local = ToPreviewLocal(mm.Position);
			var selectedPos = new Vector2(SelectedReference.SpawnX, SelectedReference.SpawnY);
			float f = (selectedPos - local).Angle() + (Mathf.Pi / 2);
			if (e.Snap)
			{
				float step = Mathf.Pi / ConfigHelper.Current.AngleSnapDivision;
				f = Mathf.Round(f / step) * step;
			}
			SelectedReference.SpawnF = f;
			if (_inGroup) // and in group
			{
				for (int i = 0; i < groupSelection.Count; i++)
				{
					var r = groupSelection[i];
					if (r == SelectedReference) continue;
					if (lmbDragging) // and holding mb, then point every selected projectile at cursor
					{
						var rPos = new Vector2(r.SpawnX, r.SpawnY);
						float rf = (rPos - local).Angle() + (Mathf.Pi / 2);
						if (e.Snap)
						{
							float step = Mathf.Pi / ConfigHelper.Current.AngleSnapDivision;
							rf = Mathf.Round(rf / step) * step;
						}
						r.SpawnF = rf;
						levelManager.UpdateReferenceInEditor(r, r.RootEditorId);
					}
					else // otherwise make the follow whatever is being set
					{
						r.SpawnF = f;
						levelManager.UpdateReferenceInEditor(r, r.RootEditorId);
					}
				}
			}
			levelManager.UpdateReferenceInEditor(SelectedReference, SelectedReference.RootEditorId);
			levelManager.Sync();
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
			var r = GetNearestReference(ToPreviewLocal(mm.Position));
			lmbDragging = true;
			SelectedReference = null;
			if (r != null)
			{
				e.DeleteReference(r);
				levelManager.UpdateReferenceInEditor(null, r.RootEditorId);
				levelManager.Sync();
			}
		}
	}
	public EditorReference GetNearestReference(Vector2 local)
	{
		float nearDist = float.MaxValue;
		EditorReference nearRef = null;
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
				float dist = new Vector2(r.SpawnX, r.SpawnY).DistanceTo(local);
				if (dist < nearDist)
				{
					nearDist = dist;
					nearRef = r;
				}
			}
		}
		if (nearRef != null && nearDist < 20)
			return nearRef;
		return null;
	}
	public EditorReference[] GetNearestReferences(Vector2 local, int count)
	{
		if (e.levelData == null) return [];
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
				dist = new Vector2(r.SpawnX, r.SpawnY).DistanceTo(local);
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
		if (e.Snap)
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
		var nearest = GetNearestReferences(ToPreviewLocal(currentMpos), 5);
		foreach (var nr in nearest)
		{
			var name = nr.Type == ModelType.Pattern ? e.PatternModels[nr.Id].Name : e.ProjectileModels[nr.Id].Name;
			PreviewRoot.DrawCircle(new Vector2(nr.SpawnX, nr.SpawnY), 4, RenderingUtils.ColorFromString(name), false);
		}
		if (r == null)
			return;
		// label for selected
		var labelPos = new Vector2(r.SpawnX, r.SpawnY) + new Vector2(15, -15);
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
				lctx.T = Math.Min(proj.Lifetime, ConfigHelper.Current.MaxPathLength) / steps * i;
				lctx.L = proj.Lifetime;
				double f = MathSafe.Sanitize(proj.fnf(lctx)) + r.SpawnF;
				var (x, y) = LevelDirector.CalculatePosition(proj.fnx, proj.fny, f, lctx);
				points[i] = new(r.SpawnX + x, r.SpawnY + y);
			}
			PreviewRoot.DrawPolyline(points, RenderingUtils.ColorFromString(proj.Name), ConfigHelper.Current.PathThickness, true);
			PreviewRoot.DrawCircle(points[0], ConfigHelper.Current.PathThickness * 1.5f, RenderingUtils.ColorFromString(proj.Name));
			var spawnPos = new Vector2(r.SpawnX, r.SpawnY);
			PreviewRoot.DrawDashedLine(spawnPos, spawnPos + (Vector2.FromAngle((float)r.SpawnF) * 50), Colors.DarkRed, 4f);
		}
		// path for patterns
		else if (r.Type == ModelType.Pattern)
		{
			var patt = e.PatternModels[r.Id];
			if (patt == null)
				return;
			Vector2[] points = new Vector2[steps];
			var lctx = new EvalContext() { N = patt.Count };
			var color = RenderingUtils.ColorFromString(patt.Name);
			for (int i = 0; i < steps; i++)
			{
				lctx.I = lctx.N / steps * i;
				var (x, y) = LevelDirector.CalculatePosition(patt.fnx, patt.fny, r.SpawnF, lctx);
				points[i] = new(r.SpawnX + x, r.SpawnY + y);
				PreviewRoot.DrawCircle(points[i], ConfigHelper.Current.PathThickness * 1.5f, color);
			}
			PreviewRoot.DrawPolyline(points, color, ConfigHelper.Current.PathThickness, true);
			var spawnPos = new Vector2(r.SpawnX, r.SpawnY);
			PreviewRoot.DrawDashedLine(spawnPos, spawnPos + (Vector2.FromAngle((float)r.SpawnF + (Mathf.Pi / 2)) * 50), Colors.DarkRed, 4f);
		}
	}
	public void Fit(Vector2I res)
	{
		var win = GetTree().Root.GetVisibleRect().Size;
		resScale = Mathf.Min(win.X / res.X, win.Y / res.Y) * PreviewScale;
		PreviewVP.Size = (Vector2I)((Vector2)res * resScale);
		PreviewRoot.Scale = Vector2.One * resScale;
	}
	private void OnWindowResized() => Fit(PlayingField.Resolutions[e.levelData.AspectRatio]);
	// only call on load
	public void Load(LevelData level)
	{
		nextId = 0;
		levelManager.ClearTimeline();
		Console.Inst.Log("[LevelPreview] Attempting to compile");
		for (int i = 0; i < Editor.MaxModelCount; i++)
		{
			var m = level.ProjectileModels[i];
			CompileProjectile(m);
		}
		for (int i = 0; i < Editor.MaxModelCount; i++)
		{
			var m = level.PatternModels[i];
			CompilePattern(m);
		}
		for (int i = 0; i < level.References.Count; i++)
		{
			var r = level.References[i];
			r.RootEditorId = nextId++;
			levelManager.UpdateReferenceInEditor(r, r.RootEditorId);
		}
		Console.Inst.Log("[LevelPreview] Completed compile all");
	}
	public void CompileProjectile(ProjectileModel model)
	{
		if (model == null)
			return;
		// create render group
		// compile and redraw
		LevelCompiler.CompileProjectile(model);
		PreviewRoot.QueueRedraw();
	}
	public void CompilePattern(PatternModel model)
	{
		if (model == null)
			return;
		// create render group
		var mesh = RenderingUtils.BuildCircleMesh(10);
		// compile
		LevelCompiler.CompilePattern(model);
		var lctx = new EvalContext { I = 0, N = model.Count > 1 ? model.Count - 1 : 1 };
		double maxSpawnT = 0;
		for (int j = 0; j < model.Count; j++)
		{
			lctx.I = j;
			double t = model.fnt(lctx);
			if (t > maxSpawnT)
				maxSpawnT = t;
		}
		var proj = e.ProjectileModels[model.SpawningId];
		model.lifetime = (float)(maxSpawnT + (proj != null ? proj.Lifetime : 0));
		// redraw
		PreviewRoot.QueueRedraw();
	}
	public void UpdateModel(IEditorModel model, bool delete = false)
	{
		for (int i = 0; i < e.levelData.References.Count; i++)
		{
			var r = e.levelData.References[i];
			if (r.Id == model.Id)
			{
				if (delete)
					levelManager.UpdateReferenceInEditor(null, r.RootEditorId);
				else
					levelManager.UpdateReferenceInEditor(r, r.RootEditorId);
			}
		}
		levelManager.Sync();
	}
	public void Sync() => levelManager.Sync();
	public void UpdateReferenceInEditor(EditorReference r, int modifiedId) => levelManager.UpdateReferenceInEditor(r, modifiedId);
}