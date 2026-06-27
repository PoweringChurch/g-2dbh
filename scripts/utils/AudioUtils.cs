using Godot;
using System.Collections.Generic;
using System.Linq;
public partial class AudioUtils : Node
{
    public static AudioUtils Instance { get; private set; }
    const string soundsDir = "user://data/sounds/";
    public override void _Ready()
    {
        Instance = this;
    }
    public bool PlayAudio(string audioName, float volume = 1)
    {
        var audio = LoadAudio(soundsDir, audioName);
        if (audio == null)
        {
            audio = ResourceLoader.Load<AudioStream>($"res://data/sounds/{audioName}.wav"); // try default
            if (audio == null)
                return false;
        }
        AudioStreamPlayer player = new() { Stream = audio, VolumeLinear = volume};
        AddChild(player);
        player.Play();
        player.Finished += player.QueueFree;
        return true;
    }
    private static Dictionary<string, AudioStream> _audioCache = [];
    public static AudioStream LoadAudio(string inDir, string audioName)
    {
        string cacheKey = inDir + audioName;
        if (_audioCache.TryGetValue(cacheKey, out var cached))
            return cached;

        foreach (string ext in new[] { "" }.Concat(ValidExtensions.Audio))
        {
            string path = $"{inDir}{audioName}{ext}";
            if (!FileAccess.FileExists(path))
                continue;

            AudioStream stream = null;
            string lowercaseExt = ext.ToLower();

            if (lowercaseExt == ".wav")
            {
                var wavStream = new AudioStreamWav();
                // Godot needs raw bytes for WAV loading
                byte[] bytes = FileAccess.GetFileAsBytes(path);
                wavStream.Data = bytes;
                
                // Note: You may need to manually set format properties if your WAVs vary:
                // wavStream.Format = AudioStreamWav.FormatEnum.Format16Bits;
                // wavStream.MixRate = 44100;
                // wavStream.Stereo = true;

                stream = wavStream;
            }
            else if (lowercaseExt == ".mp3")
            {
                var mp3Stream = new AudioStreamMP3();
                byte[] bytes = FileAccess.GetFileAsBytes(path);
                mp3Stream.Data = bytes;
                stream = mp3Stream;
            }
            else if (lowercaseExt == ".ogg")
            {
                stream = AudioStreamOggVorbis.LoadFromFile(path);
            }
            if (stream != null)
            {
                _audioCache[cacheKey] = stream;
                return stream;
            }
        }
        return null;
    } 
}
