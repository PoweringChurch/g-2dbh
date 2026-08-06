using System.Text.Json.Serialization;
using Godot;

public class BackgroundLayer
{
    public string Name {get; set;} = "unnamed layer";
    public int BackgroundId {get; set;} = 0;
    public int RepeatCount {get; set;} = 2;
    public string ScrollFunctionX {get; set;} = "0";
    public string ScrollFunctionY {get; set;} = "0";
    public string TransparencyFn {get; set;} = "0";
    public int Order {get; set;} = 0;
    public float Scale {get; set;} = 1;
}
