using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class UIManager : Node
{
    public static UIManager Instance;
    [Export] public HUD HUD { get; private set; }
    [Export] ScoreSummary scoreSummary;
    [Export] SettingsMenu _settings;
    [Export] PauseMenu _pause;
    [Export] MainMenu _mainMenu;
    [Export] LevelSelect _levelSelect;
    [Export] Campaign _campaign;
    [Export] DialogueHandler dialogueHandler;
    GameSession gs => GameSession.Instance;
    Editor e => Editor.Instance;
    private bool canPause = false;
    private List<CanvasLayer> uiPath = new();
    public override void _EnterTree() =>
        Instance = this;
    public override void _Ready()
    {
        _pause.RequestResume += () => TogglePause(false);
        _pause.RequestReset += Reset;
        _pause.RequestReturn += Return;

        scoreSummary.RequestReset += Reset;
        scoreSummary.RequestReturn  += Return;

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
        if (e.IsActionPressed("return"))
        {
            if (canPause)
                TogglePause(true);
            else if (uiPath.Count > 1)
                Return();
        }
    }
    public void ShowEditor() => Open(e);
    public void ShowHUD() => Open(HUD);
    public void ShowScoreSummary(float completion, int graze, int hp, string levelName, string authorName)
    {
        scoreSummary.AnimateScoreSummary(completion, graze, hp, levelName, authorName);
        Open(scoreSummary, false);
    }
    public void ResetScoreSummary() => scoreSummary.ResetHolders();
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
    private void Open(CanvasLayer show, bool addToPath = true)
    {
        foreach (var layer in new CanvasLayer[] { HUD, _settings, _pause, _mainMenu, _levelSelect, e, scoreSummary, _campaign })
        {
            layer.Visible = layer == show;
        }
        canPause = false;
        if (show == HUD)
        {
            _pause.ToggleReset(true);
            canPause = true;
        } else if (show == e)
        {
            _pause.ToggleReset(false);
            canPause = true;
        } else if (show == _levelSelect)
        {
            _levelSelect.PopulateList();
        }
        if (addToPath)
            uiPath.Add(show);
    }
    void Return()
    {
        var current = uiPath[^1];
        // special behavior
        if (current == e)
        {
            e.SetPlaying(false);
            PlaylistHandler.Instance.FadeIn();
        } else if (current == scoreSummary && uiPath[^2] == HUD)
            uiPath.RemoveAt(uiPath.Count-2);
        // clear duplicates 
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
        
        dialogueHandler.CancelDialogue();
        TogglePause(false);

        uiPath.RemoveAt(uiPath.Count-1);
        Open(uiPath[^1], false);
    }
    void Reset()
    {
        TogglePause(false);
        gs.ResetLevel();
    }
}