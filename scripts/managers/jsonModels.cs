using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

public interface ISpatialReference
{
    string Id {get; set;}
    float X { get; set; }
    float Y { get; set; }
    float T { get; set; }
    float Forward { get; set; }
}
public interface IEditorModel
{
    string Id { get; set; }
}
public class ProjectileModel : IEditorModel
{
    [JsonPropertyName("id")]            public string   Id         { get; set; }
    [JsonPropertyName("fnX")]           public string   FunctionX   { get; set; } = "0";
    [JsonPropertyName("fnY")]           public string   FunctionY   { get; set; } = "0";
    [JsonPropertyName("radius")]        public float       Radius     { get; set; } = 8;
    [JsonPropertyName("lifetime")]      public float       Lifetime   { get; set; } = 10;
    [JsonPropertyName("useShape")]      public bool         UseShape   { get; set; } = false;
    [JsonPropertyName("shape")]         public float[][]    Shape      { get; set; } = null;
    [JsonPropertyName("persistant")]    public bool         Persistant { get; set; } = false;
    [JsonPropertyName("texture")]       public string       Texture    { get; set; } = "default";
    public ProjectileModel() {}
    public ProjectileModel(ProjectileModel other)
    {
        Id = other.Id;
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
public class ProjectileReference : ISpatialReference
{
    [JsonPropertyName("modelId")]        public string Id   { get; set; }
    [JsonPropertyName("x")]         public float  X         { get; set; } = 0;
    [JsonPropertyName("y")]         public float  Y         { get; set; } = 0;
    [JsonPropertyName("t")]         public float  T         { get; set; } = 0; // projectile spawn time
    [JsonPropertyName("forward")]   public float  Forward { get; set; } = 0; // degrees
    public ProjectileReference() {}
    public ProjectileReference(ProjectileReference other)
    {
        Id = other.Id;
        X = other.X;
        Y = other.Y;
        T = other.T;
        Forward = other.Forward;
    }
}
public class PatternModel : IEditorModel
{
    [JsonPropertyName("id")]            public string   Id { get; set; }
    [JsonPropertyName("projectileId")]  public string   ProjectileId { get; set; }
    [JsonPropertyName("fnX")]           public string   FunctionX   { get; set; } = "0";
    [JsonPropertyName("fnY")]           public string   FunctionY   { get; set; } = "0";
    [JsonPropertyName("fnT")]           public string   FunctionT   { get; set; } = "0";
    [JsonPropertyName("fnFwd")]         public string   FunctionFwd   { get; set; } = "0";
    [JsonPropertyName("count")]         public int      Count       {get; set;} = 1;
    public PatternModel() {}
    public PatternModel(PatternModel other)
    {
        Id = other.Id;
        ProjectileId = other.ProjectileId;
        FunctionX = other.FunctionX;
        FunctionY = other.FunctionY;
        FunctionT = other.FunctionT;
        FunctionFwd = other.FunctionFwd;
    }
}
public class PatternReference : ISpatialReference
{
    [JsonPropertyName("patternId")] public string Id        { get; set; }
    [JsonPropertyName("x")]         public float  X         { get; set; }
    [JsonPropertyName("y")]         public float  Y         { get; set; }
    [JsonPropertyName("t")]         public float  T         { get; set; }
    [JsonPropertyName("forward")]   public float  Forward   { get; set; }
}
public class LevelData
{
    [JsonPropertyName("displayName")]   public string   DisplayName { get; set; }
    [JsonPropertyName("author")]        public string   Author      { get; set; }
    [JsonPropertyName("difficulty")]    public string   Difficulty  { get; set; }
    [JsonPropertyName("bgImage")]       public string   BgImage     { get; set; }
    [JsonPropertyName("health")]        public int      Health      { get; set; } = 3;
    [JsonPropertyName("aspectRatio")]   public int      AspectRatio { get; set; } = 1;
    [JsonPropertyName("duration")]      public float    Duration     { get; set; } = 1;
    public string                       LevelId  {get; set;}
    public List<ProjectileModel>        ProjectileModels  {get; set;} = null;
    public List<ProjectileReference>    Projectiles {get; set;} = null;
    public List<PatternModel>           PatternModels  {get; set;} = null;
    public List<PatternReference>       Patterns {get; set;} = null;
}
public class EditorPrefs
{
    // Rendering
    [JsonPropertyName("pathRenderPrecision")] public int PathRenderPrecision { get; set; } = 16;
    [JsonPropertyName("deadOpacity")] public float DeadOpacity { get; set; } = 0.3f;
    [JsonPropertyName("showProjectilePathsInPatternCreator")] public bool ShowFunctionPathsInPatternCreator { get; set; } = true;
    [JsonPropertyName("showQueuedProjectilesInPreview")] public bool ShowQueuedProjectilesInPreview { get; set; } = true;
    [JsonPropertyName("useProjectileTextureInPatternsPreview")] public bool UseProjectileTextureInPatternsPreview { get; set; } = false;
    [JsonPropertyName("showCollision")] public bool ShowCollision { get; set; } = false;
    // Placement
    [JsonPropertyName("snapToGrid")] public bool SnapToGrid { get; set; } = true;
    [JsonPropertyName("gridSize")] public float GridSize { get; set; } = 16f;
    [JsonPropertyName("showGrid")] public bool ShowGrid { get; set; } = true;
    // Playback
    [JsonPropertyName("loopPlayback")] public bool LoopPlayback { get; set; } = false;
    // Saving
    [JsonPropertyName("autosaveEnabled")] public bool AutosaveEnabled { get; set; } = true;
    [JsonPropertyName("autosaveIntervalSeconds")] public int AutosaveIntervalSeconds { get; set; } = 120;
    [JsonPropertyName("undoHistoryDepth")] public int UndoHistoryDepth { get; set; } = 50;
    // Model Library
    [JsonPropertyName("modelLibrarySortMode")] public string ModelLibrarySortMode { get; set; } = "name";
    [JsonPropertyName("groupModelsByType")] public bool GroupModelsByType { get; set; } = true;
}