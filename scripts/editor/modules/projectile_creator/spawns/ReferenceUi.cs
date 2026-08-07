using Godot;
using System;

public partial class ReferenceUi : Control
{
    private EditorReference Editing;
    [Export] public Label nameLabel;
    [Export] public SpinBox spawnx;
    [Export] public SpinBox spawny;
    [Export] public SpinBox fwd;
    [Export] public SpinBox time;
    [Export] public SpinBox id;
    [Export] public OptionButton type;
    [Export] public MessageDisplay errdisplay;

    [Export] public Button remove;
    private ProjectilePreview pp => ProjectileCreator.Instance.ProjectilePreview;
    public override void _Ready()
    {
        if (Editing == null)
            Console.LogErr("A reference ui doesnt have an editing reference set!");
        spawnx.ValueChanged += (v) => { Editing.SpawnX = (float)v; pp.MarkDirty(); };
        spawny.ValueChanged += (v) => { Editing.SpawnY = (float)v; pp.MarkDirty(); };
        fwd.ValueChanged += (v) => { Editing.SpawnF = (float)v; pp.MarkDirty(); };
        time.ValueChanged += (v) => { Editing.T = (float)v; pp.MarkDirty(); };
        type.ItemSelected += (idx) => { Editing.Type = (ModelType)idx; IdChanged(Editing.Id); };
        id.ValueChanged += IdChanged;
    }
    public void Load(EditorReference r)
    {
        Editing = r;
        spawnx.SetValueNoSignal(Editing.SpawnX);
        spawny.SetValueNoSignal(Editing.SpawnY);
        fwd.SetValueNoSignal(Editing.SpawnF);
        time.SetValueNoSignal(Editing.T);
        id.SetValueNoSignal(Editing.Id);
        type.Select((int)Editing.Type);
        IdChanged(Editing.Id);
    }
    private void IdChanged(double v)
    {
        int id = (int)v;
        if (Editing.Type == ModelType.Projectile)
        {
            if (Editor.Instance.ProjectileModels[id] != null )
            {
                Editing.Id = id;
                pp.MarkDirty();
                errdisplay.ClearMessage("id");
                nameLabel.Text = Editor.Instance.ProjectileModels[id].Name;
            } else errdisplay.SetMessage("id", $"[Id] Projectile model of id '{id}' does not exist");
        } else if (Editing.Type == ModelType.Pattern)
        {
            if (Editor.Instance.PatternModels[id] != null )
            {
                Editing.Id = id;
                pp.MarkDirty();
                errdisplay.ClearMessage("id");
                nameLabel.Text = Editor.Instance.PatternModels[id].Name;
            } else errdisplay.SetMessage("id", $"[Id] Pattern model of id '{id}' does not exist");
        }
    }
    public override void _ExitTree()
    {
        Editing = null;
    }

}
