// LevelSelect.cs
using Godot;

public partial class CustomLevelSelect : LevelSelect
{
    [Export] protected Button _editButton;
    [Export] protected Button _deleteButton;
    [Export] protected Button _newLevelButton;
    [Export] protected Button _refreshButton;
    [Export] protected Label _author;
    private Editor e;
    public override void _Ready()
    {
        _gameSession = GetNode<GameSession>("/root/GameSession");
        e = GetNode<Editor>("/root/Editor");
        _levelDirectory = "user://data/levels/";

        _playButton.Pressed += OnPlayPressed;
        _playButton.Disabled = true;
        _returnButton.Pressed += OnReturnPressed;
        _editButton.Pressed += OnEditPressed;
        _editButton.Disabled = true;
        _deleteButton.Pressed += OnDeletePressed;
        _deleteButton.Disabled = true;
        _newLevelButton.Pressed += OnNewLevelPressed;
        _refreshButton.Pressed  += PopulateList;
    }
    // Play
    protected void OnEditPressed()
    {
        if (string.IsNullOrEmpty(_selectedLevel)) return;
        Hide();
        var ui = GetNode<UIManager>("/root/UIManager");
        ui.ShowEditor();
        bool success = e.OpenLevel(_selectedLevel);
        Editor.Open = true;
        if (!success)
        {
            Show();
            Popups.Instance.Show(Popups.DefaultType.OK, $"Something went wrong opening this level. \n Level Id : {_selectedLevel}");
        }
    }
    protected override void OnLevelSelected(LevelData levelData, Button pressed)
    {
        base.OnLevelSelected(levelData, pressed);
        _editButton.Disabled = false;
        _deleteButton.Disabled = false;
        _author.Text = levelData.Author;
    }
    protected override void ClearPreview()
    {
        base.ClearPreview();
        _editButton.Disabled = true;
        _deleteButton.Disabled = true;
    }
    protected void OnDeletePressed()
    {
        if (string.IsNullOrEmpty(_selectedLevel)) return;
        var levelDirectory = ProjectSettings.GlobalizePath($"{_levelDirectory}{_selectedLevel}");
        if (!DirAccess.DirExistsAbsolute(levelDirectory))
        {
            Console.Inst.LogErr($"[Editor Level Select] Could not find directory {levelDirectory}");
            return;
        }
        
        var popup = Popups.Instance.Show(Popups.DefaultType.YN, "Are you sure you want to delete this level?");
        popup.CloseOnPress = true;
        popup.Options[0].Pressed += () => 
        {
            using var dir = DirAccess.Open(levelDirectory);
            if (DirAccess.DirExistsAbsolute(levelDirectory))
            {
                DeleteDirectoryRecursive(levelDirectory);
            }
            DirAccess.RemoveAbsolute(levelDirectory);
            PopulateList();
            ClearPreview();
        };
    }
    private void DeleteDirectoryRecursive(string path)
    {
        using var dir = DirAccess.Open(path);
        if (dir == null) return;
        dir.ListDirBegin();
        string fileName = dir.GetNext();
        while (fileName != "")
        {
            if (fileName != "." && fileName != "..")
            {
                string fullPath = $"{path}/{fileName}";

                if (dir.CurrentIsDir())
                    DeleteDirectoryRecursive(fullPath);
                else
                    DirAccess.RemoveAbsolute(fullPath);
            }
            fileName = dir.GetNext();
        }

        dir.ListDirEnd();
        DirAccess.RemoveAbsolute(path);
        PopulateList();
    }
    protected void OnNewLevelPressed()
    {
        var ui = GetNode<UIManager>("/root/UIManager");
        ui.ShowEditor();
        e.NewLevel();
        PopulateList();
    }
}