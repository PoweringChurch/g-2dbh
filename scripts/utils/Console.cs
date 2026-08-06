using System;
using Godot;
using System.Reflection;
public partial class Console : CanvasLayer
{
    
    private const string SavePath = "user://data/logs/";
    public static Console Inst;
    [Export] TextEdit _consoleText;
    [Export] Button _save;
    public override void _Ready()
    {
        Inst = this;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("console"))
        {
            Visible = !Visible;
        }
    }
    public void Log(object message, bool sync = true)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        string formattedMessage = $"({timestamp}) {message}\n";
        _consoleText.InsertTextAtCaret(formattedMessage);
        if (sync)
            GD.Print(message); 
    }
    public void LogErr(object message, bool sync = true)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        string formattedMessage = $"[{timestamp}] {message}\n";
        _consoleText.InsertTextAtCaret(formattedMessage);
        if (sync)
            GD.Print(message); 
    }
    private void OnSaveButtonPressed()
    {
        string fileName = $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        string fullPath = System.IO.Path.Combine(SavePath, fileName);

        // Godot's built-in file access handles 'user://' paths perfectly
        using var file = FileAccess.Open(fullPath, FileAccess.ModeFlags.Write);
        
        if (file != null)
        {
            file.StoreString(_consoleText.Text);
            Log($"Log successfully saved to: {fullPath}");
        }
        else
        {
            GD.PrintErr($"Failed to save log file. Error code: {FileAccess.GetOpenError()}");
        }
    }
}
