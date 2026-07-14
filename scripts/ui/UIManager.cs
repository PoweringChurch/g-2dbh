using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class UIManager : Node
{
    [Export] NodePath HUDPath = "/root/main/HUD";
    [Export] NodePath PauseMenuPath = "/root/main/PauseMenu";
    [Export] NodePath MainMenuPath = "/root/main/MainMenu";
    [Export] NodePath LevelSelectPath = "/root/main/LevelSelect";
    [Export] NodePath EditorLayerPath = "/root/main/EditorLayer";
    [Export] NodePath ScoreSummaryPath = "/root/main/ScoreSummary";
    [Export] NodePath SettingsPath = "/root/main/Settings";
    [Export] NodePath CampaignPath = "/root/main/Campaign";
    public HUD HUD { get; private set; }
    public ScoreSummary ScoreSummary { get; private set;}
    SettingsMenu _settings;
    PauseMenu _pause;
    MainMenu _mainMenu;
    LevelSelect _levelSelect;
    CanvasLayer _editorLayer;
    Campaign _campaign;
    GameSession gs;
    Editor e;
    private bool _canPause = false;
    private List<CanvasLayer> uiPath = new();
    public override void _Ready()
    {
        gs = GetNode<GameSession>("/root/GameSession");
        e = GetNode<Editor>("/root/Editor");
        HUD = GetNode<HUD>(HUDPath);
        ScoreSummary = GetNode<ScoreSummary>(ScoreSummaryPath);
        _settings = GetNode<SettingsMenu>(SettingsPath);
        _pause = GetNode<PauseMenu>(PauseMenuPath);
        _mainMenu = GetNode<MainMenu>(MainMenuPath);
        _levelSelect = GetNode<LevelSelect>(LevelSelectPath);
        _editorLayer = GetNode<CanvasLayer>(EditorLayerPath);
        _campaign = GetNode<Campaign>(CampaignPath);

        _pause.RequestResume += () => TogglePause(false);
        _pause.RequestReset += Reset;
        _pause.RequestReturn += Return;

        ScoreSummary.RequestReset += Reset;
        ScoreSummary.RequestReturn  += Return;

        _mainMenu.RequestCampaign += () => Open(_campaign);
        _mainMenu.RequestLevelSelect += () => Open(_levelSelect);
        _mainMenu.RequestSettings += () => Open(_settings);
        _mainMenu.RequestReturn += Return;

        _settings.RequestReturn += Return;
        _campaign.RequestReturn += Return;
        _levelSelect.RequestReturn += Return;
        Open(_mainMenu);
    }
    public override void _Input(InputEvent e)
    {
        if (e.IsActionPressed("pause"))
        {
            if (_canPause)
                TogglePause(true);
            else
                Return();
        }
    }
    public void ShowEditor() => Open(_editorLayer);
    public void ShowHUD() => Open(HUD);
    private void TogglePause(bool on)
    {
        if (!on) // when unpausing
        {
            if (gs.Running)
            {
                Input.MouseMode = Input.MouseModeEnum.ConfinedHidden;
                gs.GAP.Play((float)gs.Elapsed);
            }
        }
        else
        {
            if (gs.Running)
            {
                Input.MouseMode = Input.MouseModeEnum.Visible;
                gs.GAP.Stop();
            }
        }
        GetTree().Paused = on;
        _pause.Visible = on;
    }
    void Open(CanvasLayer show, bool addToPath = true)
    {
        foreach (var layer in new CanvasLayer[] { HUD, _settings, _pause, _mainMenu, _levelSelect, _editorLayer, ScoreSummary, _campaign })
            layer.Visible = layer == show;
        _canPause = false;
        if (show == HUD)
        {
            _pause.ToggleReset(true);
            _canPause = true;
        } else if (show == _editorLayer)
        {
            _pause.ToggleReset(false);
            _canPause = true;
        } else if (show == _levelSelect)
        {
            _levelSelect.PopulateList();
        }
        if (addToPath)
            uiPath.Add(show);
    }
    void Return()
    {
        if (uiPath[^1] == _editorLayer)
        {
            e.SetPlaying(false);
        }
        if (uiPath.Count > 1)
            for (int i = uiPath.Count-1; i > 0; i--)
                if (uiPath[i] == uiPath[i-1])
                    uiPath.RemoveAt(i);
                else
                    break;
        else
        {
            GetTree().Quit();
            return;
        }
        if (gs.Running)
            gs.Abort();
        TogglePause(false);
        ScoreSummary.Visible = false;
        uiPath.RemoveAt(uiPath.Count-1);
        Open(uiPath[^1], false);
    }
    void Reset()
    {
        TogglePause(false);
        gs.ResetLevel();
    }
}