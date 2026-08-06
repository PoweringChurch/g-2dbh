using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text;
using System.IO;
using System.IO.Compression;

public class RawLevelData
{
    public string LocalId {get; set;}
    public string Name { get; set; } = "New Level";
    public string Author { get; set; } = "Unknown";
    public int MusicId { get; set; } = 0;
    public int AspectRatio { get; set; } = 0;
    public int Character { get; set; } = 0;
    public float Duration { get; set; } = 0;
    public float Difficulty {get; set;} = 1;
    public ProjectileModel[] ProjectileModels { get; set; } = new ProjectileModel[Editor.MaxModelCount];
    public PatternModel[] PatternModels { get; set; } = new PatternModel[Editor.MaxModelCount];
    public List<EditorReference> References { get; set; } = [];
    public Dictionary<string, string> CustomVariables { get; set; } = [];
    public List<BackgroundLayer> BackgroundLayers {get; set;} = [];
    public List<string> Tags {get; set;} = [];
}
public class LevelDataSchema
{
    [JsonPropertyName("local_id")] public string LocalId {get; set;}
    [JsonPropertyName("id")] public string Id {get; set;}
    [JsonPropertyName("data")] public string Data {get; set;}
    [JsonPropertyName("name")] public string Name {get; set;} = "Unnamed";
    [JsonPropertyName("author")] public string Author {get; set;} = "Unknown";
    [JsonPropertyName("music_id")] public int MusicId {get; set;} = 0;
    [JsonPropertyName("aspect_ratio")] public int AspectRatio { get; set; } = 0;
    [JsonPropertyName("duration")] public float Duration {get; set;} = 0;
    [JsonPropertyName("difficulty")] public float Difficulty {get; set;} = 1;
    [JsonPropertyName("tags")] public List<string> Tags {get; set;} = [];
}

public static class LevelDataConverter
{
    public static LevelDataSchema ToSchema(RawLevelData raw, string localId)
    {
        string json = JsonSerializer.Serialize(raw, SerializationUtils.Options);
        string compressed = CompressToBase64(json);

        return new LevelDataSchema
        {
            LocalId = localId,
            Data = compressed,
            Name = raw.Name,
            Author = raw.Author,
            MusicId = raw.MusicId,
            AspectRatio = raw.AspectRatio,
            Duration = raw.Duration,
            Difficulty = raw.Difficulty,
            Tags = raw.Tags
        };
    }
    public static RawLevelData FromSchema(LevelDataSchema schema)
    {
        string json = DecompressFromBase64(schema.Data);
        return JsonSerializer.Deserialize<RawLevelData>(json, SerializationUtils.Options);
    }

    private static string CompressToBase64(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
            gzip.Write(bytes, 0, bytes.Length);
        return Convert.ToBase64String(output.ToArray());
    }

    private static string DecompressFromBase64(string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        using var input = new MemoryStream(bytes);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}