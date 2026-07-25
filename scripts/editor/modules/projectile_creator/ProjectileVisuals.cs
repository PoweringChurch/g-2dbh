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
        for (int i = 0; i < LevelCompiler.ProjectileTextures.Length; i++)
            TextureDropdown.AddItem(LevelCompiler.ProjectileTextures[i].TextureName);
        TextureDropdown.ItemSelected += TextureSelected;
        XScaleSlider.ValueChanged += (v) => { Model.RenderScale = new((float)v, Model.RenderScale.Y); pp.MarkDirty(); };
        YScaleSlider.ValueChanged += (v) => { Model.RenderScale = new(Model.RenderScale.X, (float)v); pp.MarkDirty(); };
        LockRotation.Toggled += (on) => { Model.LockRotation = on; pp.MarkDirty(); };
    }
    private void TextureSelected(long id)
    {
        Model.TextureId = (int)id;
        pp.MarkDirty();
    }
    public void Load(ProjectileModel newModel)
    {
        Model = newModel;
        XScaleSlider.Value = Model.RenderScale.X;
        YScaleSlider.Value = Model.RenderScale.Y;
        LockRotation.ButtonPressed = Model.LockRotation;
        TextureDropdown.Select(Model.TextureId);
    }
}