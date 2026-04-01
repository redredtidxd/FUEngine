using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FUEngine.Runtime;

/// <summary>
/// Carga el código fuente de scripts desde disco. Opcionalmente cachea por path para hot reload.
/// </summary>
public sealed class ScriptLoader
{
    private readonly string _projectDirectory;
    private readonly Dictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);

    public ScriptLoader(string projectDirectory)
    {
        _projectDirectory = projectDirectory ?? "";
    }

    /// <summary>Raíz del proyecto en disco (scripts y assets relativos a esta carpeta).</summary>
    public string ProjectDirectory => _projectDirectory;

    /// <summary>Unifica separadores y recorta para comparar rutas (Windows vs JSON / GetRelativePath).</summary>
    public static string NormalizeRelativePath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return "";
        return relativePath.Replace('\\', '/').TrimStart('/').Trim();
    }

    /// <summary>Ruta absoluta del script (projectDirectory + path relativo).</summary>
    public string GetFullPath(string relativePath)
    {
        var key = NormalizeRelativePath(relativePath);
        return Path.Combine(_projectDirectory, key.Length > 0 ? key : relativePath?.TrimStart(Path.DirectorySeparatorChar, '/') ?? "");
    }

    /// <summary>Carga el código del script. Usa caché; invocar InvalidateCache(path) antes para hot reload.</summary>
    public string LoadSource(string relativePath)
    {
        var key = NormalizeRelativePath(relativePath);
        var fullPath = GetFullPath(key);
        if (_cache.TryGetValue(key, out var cached))
            return cached;
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Script no encontrado: {relativePath}", fullPath);
        var source = File.ReadAllText(fullPath, Encoding.UTF8);
        _cache[key] = source;
        return source;
    }

    public void InvalidateCache(string relativePath)
    {
        _cache.Remove(NormalizeRelativePath(relativePath));
    }

    public void InvalidateAllCache()
    {
        _cache.Clear();
    }
}
