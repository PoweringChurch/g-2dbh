using Godot.Collections;
using System.Linq;
using Godot;
using System;
public static class ValidExtensions
{
    public static string[] Image = [".png", ".jpg"];
    public static string[] Audio = [".wav", ".mp3", ".ogg"];
}
public static class RenderingUtils
{
    private static readonly string[] first = [""];
    private static readonly Dictionary<string, Texture2D> textureCache = new();
    public static Texture2D LoadTexture(string path)
    {
        if (textureCache.TryGetValue(path, out var cached))
            return cached;
        foreach (string ext in first.Concat(ValidExtensions.Image))
        {
            string testPath = $"{path}{ext}";
            Texture2D texture = null;
            if (ResourceLoader.Exists(testPath))
            {
                texture = ResourceLoader.Load<Texture2D>(testPath);
            }
            else if (FileAccess.FileExists(testPath))
            {
                var image = new Image();
                if (image.Load(testPath) == Error.Ok)
                    texture = ImageTexture.CreateFromImage(image);
            }
            if (texture == null)
                continue;
            textureCache[path] = texture;
            return texture;
        }
        return null;
    }
    private static Dictionary<string, Rect2> rects;
    public static Dictionary<string, Rect2> Rects 
    {
        get
        {
            rects ??= GD.Load<AtlasRectTable>(projectileRectsPath).Rects;
            return rects;
        }
    }
    private static Texture2D[] sourceTextures;
    private const string projectilesFolderPath = "res://data/images/projectiles/";
    private const string projectileRectsPath = "res://data/images/atlases/projectile_rects.tres";
    private const string projectileAtlasPath = "res://data/images/atlases/projectile_atlas.png";
    public const int AtlasSize = 512;
    private const int padding = 4;
    private static string[] projectileNames;
    public static string[] GetProjectileNames()
    {
        if (projectileNames != null)
            return projectileNames;
        string[] names = [..Rects.Keys];
        projectileNames = names;
        return projectileNames;
    }
    private static string[] GetProjectileNamesFromSource()
    {
        if (!OS.IsDebugBuild())
        {
            Console.LogErr("[RenderingUtils] Cannot get projectile names from source in a release build");
            return null;
        }
        System.Collections.Generic.List<string> names = [];
        var dir = DirAccess.Open(projectilesFolderPath);
        dir.ListDirBegin();
        string entry = dir.GetNext();
        while (entry != "")
        {
            if (entry.EndsWith(".png"))
                names.Add(entry);
            entry = dir.GetNext();
        }
        dir.ListDirEnd();
        names.Sort(StringComparer.Ordinal);
        projectileNames = [..names];
        return projectileNames;
    }
    public static void BuildProjectileAtlas()
    {
        if (!OS.IsDebugBuild())
        {
            Console.LogErr("[RenderingUtils] Cannot build atlas from a release build");
            return;
        }
        var names = GetProjectileNamesFromSource();
        sourceTextures = new Texture2D[projectileNames.Length];
        for (int i = 0; i < projectileNames.Length; i++)
            sourceTextures[i] = ResourceLoader.Load<Texture2D>($"{projectilesFolderPath}{projectileNames[i]}"); // this line causes the error
        Console.LogDebug($"Built {projectileNames.Length} textures");

        var atlas = Image.CreateEmpty(AtlasSize, AtlasSize, false, Image.Format.Rgba8);
        var rects = new Dictionary<string, Rect2>(); 

        int x = 0, y = 0, rowHeight = 0;
        for (int i = 0; i < sourceTextures.Length; i++)
        {
            var tex = sourceTextures[i];
            var img = tex.GetImage();

            if (img.IsCompressed())
                img.Decompress();
            if (img.GetFormat() != atlas.GetFormat())
                img.Convert(atlas.GetFormat());
            
            if (x + img.GetWidth() > AtlasSize) { x = 0; y += rowHeight + padding; rowHeight = 0; }
            var imgSize = img.GetSize();
            atlas.BlitRect(img, new Rect2I(Vector2I.Zero, imgSize), new Vector2I(x, y));
            rects[names[i]] = new Rect2(
                (float)x / AtlasSize, (float)y / AtlasSize,
                (float)imgSize.X / AtlasSize, (float)imgSize.Y / AtlasSize
            );
            x += imgSize.X + padding;
            rowHeight = Mathf.Max(rowHeight, imgSize.Y);
        }
        var table = new AtlasRectTable { Rects = rects };
        ResourceSaver.Save(table, projectileRectsPath);
        atlas.SavePng(projectileAtlasPath);
        Console.LogDebug($"Saved projectile atlas to {projectileAtlasPath} textures");
    }
    public static Texture2D GetProjectileAtlas()
    {
        return LoadTexture(projectileAtlasPath);
    }
    private static Dictionary<string, AtlasTexture> projectileTextureCache = new();
    public static AtlasTexture GetProjectileTexture(string projectileId)
    {
        if (projectileTextureCache.TryGetValue(projectileId, out var tex))
            return tex;
        if (!rects.TryGetValue(projectileId, out Rect2 regionRect))
        {
            Console.LogErr($"[RenderingUtils] Projectile id {projectileId} not found in rect table");
            return null;
        }
        var pxRegionSize = regionRect.Size*AtlasSize;
        var pxRegionPos = regionRect.Position*AtlasSize;
        Rect2 pxRegion = new(pxRegionPos, pxRegionSize);
        var atlasTexture = new AtlasTexture
        {
            Atlas = GetProjectileAtlas(),
            Region = pxRegion
        };
        projectileTextureCache[projectileId] = atlasTexture;
        return atlasTexture;
    }
    private static readonly Dictionary<string, Color> colorcache = new();
    public static Color ColorFromString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return new Color(1, 1, 1);
        if (colorcache.TryGetValue(input, out var cached))
            return cached;
        uint hash = Fnv1aHash(input);
        float hue = (hash & 0xFFFF) / 65535f;
        float sat = Mathf.Clamp(((hash >> 16) & 0xFF) / 255f, 0.4f, 1.0f);
        Color color = Color.FromHsv(hue, sat, 1);
        colorcache[input] = color;
        return color;
    }
    public static Color GetContrastingColor(Color c)
    {
        float luminance = 0.2126f * c.R + 0.7152f * c.G + 0.0722f * c.B;
        return luminance > 0.5f ? new Color(0, 0, 0) : new Color(1, 1, 1);
    }
    private static uint Fnv1aHash(string input)
    {
        const uint fnvPrime = 16777619;
        const uint fnvOffsetBasis = 2166136261;

        uint hash = fnvOffsetBasis;
        foreach (char c in input)
        {
            hash ^= c;
            hash *= fnvPrime;
        }
        return hash;
    }
    public static ArrayMesh BuildCircleMesh(float radius, bool outline = false, int segments = 20)
    {   
        var verts = new Vector3[segments + 2];
        verts[0] = Vector3.Zero;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.Tau / segments;
            verts[i + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
        }
        int[] indices;
        if (outline)
        {
            indices = new int[segments * 2];
            for (int i = 0; i < segments; i++)
            {
                indices[i * 2 + 0] = i + 1;
                indices[i * 2 + 1] = i + 2; // Connects to the next vertex
            }
        }
        else
        {
            indices = new int[segments * 3];
            for (int i = 0; i < segments; i++)
            {
                indices[i * 3 + 0] = 0;
                indices[i * 3 + 1] = i + 1;
                indices[i * 3 + 2] = i + 2; 
            }
        }
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = verts;
        arrays[(int)Mesh.ArrayType.Index] = indices;

        var mesh = new ArrayMesh();
        Mesh.PrimitiveType primitiveType = outline ? Mesh.PrimitiveType.Lines : Mesh.PrimitiveType.Triangles;
        mesh.AddSurfaceFromArrays(primitiveType, arrays);
        return mesh;
    }
    public static ArrayMesh BuildPolygonMesh(Vector2[] points, bool outline = false)
    {
        if (points == null || points.Length < 3) 
        {
            Console.LogErr("[LevelCompiler] Polygon has insufficient points, skipping render mesh");
            return null;
        }
        var verts = new Vector3[points.Length];
        for (int i = 0; i < points.Length; i++)
            verts[i] = new Vector3(points[i].X, -points[i].Y, 0);

        int[] indices;
        Mesh.PrimitiveType primitiveType;
        if (outline)
        {
            primitiveType = Mesh.PrimitiveType.Lines;
            indices = new int[points.Length * 2];
            for (int i = 0; i < points.Length; i++)
            {
                indices[i * 2 + 0] = i;
                indices[i * 2 + 1] = (i + 1) % points.Length;
            }
        }
        else
        {
            primitiveType = Mesh.PrimitiveType.Triangles;
            indices = Geometry2D.TriangulatePolygon(points);
            if (indices == null || indices.Length == 0)
            {
                Console.LogErr("[LevelCompiler] Polygon failed to triangulate (self-intersecting or degenerate shape), skipping render mesh");
                return null;
            }
        }
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = verts;
        arrays[(int)Mesh.ArrayType.Index] = indices;

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(primitiveType, arrays);
        return mesh;
    }
}