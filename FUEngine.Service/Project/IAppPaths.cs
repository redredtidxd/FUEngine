namespace FUEngine.Service.Project;

/// <summary>
/// Rutas de la aplicación bajo <c>%LocalAppData%/FUEngine</c>: configuración global,
/// historial del Hub, logs, cachés, plantillas globales, extensiones, etc.
/// Abstrae el layout de disco para que los consumidores no dependan de rutas
/// codificadas en duro.
/// </summary>
public interface IAppPaths
{
    string Root { get; }
    string ConfigDirectory { get; }
    string UserPreferencesPath { get; }
    string StorageDirectory { get; }
    string ProjectHistoryPath { get; }
    string LogsDirectory { get; }
    string GlobalTemplatesDirectory { get; }
    string ProjectThumbnailsDirectory { get; }
    string LuaMetadataCacheDirectory { get; }
    string ExtensionsDirectory { get; }
    string VulkanCacheDirectory { get; }

    void EnsureLayout();
    string GetThumbnailPathForProjectJson(string projectJsonPath);
}
