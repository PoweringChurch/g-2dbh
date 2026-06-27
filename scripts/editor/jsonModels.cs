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
    [JsonIgnore] public bool Alive {get; set;}
}
public interface IEditorModel
{
    int Id {get; set;}
    string Name { get; set; }
}
public class ProjectileModel : IEditorModel
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "unnamed";
    [JsonPropertyName("fnX")] public string FunctionX { get; set; } = "0";
    [JsonPropertyName("fnY")] public string FunctionY { get; set; } = "0";
    [JsonPropertyName("renderScale")] public float RenderScale = 1f;
    [JsonPropertyName("radius")] public float Radius { get; set; } = 8;
    [JsonPropertyName("lifetime")] public double Lifetime { get; set; } = 10;
    [JsonPropertyName("useShape")] public bool UseShape { get; set; } = false;
    [JsonPropertyName("shape")] public float[][] Shape { get; set; } = null;
    [JsonPropertyName("persistant")] public bool Persistant { get; set; } = false;
    [JsonPropertyName("texture")] public string Texture { get; set; } = "default";
    // editor only
    [JsonIgnore] public Func<EvalContext, double> efnx;
    [JsonIgnore] public Func<EvalContext, double> efny;
    [JsonIgnore] public int RenderGroupId;
    public ProjectileModel() { }
    public ProjectileModel(ProjectileModel other)
    {
        Name = other.Name;
        FunctionX = other.FunctionX;
        FunctionY = other.FunctionY;
        Radius = other.Radius;
        Lifetime = other.Lifetime;
        UseShape = other.UseShape;
        Persistant = other.Persistant;
        Texture = other.Texture;
        if (other.Shape != null)
        {
            Shape = new float[other.Shape.Length][];
            for (int i = 0; i < other.Shape.Length; i++)
                Shape[i] = (float[])other.Shape[i].Clone();
        }
    }
}
public class PatternModel : IEditorModel
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("projectileId")] public int ProjectileId { get; set; }
    [JsonPropertyName("fnX")] public string FunctionX { get; set; } = "0";
    [JsonPropertyName("fnY")] public string FunctionY { get; set; } = "0";
    [JsonPropertyName("fnT")] public string FunctionT { get; set; } = "0";
    [JsonPropertyName("fnFwd")] public string FunctionFwd { get; set; } = "0";
    [JsonPropertyName("count")] public int Count { get; set; } = 1;
    // editor only
    [JsonIgnore] public Func<EvalContext, double> efnx;
    [JsonIgnore] public Func<EvalContext, double> efny;
    [JsonIgnore] public Func<EvalContext, double> efnt;
    [JsonIgnore] public Func<EvalContext, double> efnfwd;
    [JsonIgnore] public float lifetime;
    public PatternModel() { }
    public PatternModel(PatternModel other)
    {
        Name = other.Name;
        ProjectileId = other.ProjectileId;
        FunctionX = other.FunctionX;
        FunctionY = other.FunctionY;
        FunctionT = other.FunctionT;
        FunctionFwd = other.FunctionFwd;
        Count = other.Count;
    }
}
public class LevelData
{
    [JsonPropertyName("displayName")] public string DisplayName { get; set; }
    [JsonPropertyName("author")] public string Author { get; set; }
    [JsonPropertyName("bgImage")] public string BgImage { get; set; }
    [JsonPropertyName("health")] public int Health { get; set; } = 3;
    [JsonPropertyName("aspectRatio")] public int AspectRatio { get; set; } = 1;
    [JsonPropertyName("duration")] public float Duration { get; set; } = 1;
    [JsonPropertyName("projectileModels")] public ProjectileModel[] ProjectileModels { get; set; } = null;
    [JsonPropertyName("bullets")] public List<EditorReference> References { get; set; } = null;
    [JsonPropertyName("patternModels")] public PatternModel[] PatternModels { get; set; } = null;
    [JsonPropertyName("id")] public string LevelId { get; set; }
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
    [ConfigField("Game", "Slow render updates on far projectiles")]
    [JsonPropertyName("slowRenderUpdatesOnFarProjectiles")] public bool SlowRenderUpdatesOnFarProjectiles { get; set; } = false;

    // EDITOR
    [ConfigField("Editor", "Path fidelity", 1, 256, 1)]
    [JsonPropertyName("pathFidelity")] public int PathFidelity { get; set; } = 64;

    [ConfigField("Editor", "Max path length", 1, 2048, 0.5)]
    [JsonPropertyName("MaxPathLength")] public float MaxPathLength { get; set; } = 128;
}