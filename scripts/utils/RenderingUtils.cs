using System.Collections.Generic;
using System.Linq;
using Godot;
public static class ValidExtensions
{
    public static string[] Image = [".png", ".jpg"];
}
public static class RenderingUtils
{
    private static readonly Dictionary<string, Texture2D> _textureCache = new();
    public static Texture2D LoadTexture(string inDir, string textureName)
    {
        if (_textureCache.TryGetValue(inDir+textureName, out var cached))
            return cached;
        foreach (string ext in new[] { "" }.Concat(ValidExtensions.Image))
        {
            string path = $"{inDir}{textureName}{ext}";
            if (!FileAccess.FileExists(path))
                continue;
            var image = new Image();
            Error err = image.Load(path);
            if (err != Error.Ok)
                continue;
            var texture = ImageTexture.CreateFromImage(image);
            _textureCache[inDir+textureName] = texture;
            return texture;
        }
        return null;
    }
    public static void EmptyTextureCache() =>
        _textureCache.Clear();
    private static readonly Dictionary<string, Color> _colorCache = new();
    public static Color ColorFromString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return new Color(1, 1, 1);
        if (_colorCache.TryGetValue(input, out var cached))
            return cached;
        int hash = input.GetHashCode();
        float hue = (hash & 0xFFFF) / 65535f;
        float sat = Mathf.Clamp(((hash >> 16) & 0xFF) / 255f, 0.4f, 1.0f);
        Color color = Color.FromHsv(hue, sat, 1);
        _colorCache[input] = color;
        return color;
    }
    public static ArrayMesh BuildCircleMesh(float radius, int segments = 20)
    {
        var verts = new Vector3[segments + 2];
        verts[0] = Vector3.Zero;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.Tau / segments;
            verts[i + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
        }

        var indices = new int[segments * 3];
        for (int i = 0; i < segments; i++)
        {
            indices[i * 3 + 0] = 0;
            indices[i * 3 + 1] = i + 1;
            indices[i * 3 + 2] = (i + 1) % segments + 1;
        }

        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = verts;
        arrays[(int)Mesh.ArrayType.Index] = indices;

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        return mesh;
    }

    public static ArrayMesh BuildPolygonMesh(Vector2[] points)
    {
        int[] indices = Geometry2D.TriangulatePolygon(points);
        if (indices == null || indices.Length == 0)
        {
            GD.PrintErr("[LevelCompiler] Polygon failed to triangulate (self-intersecting or degenerate shape) — skipping render mesh.");
            return null;
        }

        var verts = new Vector3[points.Length];
        for (int i = 0; i < points.Length; i++)
            verts[i] = new Vector3(points[i].X, points[i].Y, 0);

        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = verts;
        arrays[(int)Mesh.ArrayType.Index] = indices;

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        return mesh;
    }
}