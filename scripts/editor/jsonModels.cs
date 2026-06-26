using System;
using Godot;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public enum ModelType { Projectile, Pattern }
public class EditorReference
{
    public Vector2 SpawnPos {get; set;}
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
    [JsonIgnore] public int RenderGroupId;
    public PatternModel() { }
    public PatternModel(PatternModel other)
    {
        Name = other.Name;
        ProjectileId = other.ProjectileId;
        FunctionX = other.FunctionX;
        FunctionY = other.FunctionY;
        FunctionT = other.FunctionT;
        FunctionFwd = other.FunctionFwd;
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
    [JsonPropertyName("projectileModels")] public List<ProjectileModel> ProjectileModels { get; set; } = null;
    [JsonPropertyName("bullets")] public List<EditorReference> References { get; set; } = null;
    [JsonPropertyName("patternModels")] public List<PatternModel> PatternModels { get; set; } = null;
    [JsonPropertyName("id")] public string LevelId { get; set; }
}