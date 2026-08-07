using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public partial class LevelSelect : CanvasLayer
{
    [Export] public LevelList LevelList;
    [Export] public Button ReturnBtn;
    [Export] public Button NewLevelBtn;
    private Editor e => Editor.Instance;
    public event Action RequestReturn;
    public override void _Ready()
    {
        LevelList.LevelDisplay.RequestRepopulate += PopulateList;
        NewLevelBtn.Pressed += OnNewLevelPressed;
        ReturnBtn.Pressed += RequestReturn.Invoke;
    }

    // List
    public void PopulateList()
    {
        LevelList.LevelDisplay.ShowLevel(null);
        var saved = LevelStorage.GetSavedLevels();
        LevelList.ShowLevelList(saved);
    }
    
    protected void OnNewLevelPressed()
    {
        UIManager.Instance.ShowEditor();
        e.NewLevel();
        PopulateList();
    }
}