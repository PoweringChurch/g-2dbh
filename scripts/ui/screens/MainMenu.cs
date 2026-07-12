using System;
using Godot;
public partial class MainMenu : CanvasLayer
{
    public event Action RequestCampaign;
    public event Action RequestLevelSelect;
    public event Action RequestSettings;
    public event Action RequestReturn;
    // references
    [Export] Button _campaignButton;
    [Export] Button _customsButton;
    [Export] Button _settingsButton;
    [Export] Button _quitButton;
    public override void _Ready()
    {
        _campaignButton.Pressed += RequestCampaign.Invoke;
        _customsButton.Pressed += RequestLevelSelect.Invoke;
        _settingsButton.Pressed  += RequestSettings.Invoke;
        _quitButton.Pressed   += RequestReturn.Invoke;
    }
}