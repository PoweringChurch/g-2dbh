using Godot;
using System;
using System.Reflection;

public partial class SettingsMenu : CanvasLayer
{
    [Export] public Button _saveButton;
    [Export] public Button _openDataButton;
    [Export] public Button _returnButton;
    [Export] public VBoxContainer _fieldContainer; // where generated rows go
    public event Action RequestReturn;
    private Config config => ConfigHelper.Current;
    private readonly System.Collections.Generic.List<(PropertyInfo Prop, Control Control)> _bindings = new();
    private UIManager _ui;
    public override void _Ready()
    {
        _ui = GetNode<UIManager>("/root/UIManager");

        BuildFields();

        _saveButton.Pressed += OnSavePressed;
        _openDataButton.Pressed += OnOpenDataPressed;
        _returnButton.Pressed += RequestReturn.Invoke;
        GetWindow().Mode = config.Fullscreen ? Window.ModeEnum.Fullscreen : Window.ModeEnum.Windowed;
    }

    private void BuildFields()
    {
        string lastCategory = null;

        foreach (PropertyInfo prop in typeof(Config).GetProperties())
        {
            var attr = prop.GetCustomAttribute<ConfigFieldAttribute>();
            if (attr == null) continue; // skip anything not meant for UI

            if (attr.Category != lastCategory)
            {
                var header = new Label { Text = attr.Category };
                _fieldContainer.AddChild(header);
                lastCategory = attr.Category;
            }
            var row = new HBoxContainer();
            row.AddChild(new Label { Text = attr.Label, AutowrapMode = TextServer.AutowrapMode.WordSmart,
             SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(100, 0) });
            Control control = null;
            object currentValue = prop.GetValue(config);
            if (prop.PropertyType == typeof(bool))
            {
                var check = new CheckBox 
                { 
                    ButtonPressed = (bool)currentValue,
                    SizeFlagsHorizontal = Control.SizeFlags.Expand,
                };
                control = check;
            }
            else if (prop.PropertyType == typeof(float) || prop.PropertyType == typeof(int))
            {
                var spin = new SpinBox
                {
                    MinValue = attr.Min,
                    MaxValue = attr.Max,
                    Step = attr.Step,
                    Value = Convert.ToDouble(currentValue),
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                    CustomMinimumSize = new Vector2(100, 0)
                };
                control = spin;
            }
            else if (prop.PropertyType == typeof(string))
            {
                var textInput = new LineEdit
                {
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                    Text = Convert.ToString(currentValue),
                    CustomMinimumSize = new Vector2(100, 0)
                };
                control = textInput;
            }
            row.AddChild(control);
            _fieldContainer.AddChild(row);
            _bindings.Add((prop, control));
        }
    }

    private void OnSavePressed()
    {
        foreach (var (prop, control) in _bindings)
        {
            object newValue = control switch
            {
                CheckBox check => check.ButtonPressed,
                SpinBox spin => Convert.ChangeType(spin.Value, prop.PropertyType),
                LineEdit lineEdit => lineEdit.Text,
                _ => null
            };

            if (newValue != null)
                prop.SetValue(config, newValue);
        }
        // special
        GetWindow().Mode = config.Fullscreen ? Window.ModeEnum.Fullscreen : Window.ModeEnum.Windowed;
        ConfigHelper.Save();
    }

    private void OnOpenDataPressed()
    {
		var path = ProjectSettings.GlobalizePath("user://data"); ;
		OS.ShellOpen(path);
    }
}