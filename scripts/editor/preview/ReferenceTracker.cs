using System.Collections.Generic;
using Godot;
public abstract partial class EditorInstance : Node2D
{
    public abstract Reference Reference {get;}
    public abstract bool Invalid {get;}
    public abstract void Init(Reference r);
    public abstract void OnModelUpdate();
    public abstract bool IsAlive(float currentTime);
    public abstract float DistanceTo(Vector2 localPos);
}
public class ReferenceTracker<TEditorInstance>
    where TEditorInstance : EditorInstance, new()
{
    private readonly Dictionary<Reference, EditorInstance> _instances = new();
    private readonly Node2D _parent;
    public ReferenceTracker(Node2D parent) => _parent = parent;
    public (Reference reference, Vector2 offset) GetNearestReference(Vector2 local, float currentTime, float maxDist = 50f)
    {
        Reference nearestRef = default;
        EditorInstance nearestInstance = null;
        float nearestDist = float.MaxValue;

        foreach (var kvp in _instances)
        {
            var instance = kvp.Value;
            if (instance.Invalid || !instance.IsAlive(currentTime))
                continue;

            float dist = instance.DistanceTo(local);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearestRef = kvp.Key;
                nearestInstance = instance;
            }
        }

        if (nearestInstance != null && nearestDist < maxDist)
            return (nearestRef, local - nearestInstance.Position);

        return (default, Vector2.Zero);
    }
    public void OnModelUpdate(string modelId, string oldId)
    {
        foreach (var kvp in _instances)
            if (kvp.Key.Id == modelId)
                kvp.Value.OnModelUpdate();
    }
    public void Add(Reference r)
    {
        var instance = new TEditorInstance();
        _parent.AddChild(instance);
        instance.Init(r);
        _instances[r] = instance;
    }

    public void Remove(Reference r)
    {
        if (_instances.TryGetValue(r, out var instance))
        {
            instance.QueueFree();
            _instances.Remove(r);
        }
    }

    public void Clear()
    {
        foreach (var i in _instances.Values)
            i.QueueFree();
        _instances.Clear();
    }
}

