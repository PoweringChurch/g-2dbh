using System.Collections.Generic;
using Godot;

public class RenderGroup
{
    public Mesh Mesh;
    public Texture2D Texture; // null for shape groups
    public MultiMeshInstance2D Node;
    public MultiMesh MultiMesh;
    public List<int> BulletIndices = new();
}
public class EditorRenderGroup : RenderGroup
{
    public List<int> PatternBulletIndices = new();
}