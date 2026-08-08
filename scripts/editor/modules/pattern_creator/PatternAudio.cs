using Godot;

public partial class PatternAudio : Control
{
    public PatternModel Model;
    [Export] Label SoundPreviewLabel;
    [Export] Container SoundButtonContainer;
    [Export] Slider VolumeSlider;
    [Export] PackedScene AudioButtonScene;
    public override void _Ready()
    {
        VolumeSlider.ValueChanged += (v) => Model.SoundVolume = (float)v;

        var noneButton = AudioButtonScene.Instantiate<Button>();
        noneButton.GetNode<Label>("Sort/Label").Text = "none";

        noneButton.Pressed += () =>
        {
            Model.SFXName = "none";
            SoundPreviewLabel.Text = "none";
        };
        SoundButtonContainer.AddChild(noneButton);

        var names = AudioUtils.GetGameSFXNames();
        for (int i = 0; i < names.Length; i++)
        {
            var name = names[i];
            var path = AudioUtils.GameSFXPaths[name];

            var button = AudioButtonScene.Instantiate<Button>();
            button.GetNode<Label>("Sort/Label").Text = name;
            button.Pressed += () =>
            {
                AudioUtils.PlayAudio(path, (float)(AudioUtils.AudioVolumeMultipler*VolumeSlider.Value));
                Model.SFXName = name;
                SoundPreviewLabel.Text = name;
            };

            SoundButtonContainer.AddChild(button);
        }
    }
    public void Load(PatternModel newModel)
    {
        Model = newModel;
        SoundPreviewLabel.Text = Model.SFXName;
        VolumeSlider.Value = Model.SoundVolume;
    }
}