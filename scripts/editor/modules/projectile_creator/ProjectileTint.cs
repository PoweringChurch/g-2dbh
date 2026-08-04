using Godot;
using System;

public partial class ProjectileTint : Control
{
    public ProjectileModel Model;
    [Export] ColorRect ColorDisplay;
    [Export] Slider HueSlider;
    [Export] Slider SatSlider;
    [Export] Slider ValSlider;

    [Export] SpinBox HueSpin;
    [Export] SpinBox SatSpin;
    [Export] SpinBox ValSpin;
    private ProjectilePreview pp => ProjectileCreator.Instance.ProjectilePreview;
    public override void _Ready()
    {
        HueSlider.ValueChanged += (to) => SliderChanged(to, HueSlider, HueSpin);
        SatSlider.ValueChanged += (to) => SliderChanged(to, SatSlider, SatSpin);
        ValSlider.ValueChanged += (to) => SliderChanged(to, ValSlider, ValSpin);
        HueSpin.ValueChanged += (to) => SpinChanged(to, HueSpin, HueSlider);
        SatSpin.ValueChanged += (to) => SpinChanged(to, SatSpin, SatSlider);
        ValSpin.ValueChanged += (to) => SpinChanged(to, ValSpin, ValSlider);
    }
    private void SliderChanged(double to, Slider slider, SpinBox spin)
    {
        spin.SetValueNoSignal(to);
        Model.Tint.ToHsv(out float hue, out float sat, out float val);
        if (slider == HueSlider)
            Model.Tint = Color.FromHsv((float)to/360f, sat, val);
        else if (slider == SatSlider)
            Model.Tint = Color.FromHsv(hue, (float)to/100f, val);
        else if (slider == ValSlider)
            Model.Tint = Color.FromHsv(hue, sat, (float)to/100f);
        ColorDisplay.Color = Model.Tint;
        pp.MarkDirty();
    }
    private void SpinChanged(double to, SpinBox spin, Slider slider)
    {
        slider.SetValueNoSignal(to);
        Model.Tint.ToHsv(out float hue, out float sat, out float val);
        if (spin == HueSpin)
            Model.Tint = Color.FromHsv((float)to/360f, sat, val);
        else if (spin == SatSpin)
            Model.Tint = Color.FromHsv(hue, (float)to/100f, val);
        else if (spin == ValSpin)
            Model.Tint = Color.FromHsv(hue, sat, (float)to/100f);
        ColorDisplay.Color = Model.Tint;
        pp.MarkDirty();
    }
    public void Load(ProjectileModel newModel)
    {
        Model = newModel;
        ColorDisplay.Color = Model.Tint;

        Model.Tint.ToHsv(out float hue, out float sat, out float val);
        HueSlider.Value = hue*360;
        SatSlider.Value = sat*100;
        ValSlider.Value = val*100;
    }
}
