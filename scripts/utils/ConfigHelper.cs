using System.Text.Json;
using Godot;

public partial class ConfigHelper : Node
{
    private const string _configPath = "user://data/config.json";
    public static Config Current;
    public override void _Ready()
    {
        // get config
        if (!FileAccess.FileExists(_configPath))
        {
            DirAccess.MakeDirRecursiveAbsolute("user://data");
            DirAccess.MakeDirAbsolute("user://data/levels");
            DirAccess.MakeDirAbsolute("user://data/characters");
            DirAccess.MakeDirAbsolute("user://data/sounds");
            DirAccess.MakeDirAbsolute("user://data/logs");
            WriteJson(_configPath, new Config());
        }
        Current = ReadJson<Config>(_configPath);
    }
    public static void Save()
    {
        WriteJson(_configPath, Current);
    }
    private static T ReadJson<T>(string path)
    {
        if (!FileAccess.FileExists(path)) return default;
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null) return default;
        try { return JsonSerializer.Deserialize<T>(file.GetAsText()); }
        catch { return default; }
    }
    private static void WriteJson<T>(string path, T data)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            Console.Inst.LogErr($"[Config Helper] Failed to open file for writing: {path}");
        }
        var s = JsonSerializer.Serialize(data);
        file.StoreString(s);
    }
}