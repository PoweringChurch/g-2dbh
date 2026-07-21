using Godot;

public partial class ProjectileCollisionEditor : Control
{
    private ProjectileModel Model;
    [Export] private SpinBox Radius;
    [Export] private SpinBox TelegraphTime;
    [Export] private CheckButton CanCollide;
    [Export] private CheckButton UseShape;
    [Export] private ShapeEditor ShapeEditor;
    private ProjectilePreview pp => ProjectileCreator.Instance.ProjectilePreview;
    public override void _Ready()
    {
        Radius.ValueChanged += (v) => {Model.Radius = (float)v; pp.MarkDirty();};
        UseShape.Toggled += ToggleUseShape;
        ShapeEditor.ShapeUpdated += (pts) => {Model.Shape = CollisionUtils.Vect2sToFloatArr(pts); pp.MarkDirty();};
        CanCollide.Toggled += (on) => { Model.CanCollide = on; pp.MarkDirty(); };
        TelegraphTime.ValueChanged += (v) => { Model.TelegraphTime = (float)v; pp.MarkDirty(); };
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
        TelegraphTime.Value = Model.TelegraphTime;
        CanCollide.ButtonPressed = Model.CanCollide;
        ToggleUseShape(Model.UseShape);
        ShapeEditor.Load(Model.Shape);
        pp.MarkDirty();
    }
}