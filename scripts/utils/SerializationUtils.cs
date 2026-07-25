using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

public static class SerializationUtils
{
   public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new Vector2JsonConverter() }
    };
    public static T ReadJson<T>(string path)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            Console.Inst.LogErr($"[GameSession] Could not open file: {path}  (error: {FileAccess.GetOpenError()})");
            return default;
        }
        try { return JsonSerializer.Deserialize<T>(file.GetAsText(), Options); }
        catch (JsonException ex)
        {
            Console.Inst.LogErr($"[GameSession] JSON parse error in '{path}': {ex.Message}");
            return default;
        }
    }
    public static void WriteJson<T>(string path, T data)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            Console.Inst.LogErr($"Failed to open file for writing: {path}");
        }
        var s = JsonSerializer.Serialize(data, Options);
        file.StoreString(s);
    }
    public class Vector2JsonConverter : JsonConverter<Vector2>
    {
        public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Utf8JsonReader snapshot = reader;
            try
            {
                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    reader.Skip();
                    return Vector2.Zero;
                }
                reader.Read();
                float x = reader.GetSingle();
                reader.Read();
                float y = reader.GetSingle();
                reader.Read();
                return new Vector2(x, y);
            }
            catch
            {
                reader = snapshot;
                reader.Skip();
                GD.PushWarning($"Failed to parse Vector2, defaulting to zero");
                return Vector2.Zero;
            }
        }
        public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.X);
            writer.WriteNumberValue(value.Y);
            writer.WriteEndArray();
        }
    }
}
