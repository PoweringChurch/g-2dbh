using System.Collections.Generic;
using Godot;

public class PatternRegistry
{
    private List<PatternModel> _models = new();
    private Dictionary<string, PatternModel> _modelsById = new();
    public IReadOnlyList<PatternModel> Models => _models;
    public void SetModels(List<PatternModel> models)
    {
        _models = models ?? new();
        RebuildCache();
    }

    private void RebuildCache()
    {
        _modelsById.Clear();
        foreach (var m in _models)
            _modelsById[m.Id] = m;
    }
    public PatternModel GetModel(string id)
    {
        return _modelsById.TryGetValue(id, out var model) ? model : null;
    }
    public void AddModel(PatternModel model)
    {
        _models.Add(model);
        _modelsById[model.Id] = model;
    }
    public void RemoveModel(string id)
    {
        if (!_modelsById.TryGetValue(id, out var model))
            return;
        _models.Remove(model);
        _modelsById.Remove(id);
    }
    public void UpdateModel(PatternModel model, string previousId = null)
    {
        string oldId = previousId ?? model.Id;
        if (oldId != model.Id)
            _modelsById.Remove(oldId);

        _modelsById[model.Id] = model;

        int index = _models.FindIndex(m => m.Id == model.Id || m == _modelsById.GetValueOrDefault(oldId));
        if (index >= 0)
            _models[index] = model;
        else
            _models.Add(model);
    }
}