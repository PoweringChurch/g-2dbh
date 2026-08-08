using Godot;
using System;
using Godot.Collections;
using System.Linq;
public partial class AudioUtils : Node
{
    private static AudioUtils Instance;
    public static float SFXVolume => ConfigHelper.Current.SoundFXVolume*AudioVolumeMultipler;
    public static float MusicVolume => ConfigHelper.Current.MusicVolume*AudioVolumeMultipler;
    public const float AudioVolumeMultipler = 0.5f;

    public const string gameSFXFolderPath = "res://data/sounds/game/";

    private static readonly string[] first = [string.Empty];
    public override void _EnterTree()
    {
        Instance = this;
    }
    public static AudioStreamPlayer PlayAudio(string audioPath, float volume = 1, float delay = 0, float time = 0)
    {
        Console.LogMinor($"[AudioUtils] Playing audio '{audioPath}'");
        return PlayAudio(LoadAudio(audioPath), volume, delay, time);
    }
    private const float RateLimit = 1f / 20f;
    private static readonly Dictionary<AudioStream, float> _lastPlayTimes = [];
    public static AudioStreamPlayer PlayAudio(AudioStream audio, float volume = 1, float delay = 0, float time = 0)
    {
        if (audio == null)
            return null;
        // rate limit
        float currentTime = Time.GetTicksMsec() / 1000f;
        if (_lastPlayTimes.TryGetValue(audio, out float lastPlayTime) && (currentTime - lastPlayTime < RateLimit))
            return null;
        _lastPlayTimes[audio] = currentTime;

        // create player
        AudioStreamPlayer player = new() { Stream = audio, VolumeLinear = volume};
        Instance.AddChild(player);
        time = (float)Math.Clamp(time, 0, audio.GetLength());

        // play audio
        if (delay <= 0)
            player.Play(time);
        else
            Instance.GetTree().CreateTimer(delay).Timeout += () =>
            {
                if (IsInstanceValid(player) && player.IsInsideTree())
                    player.Play(time);
            };
        player.Finished += player.QueueFree;
        return player;
    }
    private static readonly Dictionary<string, AudioStream> audioCache = [];
    public static AudioStream LoadAudio(string path)
    {
        if (audioCache.TryGetValue(path, out var cached))
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
            audioCache[path] = stream;
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
    // game sfx
    private static Dictionary<string, string> gameSFXPaths;
    public static Dictionary<string, string> GameSFXPaths 
    {
        get
        {
            gameSFXPaths ??= GD.Load<GameSFXTable>(gameSfxPathsPath).Paths;
            return gameSFXPaths;
        }
    }
    private const string gameSfxPathsPath = "res://data/sounds/game/gameSFXNames.tres";
    private static string[] gameSfxNames;
    public static string[] GetGameSFXNames()
    {
        if (gameSfxNames != null)
            return gameSfxNames;
        string[] names = [..GameSFXPaths.Keys];
        gameSfxNames = names;
        return gameSfxNames;
    }
    public static void BuildGameSFXTable()
    {
        if (!OS.IsDebugBuild())
        {
            Console.LogErr("[AudioUtils] Cannot get build game sfx table in a release build");
            return;
        }
        Dictionary<string, string> table = [];
        var paths = GetGameSFXPathsFromSource();
        for (int i = 0; i < paths.Length; i++)
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(paths[i]);
            table[name] = paths[i];
            Console.LogInfo($"({i}/{paths.Length}) Associated '{name}' to path '{paths[i]}'");
        }
        GameSFXTable toSave = new() {Paths = table};
        ResourceSaver.Save(toSave, gameSfxPathsPath);
        Console.LogDebug($"Saved game SFX Table to {gameSfxPathsPath}");
    }
    private static string[] GetGameSFXPathsFromSource()
    {
        if (!OS.IsDebugBuild())
        {
            Console.LogErr("[AudioUtils] Cannot get audio names from source in a release build");
            return null;
        }
        System.Collections.Generic.List<string> names = [];
        var dir = DirAccess.Open(gameSFXFolderPath);
        dir.ListDirBegin();
        string entry = dir.GetNext();
        while (entry != "")
        {
            if (entry.EndsWith(".wav"))
                names.Add(gameSFXFolderPath+entry);
            entry = dir.GetNext();
        }
        dir.ListDirEnd();
        names.Sort(StringComparer.Ordinal);
        gameSfxNames = [..names];
        return gameSfxNames;
    }
}
