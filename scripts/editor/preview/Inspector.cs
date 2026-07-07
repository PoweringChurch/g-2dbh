using System;
using Godot;

public partial class Inspector : Control
{
    [Export] SpinBox xinput;
    [Export] SpinBox yinput;
    [Export] SpinBox tinput;
    [Export] SpinBox fwdinput;
    [Export] SpinBox idinput;
    [Export] OptionButton typeInput;
    private Editor e;
    public override void _Ready()
    {
        e = GetNode<Editor>("/root/Editor");
        xinput.ValueChanged += OnXChanged;
        yinput.ValueChanged += OnYChanged;
        tinput.ValueChanged += OnTChanged;
        fwdinput.ValueChanged += OnFwdChanged;
        idinput.ValueChanged += OnIdChanged;
        typeInput.ItemSelected += OnOptionsChange;
    }
    public void Update(EditorReference r)
    {
        if (r == null)
        {
            xinput.SetValueNoSignal(0);
            yinput.SetValueNoSignal(0);
            tinput.SetValueNoSignal(0);
            fwdinput.SetValueNoSignal(0);
            idinput.SetValueNoSignal(-1);
            typeInput.Selected = -1;
            return;
        }
        xinput.SetValueNoSignal(r.SpawnX);
        yinput.SetValueNoSignal(r.SpawnY);
        tinput.SetValueNoSignal(r.T);
        fwdinput.SetValueNoSignal(r.F);
        idinput.SetValueNoSignal(r.Id);
        typeInput.Selected = (int)r.Type;
    }
    private void OnXChanged(double value)
    {
        if (e.SelectedReference == null) return;
        e.SelectedReference.SpawnX = (float)value;
        e.SyncPreview();
    }
    private void OnYChanged(double value)
    {
        if (e.SelectedReference == null) return;
        e.SelectedReference.SpawnY = (float)value;
        e.SyncPreview();
    }
    private void OnTChanged(double value)
    {
        if (e.SelectedReference == null) return;
        e.SelectedReference.T = (float)value;
        e.SyncPreview();
    }
    private void OnFwdChanged(double value)
    {
        if (e.SelectedReference == null) return;
        e.SelectedReference.F = (float)value;
        e.SyncPreview();
    }
    private void OnIdChanged(double value)
    {
        if (e.SelectedReference == null) return;
        e.SelectedReference.Id = (int)value;
        e.SyncPreview();
    }
    private void OnOptionsChange(long index)
    {
        if (e.SelectedReference == null) return;
        e.SelectedReference.Type = (ModelType)index;
        e.SyncPreview();
    }
}