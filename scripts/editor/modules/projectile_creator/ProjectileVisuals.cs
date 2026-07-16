using Godot;

public partial class ProjectileVisuals : Control
{
    public ProjectileModel Model;
    [Export] private OptionButton TextureDropdown;
    [Export] private HSlider ScaleSlider;
    [Export] private CheckButton LockRotation;
    private ProjectilePreview pp => ProjectileCreator.Instance.ProjectilePreview;
    public override void _Ready()
    {
        for (int i = 0; i < LevelCompiler.ProjectileTextures.Length; i++)
            TextureDropdown.AddItem(LevelCompiler.ProjectileTextures[i].TextureName);
        TextureDropdown.ItemSelected += TextureSelected;
        ScaleSlider.ValueChanged += (v) => { Model.RenderScale = (float)v; pp.MarkDirty(); };
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
        ScaleSlider.Value = Model.RenderScale;
        LockRotation.ButtonPressed = Model.LockRotation;
        TextureDropdown.Select(Model.TextureId);
    }
}