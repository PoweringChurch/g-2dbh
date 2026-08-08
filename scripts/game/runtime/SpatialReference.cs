using Godot;

public struct SpatialReference
{
    public int ModelId;
    public ModelType Type;
    public Vector2 SpawnPos;
    public Vector2 Pos;
    public double SpawnF;
    public double F;
    public double T;
    public int Depth;
    public override string ToString()
    {
        return $"ProjId={ModelId} Type={Type}, SpawnPos={SpawnPos}, Pos={Pos}, SpawnF={SpawnF}, F={F}, T={T}, Depth={Depth}";
    }
}