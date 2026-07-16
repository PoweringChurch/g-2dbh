using System;
using Godot;

public partial class PatternPreview : Control
{
	private struct ProjectileInstance
	{
		public Vector2 Pos;
		public float F;
		public double T;
		public bool Alive;
	}
	public PatternModel Model;
	[Export] private Control DrawOn;
	[Export] private Button PlaybackToggle;
	[Export] private Button HomeButton;
	[Export] private SpinBox TimeSpin;
	private bool dirty = false;
	private SpatialReference reference;
	private ProjectileInstance[] instances = [];
	private Vector2[] pathPoints = [];
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
		if (Editor.Instance.ProjectileModels[Model.ProjectileId] == null)
			return;
		var pm = Editor.Instance.ProjectileModels[Model.ProjectileId];
		int count = Math.Max(Model.Count, 1);
		var lctx = new EvalContext { N = count > 1 ? count - 1 : 1 };
		if (instances.Length != count)
			instances = new ProjectileInstance[count];
		for (int i = 0; i < count; i++)
		{
			lctx.I = i;
			double genFwd = Model.fnf(lctx);
			double genT = Model.fnt(lctx);
			var start = LevelDirector.CalculatePosition(Model.fnx, Model.fny, (float)reference.SpawnF, lctx);
			var f = genFwd + reference.SpawnF;
			lctx.T = Math.Clamp(time-genT,0,pm.Lifetime);
			lctx.L = pm.Lifetime;
			var movement = LevelDirector.CalculatePosition(pm.fnx, pm.fny, f, lctx);
			bool alive = lctx.T > 0 && lctx.T < pm.Lifetime;
			instances[i] = new ProjectileInstance
			{
				Pos = reference.SpawnPos + start + movement,
				F = (float)(reference.SpawnF+f),
				Alive = alive,
				T = lctx.T
			};
		}
		BuildPath(lctx);
		DrawOn.QueueRedraw();
	}

	private void BuildPath(EvalContext lctx)
	{
		int steps = ConfigHelper.Current.PathFidelity;
		if (pathPoints.Length != steps)
			pathPoints = new Vector2[steps];
		for (int j = 0; j < steps; j++)
		{
			lctx.I = Math.Min(lctx.N, ConfigHelper.Current.MaxPathLength) / steps * j;
			var (x, y) = LevelDirector.CalculatePosition(Model.fnx, Model.fny, 0, lctx);
			pathPoints[j] = WorldToScreen(reference.SpawnPos + new Vector2(x, y));
		}
	}
	private void DrawPath()
	{
		if (Model == null || pathPoints.Length == 0)
			return;
		DrawOn.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
		DrawOn.DrawPolyline(pathPoints, Colors.Green, ConfigHelper.Current.PathThickness, true);
		DrawOn.DrawCircle(pathPoints[0], ConfigHelper.Current.PathThickness * 1.5f, Colors.Green);
	}

	private void DrawProjectileShapes()
	{
		if (Model == null)
			return;
		if (Editor.Instance.ProjectileModels[Model.ProjectileId] == null)
			return;
		var pm = Editor.Instance.ProjectileModels[Model.ProjectileId];
		var texture = pm.Texture != "default" ? RenderingUtils.LoadTexture(pm.Texture) : null;
		foreach (var inst in instances)
		{
			var color = Colors.White;
			if (!inst.Alive) color.A *= 0.5f;
			var forward = pm.LockRotation ? 0 : inst.F;
			DrawOn.DrawSetTransform(WorldToScreen(inst.Pos), forward, Vector2.One * pm.RenderScale * zoom);
			if (texture != null)
			{
				DrawOn.DrawTexture(texture, -texture.GetSize() / 2, color);
				continue;
			}
			if (pm.UseShape && pm.Shape != null)
				DrawOn.DrawColoredPolygon([.. pm.ShapeVect2s, pm.ShapeVect2s[0]], color);
			else
				DrawOn.DrawCircle(Vector2.Zero, pm.Radius, color);
		}
	}

	private void DrawHitboxes()
	{
		if (Model == null)
			return;
		var pm = Editor.Instance.ProjectileModels[Model.ProjectileId];
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