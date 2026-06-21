using System.Collections.Generic;

public class ProjectileRegistry
{
    private List<ProjectileModel> _models = new();
    private Dictionary<string, ProjectileModel> _modelsById = new();
    public IReadOnlyList<ProjectileModel> Models => _models;
    public void SetModels(List<ProjectileModel> models)
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
    public ProjectileModel GetModel(string id)
    {
        return _modelsById.TryGetValue(id, out var model) ? model : null;
    }
    public void AddModel(ProjectileModel model)
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
    public void UpdateModel(ProjectileModel model, string previousId = null)
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