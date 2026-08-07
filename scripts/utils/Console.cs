using System;
using Godot;
using System.Reflection;
using System.Collections.Generic;
public partial class Console : CanvasLayer
{
    private const string savePath = "user://data/logs/";
    private static Console Inst;
    [Export] Container LabelHolder;
    [Export] Button Save;
    [Export] Button LogsFolder;
    private string consoleText = GetPcSpecsString();
    public override void _Ready()
    {
        Inst = this;
        Save.Pressed += OnSaveButtonPressed;
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        Log($"Started game, {timestamp} {TimeZoneInfo.Local.DisplayName}");
        
        LogsFolder.Pressed += () => OS.ShellOpen(ProjectSettings.GlobalizePath(savePath));
    }
    private static string GetPcSpecsString()
    {
        string osName = OS.GetName();
        string processorName = OS.GetProcessorName();
        int processorCount = OS.GetProcessorCount();
        string gpuName = RenderingServer.GetVideoAdapterName();
        
        var memoryInfo = OS.GetMemoryInfo();
        ulong totalMemoryMb = 0;
        if (memoryInfo.ContainsKey("physical"))
            totalMemoryMb = memoryInfo["physical"].AsUInt64() / 1024 / 1024;
        return  $"OS: {osName} ({OS.GetVersion()}), CPU: {processorName} ({processorCount} cores), GPU: {gpuName}, RAM: {totalMemoryMb}mb";
    }
    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("console"))
        {
            Visible = !Visible;
        }
    }
    public static void Log(object message) =>
        Inst.Log(message, Colors.White);
    public static void LogErr(object message) =>
        Inst.Log(message, Colors.PaleVioletRed);
    public static void LogSuccess(object message) =>
        Inst.Log(message, Colors.LightSeaGreen);
    public static void LogInfo(object message) =>
        Inst.Log(message, Colors.SkyBlue);
    public static void LogDebug(object message) =>
        Inst.Log(message, Colors.RebeccaPurple);
    private readonly Dictionary<Color, LabelSettings> labelSettingsCache = new();
    private void Log(object message, Color color)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        string formattedMessage = $"({timestamp}) {message}\n";
        var settings = new LabelSettings() {FontColor = color};
        labelSettingsCache[color] = settings;
        var label = new Label() { Text = formattedMessage, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new(100,0), AutowrapMode = TextServer.AutowrapMode.Arbitrary, LabelSettings = settings};
        LabelHolder.AddChild(label);
        consoleText += $" \n{formattedMessage}";
    }
    private void OnSaveButtonPressed()
    {
        string fileName = $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        string fullPath = System.IO.Path.Combine(savePath, fileName);

        using var file = FileAccess.Open(fullPath, FileAccess.ModeFlags.Write);
        if (file != null)
        {
            file.StoreString(consoleText);
            LogDebug($"Log successfully saved to: {fullPath}");
        }
        else
        {
            LogErr($"Failed to save log file. Error code: {FileAccess.GetOpenError()}");
        }
    }
}
