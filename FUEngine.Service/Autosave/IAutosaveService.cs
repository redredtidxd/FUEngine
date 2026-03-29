namespace FUEngine.Service.Autosave;

/// <summary>
/// Autoguardado periódico del estado del proyecto (mapa, objetos, escena).
/// La implementación gestiona timers, rotación de backups y escritura atómica
/// (.tmp → .json). Configurable en intervalo, cantidad máxima de copias y carpeta destino.
/// </summary>
public interface IAutosaveService
{
    void Configure(
        string projectDirectory,
        bool enabled,
        int intervalMinutes,
        int maxBackupsPerType,
        string folder,
        Func<bool> hasUnsavedChanges,
        Action<string, string> saveMapAndObjectsToPaths,
        Action? onAfterAutosave = null);

    void Stop();
    void ExecuteAutosave();
}
