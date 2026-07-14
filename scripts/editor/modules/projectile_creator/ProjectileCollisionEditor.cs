using Godot;

public partial class ProjectileCollisionEditor : Control
{
    private ProjectileModel Model;
    [Export] private SpinBox Radius;
    [Export] private CheckButton UseShape;
    [Export] private ShapeEditor ShapeEditor;
    public override void _Ready()
    {
        Radius.ValueChanged += (v) => Model.Radius = (float)v;
        UseShape.Toggled += ToggleUseShape;
        ShapeEditor.ShapeUpdated += (pts) => Model.Shape = CollisionUtils.Vect2sToFloatArr(pts);
    }
    public void ToggleUseShape(bool on)
    {
        Radius.Visible = !on;
        ShapeEditor.Visible = on;
    }
    public void Load(ProjectileModel newModel)
    {
        Model = newModel;
        Radius.Value = Model.Radius;
        UseShape.ButtonPressed = Model.UseShape;
        ShapeEditor.Load(Model.Shape);
    }
}