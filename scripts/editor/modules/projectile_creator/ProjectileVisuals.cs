using Godot;

public partial class ProjectileVisuals : Control
{
    public ProjectileModel Model;
    [Export] private TextureRect SelectedTextureDisplay;
    [Export] private Container TextureSelectionTabs;
    [Export] private PackedScene TextureIconButtonScene;

    [Export] private HSlider XScaleSlider;
    [Export] private HSlider YScaleSlider;
    [Export] private CheckButton LockRotation;
    private ProjectilePreview pp => ProjectileCreator.Instance.ProjectilePreview;
    public override void _Ready()
    {
        var projNames = RenderingUtils.GetProjectileNames();
        for (int i = 0; i < projNames.Length; i++)
        {
            var projName = projNames[i];
            int group = GetProjectileGroup(projName);
            if (group == -1)
                continue;
            Container groupContainer = (Container)TextureSelectionTabs.GetChild(group);

            var projItemBtn = TextureIconButtonScene.Instantiate<Button>();
            var projectileTexture = RenderingUtils.GetProjectileTexture(projName);
            projItemBtn.GetNode<TextureRect>("Texture").Texture = projectileTexture;
            projItemBtn.Pressed += () =>
            {
                SelectedTextureDisplay.Texture = projectileTexture;
                Model.TextureName = projName;
                pp.MarkDirty();
            };
            groupContainer.AddChild(projItemBtn);
        }
        XScaleSlider.ValueChanged += (v) => { Model.RenderScale = new((float)v, Model.RenderScale.Y); pp.MarkDirty(); };
        YScaleSlider.ValueChanged += (v) => { Model.RenderScale = new(Model.RenderScale.X, (float)v); pp.MarkDirty(); };
        LockRotation.Toggled += (on) => { Model.LockRotation = on; pp.MarkDirty(); };
    }
    private void TextureSelected(long id)
    {
        var projNames = RenderingUtils.GetProjectileNames();
        Model.TextureName = projNames[(int)id];
        
    }
    public void Load(ProjectileModel newModel)
    {
        Model = newModel;
        SelectedTextureDisplay.Texture = RenderingUtils.GetProjectileTexture(Model.TextureName);
        XScaleSlider.Value = Model.RenderScale.X;
        YScaleSlider.Value = Model.RenderScale.Y;
        LockRotation.ButtonPressed = Model.LockRotation;
    }
    private static int GetProjectileGroup(string projectileId)
    {
        return projectileId switch
        {
            "pattern_marker.png" => -1,
            "bullet.png" or "dagger.png" or "death_card.png" or "double_orb.png" or "orb.png" or "big_orb.png" => 0,
            "snowball.png" or "snowflake.png" => 1,
            _ => 2,
        };
    }
}