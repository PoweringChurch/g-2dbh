using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public partial class StoryInfo : Control
{
    [Export] public StoryLevelButton[] LevelButtons; // in order
    [Export] public string WorldName;
    [Export] public CampaignCharacterDisplay CharacterDisplay;
    private LevelData[] datas;
    public event Action<LevelData> LevelSelected;
    public override void _Ready()
    {
        datas = new LevelData[LevelButtons.Length];
        for (int i = 0; i < LevelButtons.Length; i++)
        {
            var btn = LevelButtons[i];
            datas[i] = ReadJson<LevelData>(btn.LevelPath);
            int cache = i;
            btn.Pressed += () => ClickButton(cache);
        }
    }
    public void ClickButton(int idx)
    {
        var btn = LevelButtons[idx];
        var currentData = datas[idx];
        LevelSelected?.Invoke(currentData); 
        var pos = btn.PositionAsOffset ? btn.Position+btn.CharacterPosition
                : btn.CharacterPosition;
        CharacterDisplay.MoveTo(pos);
    }
    public LevelData GetLeveldata(int idx) => datas[idx];
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
