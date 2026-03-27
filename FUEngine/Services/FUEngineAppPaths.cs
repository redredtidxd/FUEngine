using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FUEngine;

/// <summary>
/// Rutas bajo <c>%LocalAppData%/FUEngine</c>: configuración global del editor (no del juego),
/// historial del Hub, logs, cachés y carpetas reservadas (plantillas globales, extensiones, Vulkan).
/// </summary>
public static class FUEngineAppPaths
{
    public static string Root =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FUEngine");

    public static string ConfigDirectory => Path.Combine(Root, "Config");

    /// <summary>Preferencias del editor (tema, idioma, rutas por defecto, atajos…). Sustituye al antiguo <c>settings.json</c> en la raíz.</summary>
    public static string UserPreferencesPath => Path.Combine(ConfigDirectory, "user_preferences.json");

    /// <summary>Compatibilidad: antes de la reorganización el archivo estaba en la raíz de AppData.</summary>
    public static string LegacySettingsPath => Path.Combine(Root, "settings.json");

    public static string StorageDirectory => Path.Combine(Root, "Storage");

    /// <summary>Lista de proyectos recientes del Hub (JSON). Sustituye al antiguo <c>recent.json</c>.</summary>
    public static string ProjectHistoryPath => Path.Combine(StorageDirectory, "project_history.json");

    public static string LegacyRecentPath => Path.Combine(Root, "recent.json");

    /// <summary>Logs de sesión y reportes de fallo (misma convención que antes: carpeta en minúsculas).</summary>
    public static string LogsDirectory => Path.Combine(Root, "logs");

    /// <summary>Seeds/prefabs exportados por el usuario para reutilizar entre proyectos (reservado).</summary>
    public static string GlobalTemplatesDirectory => Path.Combine(Root, "GlobalTemplates");

    /// <summary>Miniaturas PNG del mapa para el Hub (una por proyecto).</summary>
    public static string ProjectThumbnailsDirectory => Path.Combine(Root, "ProjectThumbs");

    public static string LuaMetadataCacheDirectory => Path.Combine(Root, "Cache", "LuaMetadata");

    public static string ExtensionsDirectory => Path.Combine(Root, "Extensions");

    /// <summary>Reservado para caché de pipelines u otros datos gráficos del runtime/editor.</summary>
    public static string VulkanCacheDirectory => Path.Combine(Root, "Vulkan");

    /// <summary>Crea las carpetas esperadas si no existen.</summary>
    public static void EnsureLayout()
    {
        foreach (var d in new[]
                 {
                     ConfigDirectory, StorageDirectory, LogsDirectory,
                     GlobalTemplatesDirectory, ProjectThumbnailsDirectory,
                     Path.Combine(Root, "Cache"), LuaMetadataCacheDirectory,
                     ExtensionsDirectory, VulkanCacheDirectory
                 })
        {
            try { Directory.CreateDirectory(d); } catch { /* no bloquear arranque */ }
        }
    }

    /// <summary>Ruta estable para la miniatura del Hub a partir de la ruta absoluta de <c>proyecto.json</c> / <c>.fue</c>.</summary>
    public static string GetThumbnailPathForProjectJson(string projectJsonPath)
    {
        var full = Path.GetFullPath(projectJsonPath.Trim());
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(full));
        var hex = Convert.ToHexString(bytes).ToLowerInvariant();
        return Path.Combine(ProjectThumbnailsDirectory, hex[..16] + ".png");
    }
}
