using Godot;
using System;
using System.Collections.Generic;
using System.IO;
public partial class LevelMetadata : Control
{
	[ExportGroup("Song")]
	[Export] Button NextSong;
	[Export] Button PrevSong;
	[Export] Label SongNameLabel;
	[Export] Label SongAuthorLabel;

	[ExportGroup("Level Metadata")]
	[Export] LineEdit LevelNameInput;
	[Export] LineEdit AuthorInput;
	[Export] SpinBox DifficultyInput;
	[Export] SpinBox DurationInput;
	[Export] OptionButton AspectRatioInput;
	[Export] OptionButton CharacterSelect;

	[ExportGroup("Actions")]
	[Export] Button SaveLevel;
	[Export] Button OpenBgEditor;

	[ExportGroup("References")]
	[Export] MessageDisplay ErrorDisplay;
	[Export] BackgroundEditor BackgroundEditor;

	public event Action<Vector2I> AspectRatioChanged;
	public event Action<AudioStream> MusicChanged;
	public event Action DurationChanged;
	public event Action SaveLevelRequested;
	Editor e => Editor.Instance;
    public override void _Input(InputEvent @event)
    {
		if (Editor.CannotUseBinds()) return;
        if (@event.IsActionPressed("background_editor")) BackgroundEditor.Visible = !BackgroundEditor.Visible;
    }
	private int currentSong = 0;
	public override void _Ready()
	{
		base._Ready();
		LevelNameInput.TextChanged += (text) => e.levelData.Name = text;
		AuthorInput.TextChanged += (text) => e.levelData.Author = text;
		DifficultyInput.ValueChanged += (val) => e.levelData.Difficulty = (float)val;
		DurationInput.ValueChanged += (val) => {e.levelData.Duration = (float)val; DurationChanged.Invoke();};
		AspectRatioInput.ItemSelected += (idx) => { 
			e.levelData.AspectRatio = AspectRatioInput.Selected; AspectRatioChanged.Invoke(PlayingField.Resolutions[idx]); };
		OpenBgEditor.Pressed += () => BackgroundEditor.Visible = true;
		CharacterSelect.ItemSelected += (idx) => e.levelData.Character = (int)idx;
		SaveLevel.Pressed += OnSavePressed;

		NextSong.Pressed += GoNextSong;
		PrevSong.Pressed += GoPrevSong;
	}
	private void GoNextSong()
	{
		if (currentSong < LevelCompiler.SongData.Length-1)
		{
			currentSong++;
			ApplyCurrentSong();
		}
	}
	private void GoPrevSong()
	{
		if (currentSong > 0)
		{
			currentSong--;
			ApplyCurrentSong();
		}
	}
	private void ApplyCurrentSong()
	{
		e.levelData.MusicId = currentSong;
		var info = LevelCompiler.SongData[currentSong];
		var stream = AudioUtils.LoadAudio(info.StreamPath);
		MusicChanged.Invoke(stream);
		SongNameLabel.Text = info.SongName;
		SongAuthorLabel.Text = info.Author; 
	}
	private void OnSavePressed()
	{
		ErrorDisplay.ClearMessage("Save");
		if (ErrorDisplay.MessageCount > 0)
		{
			ErrorDisplay.SetMessage("Save", "[Save] Cannot save with unresolved errors.");
			return;
		}
		SaveLevelRequested.Invoke();
	}
	public void Load(RawLevelData data)
	{
		AuthorInput.Text = data.Author;
		AspectRatioInput.Selected = data.AspectRatio;
		CharacterSelect.Selected = data.Character;
		DifficultyInput.Value = data.Difficulty;
		DurationInput.Value = data.Duration;
		LevelNameInput.Text = data.Name;
		BackgroundEditor.Load(data);
		currentSong = data.MusicId;
		ApplyCurrentSong();
	}
}
