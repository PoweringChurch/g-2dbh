using Godot;

public partial class ProjectileVisuals : Control
{
    public ProjectileModel Model;
    [Export] private OptionButton TextureDropdown;
    [Export] private HSlider XScaleSlider;
    [Export] private HSlider YScaleSlider;
    [Export] private CheckButton LockRotation;
    private ProjectilePreview pp => ProjectileCreator.Instance.ProjectilePreview;
    public override void _Ready()
    {
        var projNames = RenderingUtils.GetProjectileNames();
        for (int i = 0; i < projNames.Length; i++)
            TextureDropdown.AddItem(projNames[i]);
        TextureDropdown.ItemSelected += TextureSelected;
        XScaleSlider.ValueChanged += (v) => { Model.RenderScale = new((float)v, Model.RenderScale.Y); pp.MarkDirty(); };
        YScaleSlider.ValueChanged += (v) => { Model.RenderScale = new(Model.RenderScale.X, (float)v); pp.MarkDirty(); };
        LockRotation.Toggled += (on) => { Model.LockRotation = on; pp.MarkDirty(); };
    }
    private void TextureSelected(long id)
    {
        var projNames = RenderingUtils.GetProjectileNames();
        Model.TextureName = projNames[(int)id];
        pp.MarkDirty();
    }
    public void Load(ProjectileModel newModel)
    {
        Model = newModel;
        XScaleSlider.Value = Model.RenderScale.X;
        YScaleSlider.Value = Model.RenderScale.Y;
        LockRotation.ButtonPressed = Model.LockRotation;
        var projNames = RenderingUtils.GetProjectileNames();
        int found = -1;
        for (int i = 0; i < projNames.Length; i++)
        {
            if (projNames[i] == Model.TextureName)
            {
                found = i;
                break;
            }
        }
        if (found == -1)
        {
            found = 0;
            Model.TextureName = projNames[0];
        }
        TextureDropdown.Select(found);
    }
}