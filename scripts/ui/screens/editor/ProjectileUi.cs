using Godot;
using System;

public partial class ProjectileUi : Button
{
    [Export] public Label namelabel;
    [Export] public Label id;
    [Export] public ColorRect color;
    [Export] public TextureRect texture;
    public int ProjectileId; // what projectile id is this associated with
    public void ApplyProjectile(ProjectileModel model)
    {
        id.Text = model.Id.ToString();
        namelabel.Text = model.Name;
        color.Color = RenderingUtils.ColorFromString(model.Name);
        //texture.Texture = RenderingUtils.LoadTexture(LevelCompiler.ProjectileTextures[model.TextureId].TexturePath);
        ProjectileId = model.Id;
    }
}
