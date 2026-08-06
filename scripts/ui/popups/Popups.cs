using Godot;

public partial class Popups : Node
{
    [Export] public PackedScene popupScene;
    public static Popups Instance;
    public enum DefaultType
    {
        OK,
        YN,
        OKCancel
    }
    public override void _Ready() =>
        Instance = this;
    private static string[] GetLabelsFor(DefaultType type) => type switch
    {
        DefaultType.OK       => ["OK"],
        DefaultType.YN       => ["Yes", "No"],
        DefaultType.OKCancel => ["OK", "Cancel"],
        _ => []
    };
    public Popup Show(string[] optionLabels, string message = "")
    {
        var popup = Create(optionLabels, message);
        AddChild(popup);
        return popup;
    }
    public Popup Show(DefaultType type, string message = "")
    {
        var popup = Create(type, message);
        AddChild(popup);
        return popup;
    }
    private Popup Create(DefaultType type, string message = "", bool CloseOnPress = true) =>
        Create(GetLabelsFor(type), message, CloseOnPress);
    private Popup Create(string[] optionLabels, string message = "", bool CloseOnPress = true)
    {
        var popup = popupScene.Instantiate<Popup>();
        popup.CloseOnPress = CloseOnPress;
        popup.SetMessage(message);
        popup.SetOptions(optionLabels);
        return popup;
    }
}