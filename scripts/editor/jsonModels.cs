using System;
using Godot;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public enum ModelType { Projectile, Pattern }
public class EditorReference
{
    public float SpawnX {get; set;}
    public float SpawnY {get; set;}
    public double T { get; set; }
    public double F { get; set; }
    public ModelType Type { get; set; }
    public int Id { get; set; }
    // editor only
    [JsonIgnore] public Vector2 Pos {get; set;}
    [JsonIgnore] public int Depth {get; set;}
    [JsonIgnore] public int RootEditorId = -1;
    public EditorReference() {}
    public EditorReference(EditorReference other)
    {
        SpawnX = other.SpawnX;
        SpawnY = other.SpawnY;
        T = other.T;
        F = other.F;
        Type = other.Type;
        Id = other.Id;
    }
}
public interface IEditorModel
{
    int Id {get; set;}
    string Name { get; set; }
}
public class LevelData
{
    [JsonPropertyName("displayName")] public string DisplayName { get; set; } = "New Level";
    [JsonPropertyName("author")] public string Author { get; set; } = "Unknown";
    [JsonPropertyName("music")] public string Music { get; set; } = "none";
    [JsonPropertyName("health")] public int Health { get; set; } = 3;
    [JsonPropertyName("aspectRatio")] public int AspectRatio { get; set; } = 0;
    [JsonPropertyName("duration")] public float Duration { get; set; } = 10;
    [JsonPropertyName("projectileModels")] public ProjectileModel[] ProjectileModels { get; set; } = new ProjectileModel[Editor.MaxModelCount];
    [JsonPropertyName("patternModels")] public PatternModel[] PatternModels { get; set; } = new PatternModel[Editor.MaxModelCount];
    [JsonPropertyName("bullets")] public List<EditorReference> References { get; set; } = [];
    [JsonPropertyName("variables")] public Dictionary<string, string> CustomVariables { get; set; } = [];
    [JsonPropertyName("id")] public string LevelId { get; set; } = Guid.NewGuid().ToString();
    [JsonPropertyName("backgroundLayers")] public List<BackgroundLayer> BackgroundLayers {get; set;} = [];
}