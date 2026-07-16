using System.Text.Json.Serialization;
using Godot;

public class BackgroundLayer
{
    [JsonPropertyName("name")] public string Name {get; set;} = "unnamed layer";
    [JsonPropertyName("backgroundId")] public int BackgroundId {get; set;} = 0;
    [JsonPropertyName("repeatCount")] public int RepeatCount {get; set;} = 2;
    [JsonPropertyName("scrollFnX")] public string ScrollFunctionX {get; set;} = "0";
    [JsonPropertyName("scrollFnY")] public string ScrollFunctionY {get; set;} = "0";
    [JsonPropertyName("transparencyFnY")] public string TransparencyFn {get; set;} = "0";
    [JsonPropertyName("order")] public int Order {get; set;} = 0;
    [JsonPropertyName("scale")] public float Scale {get; set;} = 1;
}
