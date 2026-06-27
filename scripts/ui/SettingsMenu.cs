using Godot;
using System;
using System.Reflection;

public partial class SettingsMenu : CanvasLayer
{
    [Export] public Button _saveButton;
    [Export] public Button _returnButton;
    [Export] public VBoxContainer _fieldContainer; // where generated rows go

    private Config _config;
    private readonly System.Collections.Generic.List<(PropertyInfo Prop, Control Control)> _bindings = new();
    private UIManager _ui;
    public override void _Ready()
    {
        _ui = GetNode<UIManager>("/root/UIManager");
        _config = ConfigHelper.Current; // however you're storing the loaded instance

        BuildFields();

        _saveButton.Pressed += OnSavePressed;
        _returnButton.Pressed += OnReturnPressed;
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
            row.AddChild(new Label { Text = attr.Label, CustomMinimumSize = new Vector2(180, 0) });
            Control control;
            object currentValue = prop.GetValue(_config);
            if (prop.PropertyType == typeof(bool))
            {
                var check = new CheckBox { ButtonPressed = (bool)currentValue };
                control = check;
            }
            else
            {
                var spin = new SpinBox
                {
                    MinValue = attr.Min,
                    MaxValue = attr.Max,
                    Step = attr.Step,
                    Value = Convert.ToDouble(currentValue),
                    CustomMinimumSize = new Vector2(100, 0)
                };
                control = spin;
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
                _ => null
            };

            if (newValue != null)
                prop.SetValue(_config, newValue);
        }

        ConfigHelper.Save();
    }

    private void OnReturnPressed()
    {
        _ui.ToggleSettings(false);
    }
}