using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public partial class StoryInfo : Control
{
    [Export] public StoryLevelButton[] LevelButtons; // in order
    [Export] public string WorldName;
    [Export] public Control CharacterDisplay;
    [Export] public Vector2 DisplayOffset;
    private LevelData[] datas;
    public event Action<LevelData> LevelSelected;
    public override void _Ready()
    {
        datas = new LevelData[LevelButtons.Length];
        for (int i = 0; i < LevelButtons.Length; i++)
        {
            var btn = LevelButtons[i];
            datas[i] = ReadJson<LevelData>(btn.LevelPath);
            LevelData currentData = datas[i];
            btn.Pressed += () => LevelSelected?.Invoke(currentData);
            CharacterDisplay.Position = (!btn.PositionAsOffset) ? btn.CharacterPosition : btn.Position+btn.CharacterPosition;
        }
    }
    private static T ReadJson<T>(string path)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            Console.Inst.LogErr($"[GameSession] Could not open file: {path}  (error: {FileAccess.GetOpenError()})");
            return default;
        }
        try { return JsonSerializer.Deserialize<T>(file.GetAsText()); }
        catch (JsonException ex)
        {
            Console.Inst.LogErr($"[GameSession] JSON parse error in '{path}': {ex.Message}");
            return default;
        }
    }
}
