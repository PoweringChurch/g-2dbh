using System;
using System.Text.Json.Serialization;
using Godot;
public class PatternModel : IEditorModel
{
    public int Id { get; set; } = 0;
    public string Name { get; set; } = "unnamed";

    public string SFXName {get; set;} = "none";
    public float SoundVolume {get; set;} = 1;

    public int SpawningId { get; set; } = 0;
    public ModelType SpawningType { get; set; } = ModelType.Projectile;
    public string FunctionX { get; set; } = "0";
    public string FunctionY { get; set; } = "0";
    public string FunctionT { get; set; } = "0";
    public string FunctionF { get; set; } = "0";
    public int Count { get; set; } = 1;
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
        SpawningId = other.SpawningId;
        SpawningType = other.SpawningType;
        SFXName = other.SFXName;
        SoundVolume = other.SoundVolume;
        FunctionX = other.FunctionX;
        FunctionY = other.FunctionY;
        FunctionT = other.FunctionT;
        FunctionF = other.FunctionF;
        Count = other.Count;
    }
}