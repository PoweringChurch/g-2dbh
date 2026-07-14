using System;
using System.Text.Json.Serialization;
using Godot;
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
    [JsonPropertyName("facePlayer")] public bool FacePlayer { get; set; } = false;
    // game only
    [JsonIgnore] public Func<EvalContext, double> fnx;
    [JsonIgnore] public Func<EvalContext, double> fny;
    [JsonIgnore] public Func<EvalContext, double> fnt;
    [JsonIgnore] public Func<EvalContext, double> fnf;
    // editor only
    [JsonIgnore] public float lifetime;
    [JsonIgnore] public int renderGroupId;
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
        FacePlayer = other.FacePlayer;
    }
}