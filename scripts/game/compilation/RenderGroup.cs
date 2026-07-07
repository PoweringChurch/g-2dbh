using System.Collections.Generic;
using Godot;

public class RenderGroup
{
    public Mesh Mesh;
    public Texture2D Texture; // null for shape groups
    public MultiMeshInstance2D Node;
    public MultiMesh MultiMesh;
    public readonly List<int> BakeIndices = new();
}
public struct DynamicProjectile
{
    public Vector2 Pos;
    public float Forward;
    public float Scale;
}