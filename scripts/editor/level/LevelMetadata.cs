using Godot;
using System;

public partial class LevelMetadata : Control
{
	[Export] LineEdit LevelNameInput;
	[Export] LineEdit BGImageInput;
	[Export] LineEdit HealthInput;
	[Export] LineEdit DurationInput;
	[Export] OptionButton AspectRatioInput;
	[Export] Button SaveLevel;
	[Export] Button OpenLevelFolder;
	[Export] MessageDisplay ErrorDisplay;
	[Signal] public delegate void AspectRatioChangedEventHandler(Vector2I aspectRatio);
	[Signal] public delegate void DurationChangedEventHandler(float newDuration);
	[Signal] public delegate void BgImageChangedEventHandler(string to);
	[Signal] public delegate void SaveLevelRequestedEventHandler();
	Editor e;

	public override void _Ready()
	{
        base._Ready();
		LevelNameInput.TextChanged	+= OnLevelNameSubmit;
		HealthInput.TextChanged   	+= OnHealthSubmit;
		DurationInput.TextChanged 	+= OnDurationSubmit;
		AspectRatioInput.ItemSelected += OnAspectSelect;
		OpenLevelFolder.Pressed		+= OnLevelFolderOpen;
		SaveLevel.Pressed			+= OnSavePressed;
		BGImageInput.TextChanged	+= OnBgImageChanged;
		e = GetNode<Editor>("/root/Editor");
	}
	private void OnBgImageChanged(string text)
	{
		Texture2D found = RenderingUtils.LoadTexture(e.LevelPath+"images/",text);
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
		var path = ProjectSettings.GlobalizePath(e.LevelPath);;
		OS.ShellOpen(path);
	}
    private void OnAspectSelect(long i)
	{
		e.levelData.AspectRatio = AspectRatioInput.Selected; 
		EmitSignal(SignalName.AspectRatioChanged, PlayingField.Resolutions[i]);
	}
	private void OnDurationSubmit(string text)
	{
		if (float.TryParse(text, out float duration) && duration > 0) 
		{
			e.levelData.Duration = duration;
			EmitSignal(SignalName.DurationChanged, duration);
			ErrorDisplay.ClearMessage("Duration");			
		}
		else ErrorDisplay.SetMessage("Duration", $"[Duration] Must be a number and greater than 0.");
	}
	private void OnHealthSubmit(string text)
	{
		if (int.TryParse(text, out int health) && health > 0)
		{
			e.levelData.Health = health;
			ErrorDisplay.ClearMessage("Health");			
		}
		else 
			ErrorDisplay.SetMessage("Health", $"[Health] Must be an integer and greater than 0.");
	}
	private void OnLevelNameSubmit(string text) =>
		e.levelData.DisplayName = text;
	public void Load(LevelData data)
	{
		AspectRatioInput.Selected = data.AspectRatio;
		HealthInput.Text = data.Health.ToString();
		DurationInput.Text = data.Duration.ToString();
		BGImageInput.Text = data.BgImage;
		LevelNameInput.Text = data.DisplayName;
	}
}
