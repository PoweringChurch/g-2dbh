using Godot;
using System;

public partial class LevelMetadata : Control
{
	[Export] LineEdit LevelNameInput;
	[Export] LineEdit AuthorInput;
	[Export] LineEdit BGImageInput;
	[Export] LineEdit MusicInput;
	[Export] SpinBox HealthInput;
	[Export] SpinBox DurationInput;
	[Export] OptionButton AspectRatioInput;
	[Export] Button SaveLevel;
	[Export] Button OpenLevelFolder;
	[Export] MessageDisplay ErrorDisplay;
	[Signal] public delegate void AspectRatioChangedEventHandler(Vector2I aspectRatio);
	[Signal] public delegate void DurationChangedEventHandler();
	[Signal] public delegate void BgImageChangedEventHandler(string to);
	[Signal] public delegate void MusicChangedEventHandler(AudioStream to);
	[Signal] public delegate void SaveLevelRequestedEventHandler();
	Editor e;

	public override void _Ready()
	{
		base._Ready();
		LevelNameInput.TextChanged += OnLevelNameSubmit;
		AuthorInput.TextChanged += OnAuthorChanged;
		HealthInput.ValueChanged += OnHealthChanged;
		DurationInput.ValueChanged += OnDurationChanged;
		MusicInput.TextChanged += OnMusicTextChanged;
		AspectRatioInput.ItemSelected += OnAspectSelect;
		OpenLevelFolder.Pressed += OnLevelFolderOpen;
		SaveLevel.Pressed += OnSavePressed;
		BGImageInput.TextChanged += OnBgImageChanged;
		e = GetNode<Editor>("/root/Editor");
	}

    private void OnMusicTextChanged(string newSong)
	{
		var found = AudioUtils.LoadAudio(e.LevelPath + "audio/", newSong);
		if (found != null && newSong == "none")
		{
			ErrorDisplay.SetMessage("Music", $"[Music] 'none' is a reserved name, please rename this audio file.");
			return;
		}
		if (found != null || newSong == "none")
		{
			e.levelData.Music = newSong;
			EmitSignal(SignalName.MusicChanged, found);
			ErrorDisplay.ClearMessage("Music");
		}
		else ErrorDisplay.SetMessage("Music", $"[Music] Could not find audio of name {newSong} in audio folder.");
	}

    private void OnBgImageChanged(string text)
	{
		Texture2D found = RenderingUtils.LoadTexture(e.LevelPath + "images/", text);
		if (found != null && text == "none")
		{
			ErrorDisplay.SetMessage("Background", $"[BG Image] 'none' is a reserved name, please rename this image file.");
			return;
		}
		if (found != null || text == "none")
		{
			e.levelData.BgImage = text;
			EmitSignal(SignalName.BgImageChanged, text);
			ErrorDisplay.ClearMessage("Background");
		}
		else ErrorDisplay.SetMessage("Background", $"[BG Image] Could not find image of name {text} in images folder.");
	}
	private void OnSavePressed()
	{
		ErrorDisplay.ClearMessage("Save");
		if (ErrorDisplay.MessageCount > 0)
		{
			ErrorDisplay.SetMessage("Save", "[Save] Cannot save with unresolved errors.");
			return;
		}
		EmitSignal(SignalName.SaveLevelRequested);
	}
	private void OnLevelFolderOpen()
	{
		var path = ProjectSettings.GlobalizePath(e.LevelPath); ;
		OS.ShellOpen(path);
	}
	private void OnAspectSelect(long i)
	{
		e.levelData.AspectRatio = AspectRatioInput.Selected;
		EmitSignal(SignalName.AspectRatioChanged, PlayingField.Resolutions[i]);
	}
	private void OnDurationChanged(double val)
	{
		e.levelData.Duration = (float)val;
		EmitSignal(SignalName.DurationChanged);
	}
	private void OnHealthChanged(double val)
	{
		e.levelData.Health = (int)val;
	}
	private void OnAuthorChanged(string text)
	{
		e.levelData.Author = text;
	}
	private void OnLevelNameSubmit(string text) =>
		e.levelData.DisplayName = text;
	public void Load(LevelData data)
	{
		AuthorInput.Text = data.Author;
		AspectRatioInput.Selected = data.AspectRatio;
		HealthInput.Value = data.Health;
		DurationInput.Value = data.Duration;
		BGImageInput.Text = data.BgImage;
		MusicInput.Text = data.Music;
		LevelNameInput.Text = data.DisplayName;
	}
}
