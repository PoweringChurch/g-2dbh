using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

public static class SerializationUtils
{
   public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new Vector2JsonConverter(), new Color2JsonConverter() }
    };
    public static T ReadJson<T>(string path)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            Console.Inst.LogErr($"[Serializer] Could not open file '{path}' (error: {FileAccess.GetOpenError()})");
            return default;
        }
        try { return JsonSerializer.Deserialize<T>(file.GetAsText(), Options); }
        catch (JsonException ex)
        {
            Console.Inst.LogErr($"[Serializer] JSON parse error in '{path}' (error: {ex.Message})");
            return default;
        }
    }
    public static void WriteJson<T>(string path, T data)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            Console.Inst.LogErr($"[Serializer] Failed to open file for writing '{path}'");
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
    public class Color2JsonConverter : JsonConverter<Color>
    {
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Utf8JsonReader snapshot = reader;
            try
            {
                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    reader.Skip();
                    return Colors.Black;
                }
                reader.Read();
                float r = reader.GetSingle();
                reader.Read();
                float g = reader.GetSingle();
                reader.Read();
                float b = reader.GetSingle();
                reader.Read();
                float a = reader.GetSingle();
                reader.Read();
                return new Color(r, g, b, a);
            }
            catch
            {
                reader = snapshot;
                reader.Skip();
                GD.PushWarning("Failed to parse Color, defaulting to black");
                return Colors.Black;
            }
        }

        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.R);
            writer.WriteNumberValue(value.G);
            writer.WriteNumberValue(value.B);
            writer.WriteNumberValue(value.A);
            writer.WriteEndArray();
        }
    }
}
