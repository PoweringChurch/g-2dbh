using System;
using Godot;

public partial class ProjectileVisuals : Control
{
    public ProjectileModel Model;
    [Export] private OptionButton TextureDropdown;
    [Export] private HSlider ScaleSlider;
    [Export] private CheckButton LockRotation;
    public override void _Ready()
    {
        TextureDropdown.ItemSelected += TextureSelected;
        ScaleSlider.ValueChanged += (v) => Model.RenderScale = (float)v;
        LockRotation.Toggled += (on) => Model.LockRotation = on;
    }
    private void TextureSelected(long id)
    {
        Model.Texture = id.ToString(); // TEMPORARY
    }
    public void Load(ProjectileModel newModel)
    {
        Model = newModel;
        ScaleSlider.Value = Model.RenderScale;
        LockRotation.ButtonPressed = Model.LockRotation;
        TextureDropdown.Selected = 0; // TEMPORARY
    }
}