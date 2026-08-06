using System;
using System.Collections.Generic;
using System.Data.Common;
using Godot;

// handles both display and processing
public class EditorLevelManager
{
	private List<BackgroundLayerInstance> bgInstances = new();
	public List<BackgroundLayerInstance> BGInstances => bgInstances;
    private struct BakedEditorReference()
    {
        public int ProjectileId;
        public Vector2 SpawnPos;
        public Vector2 Pos;
        public double SpawnF;
        public double F;
        public double T;
        public ModelType Type;
        public int Depth;
        public int RootEditorId;
		public bool Selected;
    }
    private Editor e => Editor.Instance;
	private MultiMesh multiMesh;
	private readonly MultiMeshInstance2D mmInst;
    public EditorLevelManager(Node root)
    {
        multiMesh = new MultiMesh
        {
            Mesh = new QuadMesh(),
            TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
            UseColors = true,
            UseCustomData = true,
            InstanceCount = 0,
        };
		var shader = GD.Load<Shader>("res://shaders/projectile_atlas.gdshader");
        var shaderMaterial = new ShaderMaterial { Shader = shader };
        mmInst = new MultiMeshInstance2D
        {
            Multimesh = multiMesh,
            Texture = RenderingUtils.GetProjectileAtlas(),
            Material = shaderMaterial,
			YSortEnabled = true
        };
        root.AddChild(mmInst);
    }
    private readonly List<BakedEditorReference> bakedTimeline = new();
	private int written;
    public void ClearTimeline()
    {
        bakedTimeline.Clear();
    }
	public void Sync()
	{
		if (e.levelData == null)
			return;
		// tick backgrounds
		for (int i = 0; i < e.levelData.BackgroundLayers.Count; i++)
		{
			bgInstances[i].Tick(e.CurrentTime);
		}
		written = 0;
		// tick references
		for (int i = bakedTimeline.Count - 1; i >= 0; i--)
		{
			var r = bakedTimeline[i];
			bool alive = false;
			if (r.Type == ModelType.Projectile)
				alive = ProcessProjReference(ref r) && !IsOutOfBounds(r);
			else if (r.Type == ModelType.Pattern)
			{
				var patt = e.PatternModels[r.ProjectileId];
				var t = e.CurrentTime - r.T;
				alive = t >= 0 && t <= patt.lifetime;
			}
			if (alive)
				DrawReference(r);
		}
		multiMesh.InstanceCount = written;
		if (written > 0)
		{
			RenderingServer.MultimeshSetBuffer(multiMesh.GetRid(), buffer.AsSpan(0, written * floatsPerInstance).ToArray());
			mmInst.QueueRedraw();
		}
	}
	private float[] buffer = [];
	const int floatsPerInstance = 16; // 8 transform + 4 color + 4 uv
	private void DrawReference(BakedEditorReference r)
	{
		// draw references
		var proj = e.ProjectileModels[r.ProjectileId];
		double t = e.CurrentTime - r.T;
		// calc color
		Color filter = r.Selected ? new Color(0.7f, 0.7f, 1) : Colors.White;
		Color color = proj.Tint;
		if (r.Type == ModelType.Projectile)
		{
			float alpha = 1;
			if (proj.TelegraphTime != 0)
				alpha = (float)Math.Min(t / proj.TelegraphTime,1);
			color.A = alpha;
		} else if (r.Type == ModelType.Pattern)
		{ 
			color = RenderingUtils.ColorFromString(e.PatternModels[r.ProjectileId].Name); 
			color.A = 0.8f; 
		}
		// calc forward
		float drawForward = 0;
		if (r.Type == ModelType.Projectile && !proj.LockRotation)
			drawForward = (float)r.F;
		// calc scale
		Vector2 scale = Vector2.One;
		if (r.Type == ModelType.Projectile)
			scale = proj.RenderScale;
		// determine textureId
		string textureId = "pattern_marker.png";
		if (r.Type == ModelType.Projectile)
			textureId = proj.TextureName;
		// write
		WriteIntoBuffer(r.Pos, scale, drawForward, color*filter, textureId);
		written++;
	}
	private bool ProcessProjReference(ref BakedEditorReference r)
	{
		var proj = e.ProjectileModels[r.ProjectileId];
		if (proj == null) return false;
		var lctx = new EvalContext();
		lctx.T = e.CurrentTime - r.T;
		lctx.L = proj.Lifetime;
		lctx.Unique = LevelDirector.GetUnique(r.ProjectileId, (int)r.Type, r.SpawnPos, r.SpawnF, r.T);
		bool alive = lctx.T >= 0 && lctx.T <= proj.Lifetime;
		if (!alive) return false;
		var f = MathSafe.Sanitize(proj.fnf(lctx)) + r.SpawnF;
		var pos = LevelDirector.CalculatePosition(proj.fnx, proj.fny, f, lctx);
		r.Pos = r.SpawnPos + pos;
		r.F = f;
		return true;
	}
    private void WriteIntoBuffer(Vector2 pos, Vector2 scale, float forward, Color color, string textureId)
	{
		int o = written * floatsPerInstance;
		int required = o + floatsPerInstance;
		if (buffer.Length < required)
		{
			int newSize = Math.Max(required, Math.Max(buffer.Length * 2, floatsPerInstance * 16));
			Array.Resize(ref buffer, newSize);
		}
		float cos = Mathf.Cos(forward);
		float sin = Mathf.Sin(forward);

		var rect = RenderingUtils.Rects[textureId];
		var pixelSize = rect.Size*RenderingUtils.AtlasSize;
		buffer[o + 0] = scale.X * cos * pixelSize.X;
		buffer[o + 1] = scale.X * sin * pixelSize.X;
		buffer[o + 2] = 0;
		buffer[o + 3] = pos.X;

		buffer[o + 4] = scale.Y * sin * pixelSize.Y;
		buffer[o + 5] = -scale.Y * cos * pixelSize.Y;
		buffer[o + 6] = 0;
		buffer[o + 7] = pos.Y;
		
		buffer[o + 8] = color.R;
		buffer[o + 9] = color.G;
		buffer[o + 10] = color.B;
		buffer[o + 11] = color.A;

		buffer[o + 12] = rect.Position.X;
		buffer[o + 13] = rect.Position.Y;
		buffer[o + 14] = rect.Size.X;
		buffer[o + 15] = rect.Size.Y;
	}
	private bool IsOutOfBounds(BakedEditorReference r)
	{
		var proj = e.ProjectileModels[r.ProjectileId];
		if (proj.Persistant) return false;
		var resolution = PlayingField.Resolutions[e.levelData.AspectRatio];
		var bounds = RenderingUtils.Rects[proj.TextureName].Size*RenderingUtils.AtlasSize;
		bool isOutOfBounds = r.Pos.X < -bounds.X * proj.RenderScale.X
		|| r.Pos.X > resolution.X + bounds.X * proj.RenderScale.X
		|| r.Pos.Y < -bounds.Y * proj.RenderScale.Y
		|| r.Pos.Y > resolution.Y + bounds.Y * proj.RenderScale.Y;
		return isOutOfBounds;
	}
    public void UpdateReferenceInEditor(EditorReference r, int modifiedId)
	{
		bakedTimeline.RemoveAll(x => x.RootEditorId == modifiedId);
		if (r == null) return;
		var modifiedRef = new BakedEditorReference() 
        {
            SpawnPos = new Vector2(r.SpawnX, r.SpawnY),
            Pos = new Vector2(r.SpawnX, r.SpawnY),
            SpawnF = r.SpawnF,
            F = r.SpawnF,
            T = r.T,
            Type = r.Type,
            ProjectileId = r.Id,
            Depth = 0,
			Selected = r.Selected,
			RootEditorId = r.RootEditorId
        };

		Queue<BakedEditorReference> processingQueue = new();
		processingQueue.Enqueue(modifiedRef);
		while (processingQueue.Count > 0)
		{
			var cur = processingQueue.Dequeue();
			cur.RootEditorId = modifiedId;
			bakedTimeline.Add(cur);
			if (cur.Type == ModelType.Projectile)
			{
				UnpackProjectileIntoQueue(cur, processingQueue);
			}
			else if (cur.Type == ModelType.Pattern)
			{
				UnpackPatternIntoQueue(cur, processingQueue);
			}
		}
	}
	private void UnpackProjectileIntoQueue(BakedEditorReference r, Queue<BakedEditorReference> queue)
	{
		var proj = e.ProjectileModels[r.ProjectileId];
		if (r.Depth >= proj.MaxDepth) return;
		for (int i = 0; i < proj.Spawns.Count; i++)
		{
			var childRef = proj.Spawns[i];
			var pctx = new EvalContext { T = childRef.T, L = proj.Lifetime, Unique = LevelDirector.GetUnique(r.ProjectileId, (int)r.Type, r.SpawnPos, r.SpawnF, r.T) }; // parent context at time of child spawning
			double parentF = r.SpawnF+MathSafe.Sanitize(proj.fnf(pctx));
			var parentPos = LevelDirector.CalculatePosition(proj.fnx, proj.fny, parentF, pctx) + r.SpawnPos;
			Vector2 childSpawnPos = parentPos + new Vector2(childRef.SpawnX, childRef.SpawnY);
			double childT = MathSafe.Sanitize(childRef.T+r.T);
			double childF = parentF + childRef.SpawnF;
			var newRef = new BakedEditorReference()
			{
				SpawnPos = childSpawnPos,
				Pos = childSpawnPos,
				SpawnF = childF,
				F = childF,
				T = childT,
				Type = childRef.Type,
				ProjectileId = childRef.Id,
				Depth = r.Depth +1,
				RootEditorId = r.RootEditorId
			};
			queue.Enqueue(newRef);
		}
	}
	private void UnpackPatternIntoQueue(BakedEditorReference r, Queue<BakedEditorReference> queue)
	{
		var patt = e.PatternModels[r.ProjectileId];
		if (patt == null) return;
		var lctx = new EvalContext { N = patt.Count > 1 ? patt.Count - 1 : 1, Unique = LevelDirector.GetUnique(r.ProjectileId, (int)r.Type, r.SpawnPos, r.SpawnF, r.T)};
		Vector2 basePos = r.SpawnPos;
		r.Pos = basePos;
		for (int j = 0; j < patt.Count; j++)
		{
			lctx.I = j;
			double spawnDelay = MathSafe.Sanitize(patt.fnt(lctx));
			double fwdOffset = MathSafe.Sanitize(patt.fnf(lctx));
			Vector2 spawnOffset = LevelDirector.CalculatePosition(patt.fnx, patt.fny, r.SpawnF, lctx);
			Vector2 childAbsoluteSpawnPos = basePos + spawnOffset;
			var subBulletRef = new BakedEditorReference
			{
				ProjectileId = patt.SpawningId,
				Type = patt.SpawningType,
				RootEditorId = r.RootEditorId,
				T = r.T + spawnDelay,
				SpawnPos = childAbsoluteSpawnPos,
				Pos = childAbsoluteSpawnPos,
				SpawnF = r.SpawnF + fwdOffset,
				F = r.SpawnF + fwdOffset,
				Depth = r.Depth
			};
			queue.Enqueue(subBulletRef);
		}
	}
}