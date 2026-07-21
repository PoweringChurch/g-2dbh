using System;
using Godot;

public partial class PatternPreview : Control
{
	private struct ChildInstance
	{
		public Vector2 Pos;
		public float F;
		public double T;
		public bool Alive;
	}
	private ChildInstance[] instances = [];
	public PatternModel Model;
	[Export] private Control DrawOn;
	[Export] private Button PlaybackToggle;
	[Export] private Button HomeButton;
	[Export] private SpinBox TimeSpin;
	private bool dirty = false;
	private SpatialReference reference;
	private bool dragging = false;
	private bool panning = false;
	private bool playing = false;

	private float zoom = 1f;
	private Vector2 zoomPan = Vector2.Zero;
	private const float ZoomMin = 0.25f;
	private const float ZoomMax = 4f;
	private const float ZoomStep = 1.1f;

	private double time = 0;
	public override void _Ready()
	{
		time = 0;
		DrawOn.Draw += DrawPath;
		DrawOn.Draw += DrawProjectileShapes;
		DrawOn.Draw += DrawHitboxes;
		PlaybackToggle.Pressed += () => { playing = !playing; PlaybackToggle.Text = playing ? "❚❚" : "▶"; };
		TimeSpin.ValueChanged += (v) => { time = v; MarkDirty(); };
		HomeButton.Pressed += Home;
	}
	public void Load(PatternModel m)
	{
		Model = m;
		Home();
	}
	public void MarkDirty()
	{
		dirty = true;
	}
	private void Home()
	{
		reference.SpawnPos = new Vector2(918, 694) / 2;
		TimeSpin.Value = 0;
		zoom = 1f;
		zoomPan = Vector2.Zero;
		MarkDirty();
	}
	private Vector2 ViewCenter => DrawOn.Size / 2;
	private Vector2 WorldToScreen(Vector2 world) => ViewCenter + zoomPan + (world - ViewCenter) * zoom;
	private Vector2 ScreenToWorld(Vector2 screen) => ViewCenter + (screen - ViewCenter - zoomPan) / zoom;
	private void ApplyZoom(float newZoom, Vector2 screenPos)
	{
		newZoom = Mathf.Clamp(newZoom, ZoomMin, ZoomMax);
		if (Mathf.IsEqualApprox(newZoom, zoom))
			return;
		var worldUnderCursor = ScreenToWorld(screenPos);
		zoom = newZoom;
		zoomPan = screenPos - ViewCenter - (worldUnderCursor - ViewCenter) * zoom;
		MarkDirty();
	}
	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mb)
		{
			if (mb.ButtonIndex == MouseButton.Left)
			{
				if (mb.Pressed)
				{
					var dist = (mb.Position - WorldToScreen(reference.SpawnPos)).Length();
					dragging = dist < 20;
				}
				else
					dragging = false;
			}
			else if (mb.ButtonIndex == MouseButton.Middle)
			{
				panning = mb.Pressed;
			}
			else if (mb.ButtonIndex == MouseButton.WheelUp && mb.Pressed)
			{
				ApplyZoom(zoom * ZoomStep, mb.Position - DrawOn.Position);
			}
			else if (mb.ButtonIndex == MouseButton.WheelDown && mb.Pressed)
			{
				ApplyZoom(zoom / ZoomStep, mb.Position - DrawOn.Position);
			}
		}
		else if (@event is InputEventMouseMotion mm)
		{
			if (dragging)
			{
				reference.SpawnPos = ScreenToWorld(mm.Position - DrawOn.Position);
				MarkDirty();
			}
			else if (panning)
			{
				reference.SpawnPos += mm.Relative / zoom;
				MarkDirty();
			}
		}
	}
	public override void _Process(double dt)
	{
		if (playing)
		{
			TimeSpin.Value += dt;
		}
		if (dirty)
		{
			Sync();
			dirty = false;
		}
	}
	private void Sync()
	{
		if (Model == null) 
			return;
		int count = Math.Max(Model.Count, 1);
		var lctx = new EvalContext { N = count > 1 ? count - 1 : 1 };
		if (instances.Length != count)
			instances = new ChildInstance[count];
		bool isProj = Model.SpawningType == ModelType.Projectile;
		var proj = isProj ? Editor.Instance.ProjectileModels[Model.SpawningId] : null;
		var patt = !isProj ? Editor.Instance.PatternModels[Model.SpawningId] : null;

		double lifetime = isProj ? proj.Lifetime : patt.lifetime;
		float spawnF = (float)reference.SpawnF;

		for (int i = 0; i < count; i++)
		{
			lctx.I = i;
			double genFwd = Model.fnf(lctx);
			double genT = Model.fnt(lctx);
			float f = (float)(genFwd + spawnF);
			Vector2 startPos = reference.SpawnPos + LevelDirector.CalculatePosition(Model.fnx, Model.fny, spawnF, lctx);
			double localTime = time - genT;
			Vector2 movement = Vector2.Zero;

			if (isProj)
			{
				lctx.T = Math.Clamp(localTime, 0, lifetime);
				lctx.L = lifetime;
				movement = LevelDirector.CalculatePosition(proj.fnx, proj.fny, f, lctx);
			}
			else
			{
				lctx.T = localTime;
			}
			bool alive = localTime > 0 && localTime < lifetime;
			instances[i] = new ChildInstance
			{
				Pos = startPos + movement,
				F = f,
				Alive = alive,
				T = lctx.T
			};
		}
		DrawOn.QueueRedraw();
	}
	private void DrawPath()
	{
		if (Model == null)
			return;
		int steps = ConfigHelper.Current.PathFidelity;
		if (steps <= 0)
			return;
		Vector2[] pathPoints = new Vector2[steps];
		var lctx = new EvalContext() { N = Model.Count > 1 ? Model.Count - 1 : 1 };
		for (int j = 0; j < steps; j++)
		{
			lctx.I = Math.Min(lctx.N, ConfigHelper.Current.MaxPathLength) / steps * j;
			var (x, y) = LevelDirector.CalculatePosition(Model.fnx, Model.fny, 0, lctx);
			pathPoints[j] = WorldToScreen(reference.SpawnPos + new Vector2(x, y));
		}
		DrawOn.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
		DrawOn.DrawPolyline(pathPoints, Colors.Green, ConfigHelper.Current.PathThickness, true);
		DrawOn.DrawCircle(pathPoints[0], ConfigHelper.Current.PathThickness * 1.5f, Colors.Green);
	}

	private void DrawProjectileShapes()
	{
		if (Model == null)
			return;
		if (Editor.Instance.ProjectileModels[Model.SpawningId] == null)
			return;
		if (Model.SpawningType == ModelType.Projectile)
		{
			var proj = Editor.Instance.ProjectileModels[Model.SpawningId];
			var texture = RenderingUtils.LoadTexture(LevelCompiler.ProjectileTextures[proj.TextureId].TexturePath);
			foreach (var inst in instances)
			{
				var color = Colors.White;
				if (!inst.Alive) color.A *= 0.5f;
				var forward = proj.LockRotation ? 0 : inst.F;
				DrawOn.DrawSetTransform(WorldToScreen(inst.Pos), forward, Vector2.One * proj.RenderScale * zoom);
				if (texture != null)
				{
					DrawOn.DrawTexture(texture, -texture.GetSize() / 2, color);
					continue;
				}
				if (proj.UseShape && proj.Shape != null)
					DrawOn.DrawColoredPolygon([.. proj.ShapeVect2s, proj.ShapeVect2s[0]], color);
				else
					DrawOn.DrawCircle(Vector2.Zero, proj.Radius, color);
			}
		} else if (Model.SpawningType == ModelType.Pattern)
		{
			var patt = Editor.Instance.PatternModels[Model.SpawningId];
			foreach (var inst in instances)
			{
				var color = RenderingUtils.ColorFromString(patt.Name); color.A = 0.8f;
				if (!inst.Alive) color.A *= 0.5f;
				DrawOn.DrawSetTransform(WorldToScreen(inst.Pos), inst.F, Vector2.One * zoom);
				DrawOn.DrawCircle(Vector2.Zero, 10, color);
				DrawOn.DrawDashedLine(Vector2.Zero, Vector2.Up, color);
			}
		}
	}
	private void DrawHitboxes()
	{
		if (Model == null || Model.SpawningType == ModelType.Pattern)
			return;
		var pm = Editor.Instance.ProjectileModels[Model.SpawningId];
		if (!pm.CanCollide)
			return;
		foreach (var inst in instances)
		{
			bool show = inst.Alive && (inst.T > pm.TelegraphTime);
			if (!show) continue;

			var forward = pm.LockRotation ? 0 : inst.F;
			DrawOn.DrawSetTransform(WorldToScreen(inst.Pos), forward, Vector2.One * zoom);
			if (pm.UseShape && pm.Shape != null)
				DrawOn.DrawPolyline([.. pm.ShapeVect2s, pm.ShapeVect2s[0]], Colors.Red);
			else
				DrawOn.DrawCircle(Vector2.Zero, pm.Radius, Colors.Red, false);
		}
	}
}