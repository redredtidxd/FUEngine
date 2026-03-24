using System.Collections.Generic;
using System.Linq;
using FUEngine.Core;

namespace FUEngine.Runtime;

/// <summary>
/// Contexto del mundo/escena para que WorldApi resuelva objetos por nombre, tag, y ejecute instantiate/destroy.
/// Lo implementa el motor o el editor (modo Play) para conectar Lua con la escena real.
/// </summary>
public interface IWorldContext
{
    /// <summary>Busca un objeto por nombre (en toda la escena o jerarquía).</summary>
    GameObject? GetObjectByName(string name);

    /// <summary>Busca objetos por etiqueta.</summary>
    IEnumerable<GameObject> GetObjectsByTag(string tag);

    /// <summary>Todos los objetos activos de la escena.</summary>
    IEnumerable<GameObject> GetAllObjects();

    /// <summary>Instancia un seed en la posición y rotación dadas. <paramref name="variant"/> opcional: prueba id <c>prefab_variant</c> antes que <paramref name="prefabName"/>.</summary>
    GameObject? Instantiate(string prefabName, double x, double y, double rotation = 0, string? variant = null);

    /// <summary>Destruye el objeto (marca inactivo o lo quita de la escena).</summary>
    void Destroy(GameObject gameObject);

    /// <summary>Opcional: busca por ruta jerárquica (ej: "Root/Enemy/Weapon"). Si no se implementa, puede resolverse desde raíz.</summary>
    GameObject? FindByPath(string path) => null;

    /// <summary>Busca por <see cref="GameObject.InstanceId"/> (id de instancia en objetos.json).</summary>
    GameObject? GetObjectByInstanceId(string instanceId) => null;
}

/// <summary>Implementación simple de IWorldContext sobre una lista de GameObjects (para pruebas o modo Play).</summary>
public sealed class WorldContextFromList : IWorldContext
{
    private readonly List<GameObject> _objects = new();

    public IList<GameObject> Objects => _objects;

    /// <summary>Si es true, <see cref="Destroy"/> solo marca <see cref="GameObject.PendingDestroy"/>; el host debe hacer flush al final del frame.</summary>
    public bool DeferDestroy { get; set; }

    private static bool IsLive(GameObject o) => o is { PendingDestroy: false };

    /// <summary>Si está asignado (p. ej. modo Play), intenta expandir <c>seeds.json</c> antes del stub vacío. Último argumento: variante opcional.</summary>
    public Func<string, double, double, double, string?, GameObject?>? TryExpandPrefab { get; set; }

    public GameObject? GetObjectByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        return _objects.FirstOrDefault(o => IsLive(o) && string.Equals(o.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<GameObject> GetObjectsByTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) yield break;
        foreach (var o in _objects)
        {
            if (!IsLive(o)) continue;
            if (o.Tags == null || o.Tags.Count == 0) continue;
            foreach (var t in o.Tags)
            {
                if (string.Equals(t, tag, StringComparison.OrdinalIgnoreCase))
                {
                    yield return o;
                    break;
                }
            }
        }
    }

    public IEnumerable<GameObject> GetAllObjects()
    {
        foreach (var o in _objects)
        {
            if (IsLive(o)) yield return o;
        }
    }

    public GameObject? Instantiate(string prefabName, double x, double y, double rotation = 0, string? variant = null)
    {
        if (string.IsNullOrWhiteSpace(prefabName)) return null;
        var key = prefabName.Trim();
        var expanded = TryExpandPrefab?.Invoke(key, x, y, rotation, string.IsNullOrWhiteSpace(variant) ? null : variant.Trim());
        if (expanded != null)
            return expanded;

        var go = new GameObject
        {
            Name = key,
            Transform = new Transform
            {
                X = (float)x,
                Y = (float)y,
                RotationDegrees = (float)rotation
            }
        };
        _objects.Add(go);
        return go;
    }

    public void Destroy(GameObject gameObject)
    {
        if (gameObject == null) return;
        if (DeferDestroy)
        {
            gameObject.PendingDestroy = true;
            gameObject.RuntimeActive = false;
            return;
        }
        _objects.Remove(gameObject);
    }

    public GameObject? FindByPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        var parts = path.Split(new[] { '/', '\\' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return null;
        var root = GetObjectByName(parts[0]);
        if (root == null || parts.Length == 1) return root;
        return root.FindInHierarchy(string.Join("/", parts.Skip(1)));
    }

    public GameObject? GetObjectByInstanceId(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId)) return null;
        var key = instanceId.Trim();
        foreach (var o in _objects)
        {
            if (!IsLive(o)) continue;
            if (string.IsNullOrEmpty(o.InstanceId)) continue;
            if (string.Equals(o.InstanceId, key, StringComparison.OrdinalIgnoreCase))
                return o;
        }

        return null;
    }
}
