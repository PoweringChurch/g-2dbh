using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

public class ProjectileModel : IEditorModel
{
    // Core Metadata
    public int Id { get; set; } = 0;

    public string Name { get; set; } = "unnamed";

    // Movement & Math Functions
    public string FunctionX { get; set; } = "0";
    public string FunctionY { get; set; } = "0";
    public string FunctionF { get; set; } = "0";

    // Visuals & Rendering
    public string TextureName { get; set; } = "orb.png";
    public Color Tint {get; set;} = Colors.White;
    public Vector2 RenderScale { get; set; } = Vector2.One;
    public bool LockRotation { get; set; } = false;
    public float TelegraphTime { get; set; } = 0.5f;
    // Collision & Lifetime
    public float Radius { get; set; } = 12.5f;
    public double Lifetime { get; set; } = 10;
    public bool CanCollide { get; set; } = true;
    public bool Persistant { get; set; } = false;
    public bool FacePlayer { get; set; }
    // Custom Collision Shape
    public bool UseShape { get; set; } = false;
    public List<Vector2> Shape { get; set; } = new();
    // Spawning Mechanics
    public List<EditorReference> Spawns {get; set;} = [];
    public int MaxDepth { get; set; } = 1;
    // Runtime / Game-Only Properties
    [JsonIgnore] public Func<EvalContext, double> fnx { get; set; }
    [JsonIgnore] public Func<EvalContext, double> fny { get; set; }
    [JsonIgnore] public Func<EvalContext, double> fnf { get; set; }
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
        TextureName = other.TextureName;
        RenderScale = other.RenderScale;
        TelegraphTime = other.TelegraphTime;
        LockRotation = other.LockRotation;
        Tint = other.Tint;
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
        Shape = new(other.Shape);
        
        fnx = other.fnx;
        fny = other.fny;
        fnf = other.fnf;
    }
}