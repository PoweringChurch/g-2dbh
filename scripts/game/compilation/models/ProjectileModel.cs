using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Godot;

public class ProjectileModel : IEditorModel
{
    // ==========================================
    // Core Metadata
    // ==========================================
    [JsonPropertyName("id")] 
    public int Id { get; set; } = 0;

    [JsonPropertyName("name")] 
    public string Name { get; set; } = "unnamed";

    // ==========================================
    // Movement & Math Functions
    // ==========================================
    [JsonPropertyName("fnX")] 
    public string FunctionX { get; set; } = "0";

    [JsonPropertyName("fnY")] 
    public string FunctionY { get; set; } = "0";

    // ==========================================
    // Visuals & Rendering
    // ==========================================
    [JsonPropertyName("texture")] 
    public string Texture { get; set; } = "default";

    [JsonPropertyName("renderScale")] 
    public float RenderScale { get; set; } = 1f;

    [JsonPropertyName("telegraphTime")] 
    public float TelegraphTime { get; set; } = 0f;

    // ==========================================
    // Collision & Lifetime
    // ==========================================
    [JsonPropertyName("radius")] 
    public float Radius { get; set; } = 8;

    [JsonPropertyName("lifetime")] 
    public double Lifetime { get; set; } = 10;

    [JsonPropertyName("canCollide")] 
    public bool CanCollide { get; set; } = true;

    [JsonPropertyName("persistant")] 
    public bool Persistant { get; set; } = false;

    // ==========================================
    // Custom Collision Shape
    // ==========================================
    [JsonPropertyName("useShape")] 
    public bool UseShape { get; set; } = false;

    [JsonPropertyName("shape")] 
    public float[][] Shape { get; set; } = null;

    // ==========================================
    // Death Spawning Mechanics (Nested Patterns)
    // ==========================================
    [JsonPropertyName("spawnModelOnDeath")] 
    public bool SpawnModelOnDeath { get; set; } = false;

    [JsonPropertyName("spawnOnDeathType")] 
    public ModelType SpawnOnDeathType { get; set; } = ModelType.Projectile;

    [JsonPropertyName("spawnOnDeath")] 
    public int SpawnOnDeathId { get; set; } = 0;

    [JsonPropertyName("maxDepth")] 
    public int MaxDepth { get; set; } = 1;

    // ==========================================
    // Runtime / Game-Only Properties
    // ==========================================
    [JsonIgnore] public Func<EvalContext, double> fnx { get; set; }
    [JsonIgnore] public Func<EvalContext, double> fny { get; set; }
    [JsonIgnore] public int RenderGroupId { get; set; }
    [JsonIgnore] public List<Vector2> ShapeVect2s { get; set; }

    // ==========================================
    // Constructors
    // ==========================================
    public ProjectileModel() { }

    public ProjectileModel(ProjectileModel other)
    {
        if (other == null) return;

        // Metadata & Identifiers
        Id = other.Id;
        Name = other.Name;

        // Math & Movement
        FunctionX = other.FunctionX;
        FunctionY = other.FunctionY;

        // Visuals
        Texture = other.Texture;
        RenderScale = other.RenderScale;
        TelegraphTime = other.TelegraphTime;

        // Collision & Lifetime
        Radius = other.Radius;
        Lifetime = other.Lifetime;
        CanCollide = other.CanCollide;
        Persistant = other.Persistant;

        // Nested On-Death Spawning
        SpawnModelOnDeath = other.SpawnModelOnDeath;
        SpawnOnDeathType = other.SpawnOnDeathType;
        SpawnOnDeathId = other.SpawnOnDeathId;
        MaxDepth = other.MaxDepth;

        // Deep copy the custom shape matrix
        UseShape = other.UseShape;
        if (other.Shape != null)
        {
            Shape = new float[other.Shape.Length][];
            for (int i = 0; i < other.Shape.Length; i++)
            {
                if (other.Shape[i] != null)
                {
                    Shape[i] = (float[])other.Shape[i].Clone();
                }
            }
        }

        // Copy Runtime Cache (Optional, but usually preferred for deep copies)
        fnx = other.fnx;
        fny = other.fny;
        RenderGroupId = other.RenderGroupId;
        if (other.ShapeVect2s != null)
        {
            ShapeVect2s = new List<Vector2>(other.ShapeVect2s);
        }
    }
}