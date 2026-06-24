using Godot;
using System;

public partial class UIManager : Node
{
    [Export] NodePath HUDPath = "/root/main/HUD";
    [Export] NodePath PauseMenuPath = "/root/main/PauseMenu";
    [Export] NodePath GameOverPath = "/root/main/GameOver";
    [Export] NodePath MainMenuPath = "/root/main/MainMenu";
    [Export] NodePath LevelSelectPath = "/root/main/LevelSelect";
    [Export] NodePath CustomLevelSelectPath = "/root/main/CustomLevelSelect";
    [Export] NodePath EditorLayerPath = "/root/main/EditorLayer";
    [Export] NodePath ScoreSummaryPath = "/root/main/ScoreSummary";

    public HUD HUD { get; private set; }
    public ScoreSummary ScoreSummary { get; private set;}
    PauseMenu _pause;
    GameOver _gameOver;
    MainMenu _mainMenu;
    LevelSelect _levelSelect;
    CanvasLayer _editorLayer;
    CustomLevelSelect _customLevelSelect;
    GameSession gameSession;
    private bool inCustoms;
    public bool InCustoms => inCustoms;
    public override void _Ready()
    {
        HUD = GetNode<HUD>(HUDPath);
        ScoreSummary = GetNode<ScoreSummary>(ScoreSummaryPath);
        _pause = GetNode<PauseMenu>(PauseMenuPath);
        _gameOver = GetNode<GameOver>(GameOverPath);
        _mainMenu = GetNode<MainMenu>(MainMenuPath);
        _levelSelect = GetNode<LevelSelect>(LevelSelectPath);
        _customLevelSelect = GetNode<CustomLevelSelect>(CustomLevelSelectPath);
        _editorLayer = GetNode<CanvasLayer>(EditorLayerPath);
        gameSession = GetNode<GameSession>("/root/GameSession");

        _pause.ResumeRequested += OnResume;
        _pause.ResetRequested += OnReset;
        _pause.QuitRequested += OnPauseQuit;

        _gameOver.ResetRequested += OnReset;
        _gameOver.QuitRequested += OnPauseQuit;

        ScoreSummary.ResetRequested += OnReset;
        ScoreSummary.QuitRequested  += OnPauseQuit;

        _mainMenu.StartRequested += OnStart;
        _mainMenu.CustomsRequested += OnCustoms;
        _mainMenu.QuitRequested += OnQuit;
        ShowMainMenu();
    }

    public override void _Input(InputEvent e)
    {
        if (e.IsActionPressed("pause"))
            TogglePause(true);
    }
    public void ShowEditor() => SetVisible(_editorLayer);
    public void ShowMainMenu() => SetVisible(_mainMenu);
    public void ShowHUD() => SetVisible(HUD);
    public void ShowGameOver() => SetVisible(_gameOver);
    public void ShowScoreSummary() => SetVisible(ScoreSummary);
    public void ShowEditorSelect() => SetVisible(_customLevelSelect);
    public void TogglePause(bool to)
    {
        GD.Print("toggle paused set to : " + to);
        GetTree().Paused = to;
        _pause.Visible = to;
    }
    void SetVisible(CanvasLayer show)
    {
        foreach (var layer in new CanvasLayer[] { HUD, _pause, _gameOver, _mainMenu, _levelSelect, _customLevelSelect, _editorLayer, ScoreSummary })
            layer.Visible = layer == show;
    }
    void OnResume() => TogglePause(false);
    void OnStart()
    {
        SetVisible(_levelSelect);
        inCustoms = false;
    }
    void OnCustoms() 
    {
        SetVisible(_customLevelSelect);
        inCustoms = true;
    }
    void OnPauseQuit() 
    { 
        SetVisible(inCustoms ? _customLevelSelect : _levelSelect); 
        if (inCustoms)
            _customLevelSelect.PopulateList();
        else
            _levelSelect.PopulateList();
        TogglePause(false); 
        gameSession.StopLevel(); 
    }
    void OnQuit() => GetTree().Quit();
    void OnReset()
    {
        TogglePause(false);
        gameSession.ResetLevel();
    }
}