using Godot;
using System.Collections.Generic;
using System.Linq;
public partial class AudioUtils : Node
{
    public static float SFXVolume => ConfigHelper.Current.SoundFXVolume*AudioVolumeMultipler;
    public static float MusicVolume => ConfigHelper.Current.MusicVolume*AudioVolumeMultipler;
    public const float AudioVolumeMultipler = 0.5f;
    private static readonly string[] first = [string.Empty];
    public static AudioUtils Instance { get; private set; }
    public override void _Ready()
    {
        Instance = this;
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
    public static AudioStream LoadAudio(string path)
    {
        if (_audioCache.TryGetValue(path, out var cached))
            return cached;
        foreach (string ext in first.Concat(ValidExtensions.Audio))
        {
            string testPath = $"{path}{ext}";
            AudioStream stream = null;
            if (ResourceLoader.Exists(testPath))
            {
                stream = ResourceLoader.Load<AudioStream>(testPath);
            }
            else if (FileAccess.FileExists(testPath))
            {
                stream = LoadAudioFromFileSystem(testPath, ext.ToLowerInvariant());
            }
            if (stream == null)
                continue;
            _audioCache[path] = stream;
            return stream;
        }
        return null;
    }
    private static AudioStream LoadAudioFromFileSystem(string path, string ext)
    {
        return ext switch
        {
            ".wav" => new AudioStreamWav { Data = FileAccess.GetFileAsBytes(path) },
            ".mp3" => new AudioStreamMP3 { Data = FileAccess.GetFileAsBytes(path) },
            ".ogg" => AudioStreamOggVorbis.LoadFromFile(path),
            _ => null
        };
    }
}
