using Godot;
using System;

public partial class PatternUi : Button
{
    [Export] public Label namelabel;
    [Export] public Label id;
    [Export] public ColorRect color;
    public int PatternId; // what pattern id is this associated with
    public void ApplyPattern(PatternModel model)
    {
        id.Text = model.Id.ToString();
        namelabel.Text = model.Name;
        color.Color = RenderingUtils.ColorFromString(model.Name);
    }
}
