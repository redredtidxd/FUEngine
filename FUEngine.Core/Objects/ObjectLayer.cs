namespace FUEngine.Core;

/// <summary>
/// Capa de objetos colocados en el mapa. Gestiona instancias y consultas de colisión.
/// </summary>
public class ObjectLayer
{
    private readonly List<ObjectInstance> _instances = new();
    private readonly Dictionary<string, ObjectDefinition> _definitions = new();

    public IReadOnlyList<ObjectInstance> Instances => _instances;
    public IReadOnlyDictionary<string, ObjectDefinition> Definitions => _definitions;

    public void RegisterDefinition(ObjectDefinition definition)
    {
        _definitions[definition.Id] = definition;
    }

    public ObjectDefinition? GetDefinition(string id)
    {
        return _definitions.TryGetValue(id, out var d) ? d : null;
    }

    public void AddInstance(ObjectInstance instance)
    {
        _instances.Add(instance);
    }

    public void RemoveInstance(ObjectInstance instance)
    {
        _instances.Remove(instance);
    }

    public void RemoveInstanceById(string instanceId)
    {
        var i = _instances.FirstOrDefault(x => x.InstanceId == instanceId);
        if (i != null) _instances.Remove(i);
    }

    /// <summary>
    /// Indica si hay colisión en la celda (algún objeto con colisión ocupa ese tile).
    /// Asume que cada objeto ocupa su definición Width x Height desde (X,Y).
    /// </summary>
    public bool IsCollisionAt(int tileX, int tileY)
    {
        foreach (var inst in _instances)
        {
            var def = GetDefinition(inst.DefinitionId);
            if (def == null || !inst.GetColision(def)) continue;
            int x0 = (int)Math.Floor(inst.X);
            int y0 = (int)Math.Floor(inst.Y);
            if (tileX >= x0 && tileX < x0 + def.Width && tileY >= y0 && tileY < y0 + def.Height)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Devuelve objetos que tienen colisión en la celda (para interactivos, etc.).
    /// </summary>
    public IEnumerable<ObjectInstance> GetObjectsAt(int tileX, int tileY)
    {
        foreach (var inst in _instances)
        {
            var def = GetDefinition(inst.DefinitionId);
            if (def == null) continue;
            int x0 = (int)Math.Floor(inst.X);
            int y0 = (int)Math.Floor(inst.Y);
            if (tileX >= x0 && tileX < x0 + def.Width && tileY >= y0 && tileY < y0 + def.Height)
                yield return inst;
        }
    }
}
