using System.IO;
using System.Linq;
using System.Text.Json;
using FUEngine.Editor;

namespace FUEngine;

/// <summary>Datos agregados para el Hub (inicio sin proyecto abierto): mapas, autoguardados, biblioteca, log.</summary>
internal static class StartupHubHelpers
{
    /// <summary>Último autoguardado de mapa entre proyectos recientes (carpeta Autoguardados/Mapa).</summary>
    public static (string? ProjectName, DateTime? UtcTime) FindLatestAutosaveAmongRecents()
    {
        DateTime? best = null;
        string? name = null;
        foreach (var p in StartupService.LoadRecentProjects().Take(40))
        {
            if (string.IsNullOrEmpty(p.Path) || !File.Exists(p.Path)) continue;
            var dir = Path.GetDirectoryName(p.Path);
            if (string.IsNullOrEmpty(dir)) continue;
            var mapDir = Path.Combine(dir, "Autoguardados", "Mapa");
            if (!Directory.Exists(mapDir)) continue;
            foreach (var f in Directory.GetFiles(mapDir, "*_mapa.json"))
            {
                DateTime t;
                try { t = File.GetLastWriteTimeUtc(f); }
                catch { continue; }
                if (best == null || t > best.Value)
                {
                    best = t;
                    name = string.IsNullOrWhiteSpace(p.Name) ? Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar)) : p.Name;
                }
            }
        }
        return (name, best);
    }

    public static string FormatAgo(DateTime utcTime)
    {
        var diff = DateTime.UtcNow - utcTime;
        if (diff.TotalMinutes < 1) return "hace un momento";
        if (diff.TotalHours < 1) return $"hace {(int)diff.TotalMinutes} min";
        if (diff.TotalDays < 1) return $"hace {(int)diff.TotalHours} h";
        if (diff.TotalDays < 2) return "ayer";
        return $"hace {(int)diff.TotalDays} días";
    }

    /// <summary>Snapshot más reciente en <c>snapshots/</c> (mapa temporal del editor).</summary>
    public static string? FindLatestSnapshotMapPath(string projectDir)
    {
        if (string.IsNullOrEmpty(projectDir)) return null;
        var snapDir = Path.Combine(projectDir, "snapshots");
        if (!Directory.Exists(snapDir)) return null;
        FileInfo? best = null;
        foreach (var f in Directory.GetFiles(snapDir, "*_mapa.json"))
        {
            FileInfo fi;
            try { fi = new FileInfo(f); }
            catch { continue; }
            if (best == null || fi.LastWriteTimeUtc > best.LastWriteTimeUtc) best = fi;
        }
        return best?.FullName;
    }

    /// <summary>Primera escena con mapa existente, o <c>mapa.json</c> legacy.</summary>
    public static string? ResolvePrimaryMapPath(string projectFilePath, string projectDir)
    {
        try
        {
            var json = File.ReadAllText(projectFilePath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("scenes", out var scenes) && scenes.ValueKind == JsonValueKind.Array)
            {
                foreach (var sc in scenes.EnumerateArray())
                {
                    if (!sc.TryGetProperty("mapPathRelative", out var mp)) continue;
                    var rel = mp.GetString();
                    if (string.IsNullOrWhiteSpace(rel)) continue;
                    var full = Path.Combine(projectDir, rel.Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(full)) return full;
                }
            }
            var legacy = Path.Combine(projectDir, "mapa.json");
            if (File.Exists(legacy)) return legacy;
        }
        catch { /* ignore */ }
        return null;
    }

    /// <summary>Cuenta entradas de biblioteca global: «texturas» (tileset, sprite, image, ui) y scripts (.lua en manifiesto).</summary>
    public static (int Textures, int Scripts) CountGlobalLibraryKinds(GlobalLibraryManifestDto manifest)
    {
        int tex = 0, lua = 0;
        foreach (var e in manifest.Entries)
        {
            var k = e.Kind ?? "";
            if (string.Equals(k, GlobalLibraryKinds.Tileset, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(k, GlobalLibraryKinds.Sprite, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(k, GlobalLibraryKinds.Image, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(k, GlobalLibraryKinds.Ui, StringComparison.OrdinalIgnoreCase))
                tex++;
            var rel = e.RelativePath ?? "";
            if (rel.EndsWith(".lua", StringComparison.OrdinalIgnoreCase)) lua++;
        }
        return (tex, lua);
    }

    /// <summary>Errores y críticos en el log de sesión del día (archivo en LocalApplicationData).</summary>
    public static (int Errors, int Critical) CountErrorLinesInTodaySessionLog()
    {
        var path = EditorLog.SessionLogFilePath;
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return (0, 0);
        int err = 0, crit = 0;
        try
        {
            foreach (var line in File.ReadLines(path))
            {
                if (line.Contains("[Error]", StringComparison.Ordinal)) err++;
                else if (line.Contains("[Critical]", StringComparison.Ordinal)) crit++;
            }
        }
        catch { /* ignore */ }
        return (err, crit);
    }
}
