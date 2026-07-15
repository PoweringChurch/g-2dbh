using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;

public partial class ProjectileVisuals : Control
{
    public ProjectileModel Model;
    [Export] private OptionButton TextureDropdown;
    [Export] private HSlider ScaleSlider;
    [Export] private CheckButton LockRotation;
    private ProjectilePreview pp => ProjectileCreator.Instance.ProjectilePreview;
    private string[] textures;
    public override void _Ready()
    {
        var texList = new List<string>() {"default"};
        var dir = DirAccess.Open("res://data/default-assets/images/");
        var files = dir.GetFiles();
        for (int i = 0; i < files.Length; i++)
        {
            var file = files[i];
            if (file.EndsWith(".import") || file.StartsWith('.')) continue;
            var fileName = Path.GetFileNameWithoutExtension(file);
            texList.Add("res://data/default-assets/images/"+fileName);
            TextureDropdown.AddItem(fileName);
        }
        textures = [.. texList];
        dir.ListDirEnd();
        TextureDropdown.ItemSelected += TextureSelected;
        ScaleSlider.ValueChanged += (v) => { Model.RenderScale = (float)v; pp.MarkDirty(); };
        LockRotation.Toggled += (on) => { Model.LockRotation = on; pp.MarkDirty(); };
    }
    private void TextureSelected(long id)
    {
        Model.Texture = textures[id];
        pp.MarkDirty();
    }
    public void Load(ProjectileModel newModel)
    {
        Model = newModel;
        ScaleSlider.Value = Model.RenderScale;
        LockRotation.ButtonPressed = Model.LockRotation;
        int found = 0;
        for (int i = 0; i < textures.Length; i++)
        {
            if (textures[i] == Model.Texture)
            {
                found = i;
                break;
            }
        }
        TextureDropdown.Select(found);
    }
}