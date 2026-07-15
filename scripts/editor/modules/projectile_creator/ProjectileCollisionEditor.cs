using Godot;

public partial class ProjectileCollisionEditor : Control
{
    private ProjectileModel Model;
    [Export] private SpinBox Radius;
    [Export] private CheckButton UseShape;
    [Export] private ShapeEditor ShapeEditor;
    private ProjectilePreview pp => ProjectileCreator.Instance.ProjectilePreview;
    public override void _Ready()
    {
        Radius.ValueChanged += (v) => {Model.Radius = (float)v; pp.MarkDirty();};
        UseShape.Toggled += ToggleUseShape;
        ShapeEditor.ShapeUpdated += (pts) => {Model.Shape = CollisionUtils.Vect2sToFloatArr(pts); pp.MarkDirty();};
    }
    public void ToggleUseShape(bool on)
    {
        Radius.Visible = !on;
        ShapeEditor.Visible = on;
        Model.UseShape = on;
        pp.MarkDirty();
    }
    public void Load(ProjectileModel newModel)
    {
        Model = newModel;
        Radius.Value = Model.Radius;
        UseShape.ButtonPressed = Model.UseShape;
        ToggleUseShape(Model.UseShape);
        ShapeEditor.Load(Model.Shape);
        pp.MarkDirty();
    }
}