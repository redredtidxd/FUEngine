using System.IO;
using System.Linq;
using System.Text.Json;
using FUEngine.Editor;

namespace FUEngine;

/// <summary>
/// Gestiona proyectos recientes y autodetección. Los datos se guardan en AppData
/// para que persistan al actualizar el ejecutable del motor.
/// </summary>
public static class StartupService
{
    /// <summary>Ruta en AppData (no junto al .exe) para que actualizar el motor no borre la lista.</summary>
    private static string RecentPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FUEngine", "recent.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>Tipos de proyecto para filtros rápidos.</summary>
    public static readonly string[] ProjectTypeFilters = { "Todos", "Pixel Art", "RPG", "Plataforma", "FPS", "Shooter", "Puzzle", "Otro" };

    /// <summary>Etiquetas sugeridas para proyectos.</summary>
    public static readonly string[] SuggestedTags = { "Prototipo", "Trabajo", "Experimento", "Demo", "Personal", "Jam" };

    public static List<RecentProjectInfo> LoadRecentProjects()
    {
        try
        {
            if (!File.Exists(RecentPath)) return new List<RecentProjectInfo>();
            var json = File.ReadAllText(RecentPath);
            var list = JsonSerializer.Deserialize<List<RecentProjectInfo>>(json, JsonOptions);
            list ??= new List<RecentProjectInfo>();
            foreach (var p in list)
            {
                if (p.Tags == null) p.Tags = new List<string>();
            }
            return list;
        }
        catch { return new List<RecentProjectInfo>(); }
    }

    /// <summary>Devuelve el proyecto más reciente (primero de la lista) o null si no hay ninguno.</summary>
    public static RecentProjectInfo? LoadMostRecent() =>
        LoadRecentProjects().FirstOrDefault();

    /// <summary>Añade o actualiza un proyecto en la lista y guarda (path = ruta a proyecto.json).</summary>
    public static void AddRecentProject(string projectPath, string name, string? description, string? engineVersion = null)
    {
        var list = LoadRecentProjects();
        var existing = list.FirstOrDefault(x => string.Equals(x.Path, projectPath, StringComparison.OrdinalIgnoreCase));
        var wasPinned = existing?.IsPinned ?? false;
        var tags = existing?.Tags ?? new List<string>();
        var projectType = existing?.ProjectType;
        var resolution = existing?.Resolution;
        var fps = existing?.Fps;
        list.RemoveAll(x => string.Equals(x.Path, projectPath, StringComparison.OrdinalIgnoreCase));
        var newItem = new RecentProjectInfo
        {
            Path = projectPath,
            Name = name,
            Description = description,
            LastOpened = DateTime.Now,
            OpenedWithEngineVersion = engineVersion ?? FUEngine.Core.EngineVersion.Current,
            IsPinned = wasPinned,
            Tags = new List<string>(tags),
            ProjectType = projectType,
            Resolution = resolution,
            Fps = fps
        };
        RefreshProjectStats(newItem);
        list.Insert(0, newItem);
        const int max = 25;
        if (list.Count > max) list = list.Take(max).ToList();
        SaveRecentList(list);
    }

    /// <summary>Invierte el estado de fijado del proyecto y guarda. Los fijados se ordenan primero.</summary>
    public static void TogglePin(string projectPath)
    {
        var list = LoadRecentProjects();
        var idx = list.FindIndex(x => string.Equals(x.Path, projectPath, StringComparison.OrdinalIgnoreCase));
        if (idx < 0) return;
        list[idx].IsPinned = !list[idx].IsPinned;
        list = list.OrderByDescending(x => x.IsPinned).ThenByDescending(x => x.LastOpened).ToList();
        SaveRecentList(list);
    }

    /// <summary>Establece las etiquetas de un proyecto y guarda.</summary>
    public static void SetTags(string projectPath, List<string> tags)
    {
        var list = LoadRecentProjects();
        var p = list.FirstOrDefault(x => string.Equals(x.Path, projectPath, StringComparison.OrdinalIgnoreCase));
        if (p == null) return;
        p.Tags = new List<string>(tags ?? new List<string>());
        SaveRecentList(list);
    }

    /// <summary>Establece el tipo/género del proyecto y guarda.</summary>
    public static void SetProjectType(string projectPath, string? projectType)
    {
        var list = LoadRecentProjects();
        var p = list.FirstOrDefault(x => string.Equals(x.Path, projectPath, StringComparison.OrdinalIgnoreCase));
        if (p == null) return;
        p.ProjectType = string.IsNullOrWhiteSpace(projectType) ? null : projectType;
        SaveRecentList(list);
    }

    /// <summary>Rellena LastModified, ProjectSizeBytes, SceneCount, AssetsSizeBytes leyendo del disco.</summary>
    public static void RefreshProjectStats(RecentProjectInfo item)
    {
        if (item == null || string.IsNullOrEmpty(item.Path)) return;
        try
        {
            var projectDir = System.IO.Path.GetDirectoryName(item.Path) ?? item.Path;
            if (File.Exists(item.Path))
            {
                var fi = new FileInfo(item.Path);
                item.LastModified = fi.LastWriteTimeUtc;
                item.ProjectSizeBytes = fi.Length;
            }
            item.SceneCount = 0;
            item.ObjectCount = 0;
            item.AssetsSizeBytes = 0;
            if (Directory.Exists(projectDir))
            {
                try
                {
                    var projJson = File.ReadAllText(item.Path);
                    using var doc = JsonDocument.Parse(projJson);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("scenes", out var scenes) && scenes.ValueKind == JsonValueKind.Array)
                        item.SceneCount = scenes.GetArrayLength();
                    if (item.Resolution == null && root.TryGetProperty("gameResolutionWidth", out var w) && root.TryGetProperty("gameResolutionHeight", out var h))
                        item.Resolution = $"{w.GetInt32()}×{h.GetInt32()}";
                    if (item.Fps == null && root.TryGetProperty("fps", out var fpsEl))
                        item.Fps = fpsEl.GetInt32();
                    item.ObjectCount = CountObjectInstancesInProjectJson(root, projectDir);
                }
                catch { /* use 0 */ }
                foreach (var dir in new[] { "Assets", "Sprites", "Maps", "Scripts", "Seeds" })
                {
                    var full = System.IO.Path.Combine(projectDir, dir);
                    if (Directory.Exists(full))
                        item.AssetsSizeBytes += new DirectoryInfo(full).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
                }
            }
        }
        catch { /* leave defaults */ }
    }

    /// <summary>Estadísticas para el HUD: total, abiertos hoy, abiertos en últimos 7 días.</summary>
    public static (int Total, int OpenedToday, int Last7Days) GetStats(List<RecentProjectInfo>? list = null)
    {
        list ??= LoadRecentProjects();
        var now = DateTime.Now.Date;
        var today = list.Count(x => x.LastOpened.Date == now);
        var last7 = list.Count(x => (now - x.LastOpened.Date).TotalDays <= 7);
        return (list.Count, today, last7);
    }

    /// <summary>Quita una ruta de la lista de recientes y guarda (p. ej. cuando el archivo ya no existe).</summary>
    public static void RemoveFromRecent(string projectPath)
    {
        var list = LoadRecentProjects();
        list.RemoveAll(x => string.Equals(x.Path, projectPath, StringComparison.OrdinalIgnoreCase));
        SaveRecentList(list);
    }

    /// <summary>Guarda la lista en AppData (persiste entre actualizaciones del motor).</summary>
    public static void SaveRecentList(List<RecentProjectInfo> list)
    {
        try
        {
            var dir = Path.GetDirectoryName(RecentPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(RecentPath, JsonSerializer.Serialize(list, JsonOptions));
        }
        catch { /* ignore */ }
    }

    /// <summary>Busca proyecto.json en la carpeta (y un nivel de subcarpetas) y devuelve proyectos detectados.</summary>
    public static List<RecentProjectInfo> DiscoverProjects(string directory, int maxDepth = 2)
    {
        var result = new List<RecentProjectInfo>();
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory)) return result;
        try
        {
            DiscoverProjectsRecursive(directory, 0, maxDepth, result);
        }
        catch { /* ignore */ }
        return result;
    }

    private static void DiscoverProjectsRecursive(string dir, int depth, int maxDepth, List<RecentProjectInfo> result)
    {
        if (depth > maxDepth) return;
        try
        {
            var fuePath = Path.Combine(dir, NewProjectStructure.ProjectFileName);
            var proyectoPath = Path.Combine(dir, "proyecto.json");
            var projectJsonPath = Path.Combine(dir, "Project.json");
            var projectPath = File.Exists(fuePath) ? fuePath : File.Exists(proyectoPath) ? proyectoPath : File.Exists(projectJsonPath) ? projectJsonPath : null;
            if (projectPath != null)
            {
                var name = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                string? description = null;
                try
                {
                    var json = File.ReadAllText(projectPath);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("nombre", out var n)) name = n.GetString() ?? name;
                    if (doc.RootElement.TryGetProperty("descripcion", out var d)) description = d.GetString();
                }
                catch { /* use defaults */ }
                result.Add(new RecentProjectInfo
                {
                    Path = projectPath,
                    Name = name ?? "Proyecto",
                    Description = description,
                    LastOpened = File.GetLastWriteTimeUtc(projectPath),
                    OpenedWithEngineVersion = null
                });
                return;
            }
            foreach (var sub in Directory.GetDirectories(dir))
                DiscoverProjectsRecursive(sub, depth + 1, maxDepth, result);
        }
        catch { /* ignore */ }
    }

    /// <summary>Fusiona la lista de recientes con proyectos detectados (sin duplicar por ruta) y ordena por última apertura.</summary>
    public static List<RecentProjectInfo> MergeWithDiscovered(List<RecentProjectInfo> recent, List<RecentProjectInfo> discovered)
    {
        var byPath = new Dictionary<string, RecentProjectInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in recent)
            byPath[r.Path] = r;
        foreach (var d in discovered)
        {
            if (!byPath.ContainsKey(d.Path))
                byPath[d.Path] = d;
        }
        return byPath.Values.OrderByDescending(x => x.LastOpened).ToList();
    }

    /// <summary>Suma instancias en <c>objetos.json</c> de cada escena (o legacy único).</summary>
    private static int CountObjectInstancesInProjectJson(JsonElement root, string projectDir)
    {
        var total = 0;
        try
        {
            if (root.TryGetProperty("scenes", out var scenes) && scenes.ValueKind == JsonValueKind.Array && scenes.GetArrayLength() > 0)
            {
                foreach (var sc in scenes.EnumerateArray())
                {
                    if (!sc.TryGetProperty("objectsPathRelative", out var op)) continue;
                    var rel = op.GetString();
                    if (string.IsNullOrWhiteSpace(rel)) continue;
                    var path = Path.Combine(projectDir, rel.Replace('/', Path.DirectorySeparatorChar));
                    total += CountInstancesInObjectsFile(path);
                }
                return total;
            }
            if (root.TryGetProperty("mainObjectsPath", out var mp))
            {
                var rel = mp.GetString();
                if (!string.IsNullOrWhiteSpace(rel))
                {
                    var path = Path.Combine(projectDir, rel.Replace('/', Path.DirectorySeparatorChar));
                    total += CountInstancesInObjectsFile(path);
                }
            }
            if (total == 0)
            {
                var legacy = Path.Combine(projectDir, "objetos.json");
                total += CountInstancesInObjectsFile(legacy);
            }
        }
        catch { /* ignore */ }
        return total;
    }

    private static int CountInstancesInObjectsFile(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return 0;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (doc.RootElement.TryGetProperty("instances", out var inst) && inst.ValueKind == JsonValueKind.Array)
                return inst.GetArrayLength();
        }
        catch { /* ignore */ }
        return 0;
    }
}
