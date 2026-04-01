using System.Collections.Generic;

namespace FUEngine.Core;

/// <summary>
/// Caché de assets en memoria (sprites, tiles, animaciones) para render rápido.
/// El runtime puede precargar aquí las rutas y datos para no acceder a disco en cada frame.
/// </summary>
public class AssetCache
{
    private readonly Dictionary<string, object?> _byPath = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, object?> _byId = new(StringComparer.OrdinalIgnoreCase);

    public void RegisterPath(string path, object? data = null)
    {
        _byPath[path] = data;
    }

    public void RegisterId(string id, object? data)
    {
        _byId[id] = data;
    }

    public bool TryGetByPath(string path, out object? data) => _byPath.TryGetValue(path, out data);
    public bool TryGetById(string id, out object? data) => _byId.TryGetValue(id, out data);
    public void Clear()
    {
        _byPath.Clear();
        _byId.Clear();
    }
}
