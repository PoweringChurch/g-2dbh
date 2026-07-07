using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

public partial class ConfigHelper : Node
{
    private const string _configPath = "user://data/config.json";
    public static Config Current;
    public override void _Ready()
    {
        // get config
        if (!FileAccess.FileExists(_configPath))
        {
            DirAccess.MakeDirRecursiveAbsolute("user://data");
            DirAccess.MakeDirAbsolute("user://data/levels");
            DirAccess.MakeDirAbsolute("user://data/characters");
            DirAccess.MakeDirAbsolute("user://data/sounds");
            DirAccess.MakeDirAbsolute("user://data/logs");
            WriteJson(_configPath, new Config());
        }
        Current = ReadJson<Config>(_configPath);
    }
    public static void Save()
    {
        WriteJson(_configPath, Current);
    }
    private static T ReadJson<T>(string path)
    {
        if (!FileAccess.FileExists(path)) return default;
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null) return default;
        try { return JsonSerializer.Deserialize<T>(file.GetAsText()); }
        catch { return default; }
    }
    private static void WriteJson<T>(string path, T data)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            Console.Inst.LogErr($"[Config Helper] Failed to open file for writing: {path}");
        }
        var s = JsonSerializer.Serialize(data);
        file.StoreString(s);
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class ConfigFieldAttribute : Attribute
{
    public string Category { get; }
    public string Label { get; }
    public double Min { get; }
    public double Max { get; }
    public double Step { get; }

    public ConfigFieldAttribute(string category, string label, double min = 0, double max = 1000, double step = 1)
    {
        Category = category;
        Label = label;
        Min = min;
        Max = max;
        Step = step;
    }
}
public class Config
{
    // GAME
    // display
    [ConfigField("Game", "Fullscreen")]
    [JsonPropertyName("fullscreen")] public bool Fullscreen { get; set; } = false;
    // personalization
    [ConfigField("Game", "Character")]
    [JsonPropertyName("character")] public string Character { get; set; } = "default";
    [ConfigField("Game", "Character Scale", 0, 1, 0.05f)]
    [JsonPropertyName("characterScale")] public float CharacterScale { get; set; } = 0.3f;
    // sound
    [ConfigField("Game", "Music volume", 0, 1, 0.05f)]
    [JsonPropertyName("musicVolume")] public float MusicVolume { get; set; } = 1;
    [ConfigField("Game", "Graze volume", 0, 1, 0.05f)]
    [JsonPropertyName("grazeVolume")] public float GrazeVolume { get; set; } = 0.1f;
    [ConfigField("Game", "Hurt volume", 0, 1, 0.05f)]
    [JsonPropertyName("hurtVolume")] public float HurtVolume { get; set; } = 0.1f;
    // EDITOR
    // projectile preview
    [ConfigField("Editor", "Display projectile collision in projectile creator")]
    [JsonPropertyName("showCollision")] public bool ShowCollision { get; set; } = true;
    // path
    [ConfigField("Editor", "Path fidelity", 1, 256, 1)]
    [JsonPropertyName("pathFidelity")] public int PathFidelity { get; set; } = 64;

    [ConfigField("Editor", "Max path length", 1, 2048, 0.5)]
    [JsonPropertyName("MaxPathLength")] public float MaxPathLength { get; set; } = 128;
    [ConfigField("Editor", "Path thickness (px)", 0.5, 10, 0.05)]
    [JsonPropertyName("pathThickness")] public float PathThickness { get; set; } = 0.5f;
    // placement
    [ConfigField("Editor", "Angle snap divisions", 4, 16, 1)]
    [JsonPropertyName("angleSnapDivisions")] public float AngleSnapDivision { get; set; } = 8;
    [ConfigField("Editor", "Grid cell size", 4, 256, 1)]
    [JsonPropertyName("gridCellSize")] public float GridSnapCellSize { get; set; } = 8;
    // TESTING
    [ConfigField("Testing & Debugging", "No hit")]
    [JsonPropertyName("noHit")] public bool NoHit { get; set; } = false;
    [ConfigField("Testing & Debugging", "No graze")]
    [JsonPropertyName("noGraze")] public bool NoGraze { get; set; } = false;
    [ConfigField("Testing & Debugging", "No graze tracking")]
    [JsonPropertyName("noGrazeTracking")] public bool NoGrazeTracking { get; set; } = false;
    [ConfigField("Testing & Debugging", "Slow movement")]
    [JsonPropertyName("slowMovement")] public bool SlowMovement { get; set; } = false;
}