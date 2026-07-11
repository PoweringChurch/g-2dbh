using System;
using Godot;

public partial class PlaylistHandler : Node
{
    public static PlaylistHandler Instance;
    [Export] AudioStream[] tracklist;
    [Export] AudioStreamPlayer player;
    public int currentSong = 0;
    public float position = 0;
    private RandomNumberGenerator _rng = new();
    private Tween _fadeTween;
    public override void _Ready()
    {
        Instance = this;
        _rng.Randomize();
        player.Finished += OnTrackFinished;

        ShuffleTracklist();
        PlayCurrent();
    }
    private float __lastvol = 0;
    private void OnTrackFinished()
    {
        currentSong++;
        if (currentSong >= tracklist.Length)
        {
            AudioStream lastTrack = tracklist[tracklist.Length - 1];
            ShuffleTracklist(lastTrack);
            currentSong = 0;
        }
        PlayCurrent();
    }
    public override void _Process(double delta)
    {
        if (player.Playing)
        {
            position += (float)delta;
            float musicVolume = ConfigHelper.Current.MusicVolume*AudioUtils.MusicVolumeMultiplier;
            if (__lastvol != ConfigHelper.Current.MusicVolume)
            {
                __lastvol = ConfigHelper.Current.MusicVolume;
                player.VolumeLinear = musicVolume;
            }
        }
    }
    public void FadeIn(float duration = 1.0f)
    {
        player.Play(position);
        float musicVolume = ConfigHelper.Current.MusicVolume * AudioUtils.MusicVolumeMultiplier;
        _fadeTween?.Kill();
        _fadeTween = CreateTween();
        _fadeTween.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        _fadeTween.TweenProperty(player, "volume_linear", musicVolume, duration)
            .From(player.VolumeLinear);
    }
    public void FadeOut(float duration = 1.0f)
    {
        _fadeTween?.Kill();
        _fadeTween = CreateTween();
        _fadeTween.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
        _fadeTween.TweenProperty(player, "volume_linear", 0f, duration)
            .From(player.VolumeLinear);
        _fadeTween.TweenCallback(Callable.From(player.Stop));
    }
    private void PlayCurrent()
    {
        player.Stream = tracklist[currentSong];
        player.VolumeLinear = ConfigHelper.Current.MusicVolume*AudioUtils.MusicVolumeMultiplier;
        position = 0;
        player.Play();
    }
    private void ShuffleTracklist(AudioStream avoidFirst = null)
    {
        for (int i = tracklist.Length - 1; i > 0; i--)
        {
            int j = _rng.RandiRange(0, i);
            (tracklist[i], tracklist[j]) = (tracklist[j], tracklist[i]);
        }
        if (avoidFirst != null && tracklist.Length > 1 && tracklist[0] == avoidFirst)
        {
            int swapIndex = _rng.RandiRange(1, tracklist.Length - 1);
            (tracklist[0], tracklist[swapIndex]) = (tracklist[swapIndex], tracklist[0]);
        }
    }
}