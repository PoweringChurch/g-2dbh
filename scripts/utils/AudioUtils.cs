using Godot;
using System.Collections.Generic;
using System.Linq;
public partial class AudioUtils : Node
{
    public const float MusicVolumeMultiplier = 0.3f;
    public static AudioUtils Instance { get; private set; }
    const string soundsDir = "user://data/sounds/";
    public override void _Ready()
    {
        Instance = this;
    }
    public AudioStreamPlayer PlayAudio(string audioName, float volume = 1, float time = 0)
    {
        var audio = LoadAudio(soundsDir, audioName);
        if (audio == null)
        {
            audio = ResourceLoader.Load<AudioStream>($"res://data/sounds/{audioName}.wav"); // try default
            if (audio == null)
                return null;
        }
        return PlayAudio(audio, volume, time);
    }
    public AudioStreamPlayer PlayAudio(AudioStream audio, float volume = 1, float time = 0)
    {
        if (audio == null)
            return null;
        AudioStreamPlayer player = new() { Stream = audio, VolumeLinear = volume};
        AddChild(player);
        if (time <= audio.GetLength())
        {
            player.Play(time);
        }
        else
        {
            player.Play(0);
        }
        player.Finished += player.QueueFree;
        return player;
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
