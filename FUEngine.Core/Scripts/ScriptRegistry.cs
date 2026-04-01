namespace FUEngine.Core;

/// <summary>
/// Registro de scripts disponibles. Permite registrar y listar por id para el editor y futuro runtime.
/// </summary>
public class ScriptRegistry
{
    private readonly Dictionary<string, ScriptDefinition> _scripts = new();

    public void Register(ScriptDefinition definition)
    {
        _scripts[definition.Id] = definition;
    }

    /// <summary>Quita una definición (p. ej. al eliminar el .lua del proyecto).</summary>
    public bool Unregister(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        return _scripts.Remove(id);
    }

    public ScriptDefinition? Get(string id)
    {
        return _scripts.TryGetValue(id, out var d) ? d : null;
    }

    public IReadOnlyCollection<ScriptDefinition> GetAll()
    {
        return _scripts.Values.ToList();
    }

    public IEnumerable<string> GetIds()
    {
        return _scripts.Keys;
    }
}
