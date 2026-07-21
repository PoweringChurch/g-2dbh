using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Godot;

public class ProjectileModel : IEditorModel
{
    // Core Metadata
    [JsonPropertyName("id")] 
    public int Id { get; set; } = 0;

    [JsonPropertyName("name")] 
    public string Name { get; set; } = "unnamed";

    // Movement & Math Functions
    [JsonPropertyName("fnX")] 
    public string FunctionX { get; set; } = "0";

    [JsonPropertyName("fnY")] 
    public string FunctionY { get; set; } = "0";
    [JsonPropertyName("fnF")] 
    public string FunctionF { get; set; } = "0";

    // Visuals & Rendering
    [JsonPropertyName("textureId")]
    public int TextureId { get; set; } = 0;

    [JsonPropertyName("renderScale")] 
    public float RenderScale { get; set; } = 1f;
    [JsonPropertyName("lockRotation")] 
    public bool LockRotation { get; set; } = false;

    [JsonPropertyName("telegraphTime")] 
    public float TelegraphTime { get; set; } = 0f;
    // Collision & Lifetime
    [JsonPropertyName("radius")] 
    public float Radius { get; set; } = 8;

    [JsonPropertyName("lifetime")] 
    public double Lifetime { get; set; } = 10;

    [JsonPropertyName("canCollide")] 
    public bool CanCollide { get; set; } = true;

    [JsonPropertyName("persistant")] 
    public bool Persistant { get; set; } = false;
    [JsonPropertyName("facePlayer")] 
    public bool FacePlayer { get; set; }

    // Custom Collision Shape
    [JsonPropertyName("useShape")] 
    public bool UseShape { get; set; } = false;

    [JsonPropertyName("shape")] 
    public float[][] Shape { get; set; } = null;

    // Spawning Mechanics
    [JsonPropertyName("spawns")] 
    public List<EditorReference> Spawns {get; set;} = [];

    [JsonPropertyName("maxDepth")] 
    public int MaxDepth { get; set; } = 1;

    // Runtime / Game-Only Properties
    [JsonIgnore] public Func<EvalContext, double> fnx { get; set; }
    [JsonIgnore] public Func<EvalContext, double> fny { get; set; }
    [JsonIgnore] public Func<EvalContext, double> fnf { get; set; }
    [JsonIgnore] public int RenderGroupId { get; set; }
    [JsonIgnore] public List<Vector2> ShapeVect2s { get; set; }
    [JsonIgnore] public List<SpatialReference> RuntimeSpawns { get; set; }
    // Constructors
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
        FunctionF = other.FunctionF;

        // Visuals
        TextureId = other.TextureId;
        RenderScale = other.RenderScale;
        TelegraphTime = other.TelegraphTime;
        LockRotation = other.LockRotation;
        // Collision & Lifetime
        Radius = other.Radius;
        Lifetime = other.Lifetime;
        CanCollide = other.CanCollide;
        Persistant = other.Persistant;

        Spawns = [];
        for (int i = 0; i < other.Spawns.Count; i++)
        {
            var spawn = other.Spawns[i];
            Spawns.Add(new(spawn));
        }
        MaxDepth = other.MaxDepth;

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

        fnx = other.fnx;
        fny = other.fny;
        fnf = other.fnf;
        RenderGroupId = other.RenderGroupId;
        if (other.ShapeVect2s != null)
            ShapeVect2s = new List<Vector2>(other.ShapeVect2s);
        if (other.RuntimeSpawns != null)
            RuntimeSpawns = new List<SpatialReference>(other.RuntimeSpawns);
    }
}