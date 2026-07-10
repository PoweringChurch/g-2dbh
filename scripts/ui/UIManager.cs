using Godot;
using System;

public partial class UIManager : Node
{
    [Export] NodePath HUDPath = "/root/main/HUD";
    [Export] NodePath PauseMenuPath = "/root/main/PauseMenu";
    [Export] NodePath MainMenuPath = "/root/main/MainMenu";
    [Export] NodePath LevelSelectPath = "/root/main/LevelSelect";
    [Export] NodePath CustomLevelSelectPath = "/root/main/CustomLevelSelect";
    [Export] NodePath EditorLayerPath = "/root/main/EditorLayer";
    [Export] NodePath ScoreSummaryPath = "/root/main/ScoreSummary";
    [Export] NodePath SettingsPath = "/root/main/Settings";

    public HUD HUD { get; private set; }
    public ScoreSummary ScoreSummary { get; private set;}
    SettingsMenu _settings;
    PauseMenu _pause;
    MainMenu _mainMenu;
    LevelSelect _levelSelect;
    CanvasLayer _editorLayer;
    CustomLevelSelect _customLevelSelect;
    GameSession gameSession;
    private bool _canPause = false;
    private bool inCustoms;
    public bool InCustoms => inCustoms;
    public override void _Ready()
    {
        HUD = GetNode<HUD>(HUDPath);
        ScoreSummary = GetNode<ScoreSummary>(ScoreSummaryPath);
        _settings = GetNode<SettingsMenu>(SettingsPath);
        _pause = GetNode<PauseMenu>(PauseMenuPath);
        _mainMenu = GetNode<MainMenu>(MainMenuPath);
        _levelSelect = GetNode<LevelSelect>(LevelSelectPath);
        _customLevelSelect = GetNode<CustomLevelSelect>(CustomLevelSelectPath);
        _editorLayer = GetNode<CanvasLayer>(EditorLayerPath);
        gameSession = GetNode<GameSession>("/root/GameSession");

        _pause.ResumeRequested += OnResume;
        _pause.ResetRequested += OnReset;
        _pause.QuitRequested += OnPauseQuit;

        ScoreSummary.ResetRequested += OnReset;
        ScoreSummary.QuitRequested  += OnPauseQuit;

        _mainMenu.StartRequested += ShowStart;
        _mainMenu.CustomsRequested += ShowCustoms;
        _mainMenu.QuitRequested += OnQuit;
        _mainMenu.SettingsRequested += OnSettings;
        ShowMainMenu();
    }
    public override void _Input(InputEvent e)
    {
        if (e.IsActionPressed("pause") && _canPause)
        {
            TogglePause(true);
        }
    }
    public void ShowEditor()
    {
        SetVisible(_editorLayer);
        _pause.DisableReset(true);
        _canPause = true;
    }
    public void ShowMainMenu() => SetVisible(_mainMenu);
    public void ShowHUD() 
    {
        SetVisible(HUD);
        _pause.DisableReset(false);
        _canPause = true;
    }
    public void ShowScoreSummary() => SetVisible(ScoreSummary);
    public void TogglePause(bool on)
    {
        if (!on)
        {
            gameSession.GAP.Play((float)gameSession.Elapsed);
        }
        else
        {
            gameSession.GAP.Stop();
        }

        GetTree().Paused = on;
        _pause.Visible = on;
    }
    public void OnSettings()
    {
        ToggleSettings(true);
    }
    public void ToggleSettings(bool to)
    {
        GetTree().Paused = to;
        _settings.Visible = to;
    }
    void SetVisible(CanvasLayer show)
    {
        foreach (var layer in new CanvasLayer[] { HUD, _settings, _pause, _mainMenu, _levelSelect, _customLevelSelect, _editorLayer, ScoreSummary })
            layer.Visible = layer == show;
        _canPause = false; // assume that whatever were switching to cant pause
    }
    void OnResume() => TogglePause(false);
    public void ShowStart()
    {
        _levelSelect.PopulateList();
        SetVisible(_levelSelect);
        inCustoms = false;
    }
    public void ShowCustoms() 
    {
        _customLevelSelect.PopulateList();
        SetVisible(_customLevelSelect);
        inCustoms = true;
    }
    void OnPauseQuit() 
    { 
        SetVisible(inCustoms ? _customLevelSelect : _levelSelect); 
        if (inCustoms)
        {
            _customLevelSelect.PopulateList();
            Editor.Open = false;
        }
        else
            _levelSelect.PopulateList();
        TogglePause(false); 
        gameSession.Abort();
        GetNode<Editor>("/root/Editor").SetPlaying(false);
    }
    void OnQuit() => GetTree().Quit();
    void OnReset()
    {
        TogglePause(false);
        gameSession.ResetLevel();
    }
}