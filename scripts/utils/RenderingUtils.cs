using System.Collections.Generic;
using System.Linq;
using Godot;

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
}