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
    [JsonPropertyName("id")] public int Id { get; set; } = 0;
    [JsonPropertyName("name")] public string Name { get; set; } = "unnamed";
    [JsonPropertyName("fnX")] public string FunctionX { get; set; } = "0";
    [JsonPropertyName("fnY")] public string FunctionY { get; set; } = "0";
    [JsonPropertyName("radius")] public float Radius { get; set; } = 8;
    [JsonPropertyName("lifetime")] public double Lifetime { get; set; } = 10;
    [JsonPropertyName("useShape")] public bool UseShape { get; set; } = false;
    [JsonPropertyName("shape")] public float[][] Shape { get; set; } = null;
    [JsonPropertyName("persistant")] public bool Persistant { get; set; } = false;
    [JsonPropertyName("texture")] public string Texture { get; set; } = "default";
    [JsonPropertyName("renderScale")] public float RenderScale {get; set;} = 1f;
    [JsonPropertyName("lockRotation")] public bool LockRotation {get; set;} = false;
    // editor only
    [JsonIgnore] public Func<EvalContext, double> efnx;
    [JsonIgnore] public Func<EvalContext, double> efny;
    [JsonIgnore] public int RenderGroupId;
    public ProjectileModel() { }
    public ProjectileModel(ProjectileModel other)
    {
        other ??= new();
        Name = other.Name;
        FunctionX = other.FunctionX;
        FunctionY = other.FunctionY;
        Radius = other.Radius;
        Lifetime = other.Lifetime;
        UseShape = other.UseShape;
        Persistant = other.Persistant;
        Texture = other.Texture;
        RenderScale = other.RenderScale;
        LockRotation = other.LockRotation;
        Id = other.Id;
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
    [JsonPropertyName("id")] public int Id { get; set; } = 0;
    [JsonPropertyName("name")] public string Name { get; set; } = "unnamed";
    [JsonPropertyName("projectileId")] public int ProjectileId { get; set; } = 0;
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
        other ??= new();
        Id = other.Id;
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
    [JsonPropertyName("music")] public string Music { get; set; }
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