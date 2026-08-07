using Godot;
using System;
using System.Collections.Generic;

public static class LevelStorage
{
    public const string SavedLevelsDirectory = "user://data/levels/";
    public static void SaveLevel(RawLevelData raw)
    {
        var schema = LevelDataConverter.ToSchema(raw, raw.LocalId);
        var path = $"{SavedLevelsDirectory}{raw.LocalId}.json";
        SerializationUtils.WriteJson(path, schema);
        Console.LogInfo($"Saved level to '{path}'");
    }
    public static List<LevelDataSchema> GetSavedLevels()
    {
        var list = new List<LevelDataSchema>();
        using var dir = DirAccess.Open(SavedLevelsDirectory);
        dir.ListDirBegin();
        string entry = dir.GetNext();
        while (entry != "")
        {
            if (entry.EndsWith(".json"))
            {
                list.Add(SerializationUtils.ReadJson<LevelDataSchema>($"{SavedLevelsDirectory}{entry}"));
            }
            entry = dir.GetNext();
        }
        dir.ListDirEnd();
        return list;
    }
    public static void DeleteLevel(string LocalId)
    {
        using var dir = DirAccess.Open(SavedLevelsDirectory);
        var path = $"{LocalId}.json";
        if (dir.FileExists(path))
        {
            dir.Remove(path);
        }
        Console.LogInfo($"Deleted level at '{path}'");
    }
}
