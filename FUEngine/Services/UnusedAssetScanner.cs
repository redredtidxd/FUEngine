using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FUEngine.Editor;

namespace FUEngine;

/// <summary>
/// Escanea el proyecto y devuelve rutas de archivos que no están referenciados por mapa, objetos, scripts ni seeds.
/// </summary>
public static class UnusedAssetScanner
{
    public static HashSet<string> GetAllProjectFiles(string projectDirectory)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(projectDirectory) || !Directory.Exists(projectDirectory))
            return set;
        foreach (var file in Directory.GetFiles(projectDirectory, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(file);
            if (name != null && (name.StartsWith(".", StringComparison.Ordinal) || name.Equals("snapshots", StringComparison.OrdinalIgnoreCase)))
                continue;
            set.Add(file);
        }
        return set;
    }

    public static HashSet<string> GetAllReferencedPaths(string projectDirectory)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(projectDirectory)) return set;

        var mapPath = Path.Combine(projectDirectory, "mapa.json");
        if (File.Exists(mapPath)) set.Add(mapPath);

        var objPath = Path.Combine(projectDirectory, "objetos.json");
        if (File.Exists(objPath))
        {
            set.Add(objPath);
            try
            {
                var json = File.ReadAllText(objPath);
                var dto = JsonSerializer.Deserialize<ObjectsDto>(json);
                foreach (var d in dto?.Definitions ?? new List<ObjectDefinitionDto>())
                {
                    if (!string.IsNullOrWhiteSpace(d.SpritePath))
                        set.Add(Path.GetFullPath(Path.Combine(projectDirectory, d.SpritePath)));
                }
            }
            catch { /* ignore */ }
        }

        var scriptsPath = Path.Combine(projectDirectory, "scripts.json");
        if (File.Exists(scriptsPath)) set.Add(scriptsPath);

        var animPath = Path.Combine(projectDirectory, "animaciones.json");
        if (File.Exists(animPath)) set.Add(animPath);

        var seedsPath = Path.Combine(projectDirectory, "seeds.json");
        if (File.Exists(seedsPath)) set.Add(seedsPath);
        var prefabsPath = Path.Combine(projectDirectory, "prefabs.json");
        if (File.Exists(prefabsPath)) set.Add(prefabsPath);

        foreach (var file in Directory.GetFiles(projectDirectory, "*.json", SearchOption.AllDirectories))
        {
            var rel = file.StartsWith(projectDirectory, StringComparison.OrdinalIgnoreCase)
                ? file.Substring(projectDirectory.Length).TrimStart(Path.DirectorySeparatorChar)
                : file;
            if (rel.StartsWith("snapshots", StringComparison.OrdinalIgnoreCase) || Path.GetFileName(file)?.StartsWith(".", StringComparison.Ordinal) == true)
                continue;
            set.Add(file);
        }

        return set;
    }

    public static List<string> GetUnusedFiles(string projectDirectory)
    {
        var all = GetAllProjectFiles(projectDirectory);
        var referenced = GetAllReferencedPaths(projectDirectory);
        var unused = new List<string>();
        foreach (var path in all)
        {
            if (referenced.Contains(path)) continue;
            var name = Path.GetFileName(path);
            if (name != null && (name.StartsWith(".", StringComparison.Ordinal) || name.Equals(FUEngine.Editor.NewProjectStructure.ProjectFileName, StringComparison.OrdinalIgnoreCase) || name.Equals("Project.json", StringComparison.OrdinalIgnoreCase) || name.Equals("proyecto.json", StringComparison.OrdinalIgnoreCase)))
                continue;
            unused.Add(path);
        }
        return unused.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
