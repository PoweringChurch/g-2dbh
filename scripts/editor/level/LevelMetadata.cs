using Godot;
using System;
using System.Collections.Generic;

public partial class LevelMetadata : Control
{
    [Export] Control BackgroundHolder;
	[Export] LineEdit LevelNameInput;
	[Export] LineEdit AuthorInput;
	[Export] LineEdit MusicInput;
	[Export] SpinBox HealthInput;
	[Export] SpinBox DurationInput; 
	[Export] OptionButton AspectRatioInput;
	[Export] OptionButton CharacterSelect;
	[Export] Button SaveLevel;
	[Export] Button OpenLevelFolder;
	[Export] Button OpenBgEditor;
	[Export] MessageDisplay ErrorDisplay;
	[Signal] public delegate void AspectRatioChangedEventHandler(Vector2I aspectRatio);
	[Signal] public delegate void DurationChangedEventHandler();
	[Signal] public delegate void MusicChangedEventHandler(AudioStream to);
	[Signal] public delegate void SaveLevelRequestedEventHandler();
	Editor e;
	[Export] BackgroundEditor BackgroundEditor;
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
		OpenBgEditor.Pressed += OnBgEditPressed;
		CharacterSelect.ItemSelected += OnCharacterSelected;
		e = GetNode<Editor>("/root/Editor");
	}

    private void OnCharacterSelected(long index)
	{
		e.levelData.Character = (int)index;
	}
    private void OnMusicTextChanged(string newSong)
	{
		var found = AudioUtils.LoadAudio($"{e.LevelPath}/audio/{newSong}");
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

    private void OnBgEditPressed()
	{
		BackgroundEditor.Visible = true;
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
		foreach (var child in BackgroundHolder.GetChildren())
			child.QueueFree();
		AuthorInput.Text = data.Author;
		AspectRatioInput.Selected = data.AspectRatio;
		CharacterSelect.Selected = data.Character;
		HealthInput.Value = data.Health;
		DurationInput.Value = data.Duration;
		MusicInput.Text = data.Music;
		LevelNameInput.Text = data.DisplayName;
		BackgroundHolder.Position = (Vector2)PlayingField.Resolutions[data.AspectRatio]/2*LevelPreview.PreviewScale;

		BackgroundEditor.Load(data);
	}
}
