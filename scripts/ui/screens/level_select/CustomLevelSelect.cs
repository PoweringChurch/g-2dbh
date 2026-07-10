// LevelSelect.cs
using Godot;

public partial class CustomLevelSelect : LevelSelect
{
    [Export] protected Button _newLevelButton;
    [Export] protected Button _refreshButton;
    [Export] protected Button _openLevelsFolder;
    private Editor e;
    public override void _Ready()
    {
        e = GetNode<Editor>("/root/Editor");
        _levelDirectory = "user://data/levels/";

        _returnButton.Pressed += OnReturnPressed;
        _newLevelButton.Pressed += OnNewLevelPressed;
        _refreshButton.Pressed  += PopulateList;
        _levelDisplay.Repopulate += PopulateList;
        _openLevelsFolder.Pressed += OpenLevelsFolder;
    }
    private void OpenLevelsFolder()
    {
        var path = ProjectSettings.GlobalizePath(_levelDirectory);
		OS.ShellOpen(path);
    }
    protected void OnNewLevelPressed()
    {
        var ui = GetNode<UIManager>("/root/UIManager");
        ui.ShowEditor();
        e.NewLevel();
        PopulateList();
    }
}