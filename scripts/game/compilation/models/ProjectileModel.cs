using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Godot;

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
    [JsonPropertyName("spawnModelOnDeath")] public bool SpawnModelOnDeath {get; set;} = false;
    [JsonPropertyName("spawnOnDeathType")] public ModelType SpawnOnDeathType {get; set;} = ModelType.Projectile;
    [JsonPropertyName("spawnOnDeath")] public int SpawnOnDeathId {get; set;} = 0;
    [JsonPropertyName("maxDepth")] public int MaxDepth {get; set;} = 1;
    // game only
    [JsonIgnore] public Func<EvalContext, double> fnx;
    [JsonIgnore] public Func<EvalContext, double> fny;
    [JsonIgnore] public int RenderGroupId;
    [JsonIgnore] public List<Vector2> ShapeVect2s;
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

        SpawnModelOnDeath = other.SpawnModelOnDeath;
        SpawnOnDeathType = other.SpawnOnDeathType;
        SpawnOnDeathId = other.SpawnOnDeathId;
        MaxDepth = other.MaxDepth;

        Id = other.Id;
        if (other.Shape != null)
        {
            Shape = new float[other.Shape.Length][];
            for (int i = 0; i < other.Shape.Length; i++)
                Shape[i] = (float[])other.Shape[i].Clone();
        }
    }
}