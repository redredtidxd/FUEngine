using System.IO;
using System.Windows.Threading;
using FUEngine.Editor;

namespace FUEngine;

/// <summary>
/// Autoguardado de mapa y objetos en carpeta separada (Autoguardados/Mapa, Autoguardados/Objetos), con archivos .tmp que se renombran a .json al completar.
/// No reemplaza el guardado oficial; limita el número de backups por tipo (elimina los más antiguos).
/// GuardarSoloCambios: ya se respeta — solo se ejecuta cuando hay cambios sin guardar (HasUnsavedChanges).
/// Autoguardado incremental/diferencial (solo guardar deltas o chunks modificados) requeriría serialización por chunks y detección de cambios; de momento se guarda el estado completo por tipo.
/// </summary>
public class AutoSaveService
{
    private DispatcherTimer? _timer;
    private string _projectDirectory = "";
    private bool _enabled;
    private int _intervalMinutes;
    private int _maxBackupsPerType;
    private string _folder = "Autoguardados";
    private Func<bool>? _hasUnsavedChanges;
    private Action<string, string>? _saveMapAndObjectsToPaths;
    private Action? _onAfterAutosave;

    public void Configure(
        string projectDirectory,
        bool enabled,
        int intervalMinutes,
        int maxBackupsPerType,
        string folder,
        Func<bool> hasUnsavedChanges,
        Action<string, string> saveMapAndObjectsToPaths,
        Action? onAfterAutosave = null)
    {
        Stop();
        _projectDirectory = projectDirectory ?? "";
        _enabled = enabled;
        _intervalMinutes = intervalMinutes <= 0 ? 0 : Math.Clamp(intervalMinutes, 1, 120);
        _maxBackupsPerType = Math.Clamp(maxBackupsPerType, 1, 100);
        _folder = string.IsNullOrWhiteSpace(folder) ? "Autoguardados" : folder.Trim();
        _hasUnsavedChanges = hasUnsavedChanges;
        _saveMapAndObjectsToPaths = saveMapAndObjectsToPaths;
        _onAfterAutosave = onAfterAutosave;
        if (_enabled && _intervalMinutes > 0)
        {
            _timer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMinutes(_intervalMinutes)
            };
            _timer.Tick += (_, _) => ExecuteAutosave();
            _timer.Start();
        }
    }

    public void Stop()
    {
        _timer?.Stop();
        _timer = null;
    }

    /// <summary>Ejecuta un autoguardado inmediato (p. ej. al cerrar si hay cambios).</summary>
    public void ExecuteAutosave()
    {
        if (string.IsNullOrEmpty(_projectDirectory) || _saveMapAndObjectsToPaths == null) return;
        if (_hasUnsavedChanges != null && !_hasUnsavedChanges()) return;

        var basePath = GetAutosaveFolderPath();
        var mapSubdir = Path.Combine(basePath, "Mapa");
        var objSubdir = Path.Combine(basePath, "Objetos");
        try
        {
            if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);
            if (!Directory.Exists(mapSubdir)) Directory.CreateDirectory(mapSubdir);
            if (!Directory.Exists(objSubdir)) Directory.CreateDirectory(objSubdir);
        }
        catch
        {
            return;
        }

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var mapTmp = Path.Combine(mapSubdir, $"{timestamp}_mapa.tmp");
        var objTmp = Path.Combine(objSubdir, $"{timestamp}_objetos.tmp");
        var mapJson = Path.Combine(mapSubdir, $"{timestamp}_mapa.json");
        var objJson = Path.Combine(objSubdir, $"{timestamp}_objetos.json");

        try
        {
            _saveMapAndObjectsToPaths(mapTmp, objTmp);
            if (File.Exists(mapTmp))
                File.Move(mapTmp, mapJson, overwrite: true);
            if (File.Exists(objTmp))
                File.Move(objTmp, objJson, overwrite: true);
            PruneOldBackups(mapSubdir, "*_mapa.json", _maxBackupsPerType);
            PruneOldBackups(objSubdir, "*_objetos.json", _maxBackupsPerType);
            _onAfterAutosave?.Invoke();
        }
        catch (Exception ex)
        {
            EditorLog.Warning($"Autoguardado: {ex.Message}", "Autoguardado");
        }
    }

    private string GetAutosaveFolderPath()
    {
        if (string.IsNullOrEmpty(_folder)) return Path.Combine(_projectDirectory, "Autoguardados");
        if (Path.IsPathRooted(_folder)) return _folder;
        return Path.Combine(_projectDirectory, _folder);
    }

    private static void PruneOldBackups(string folderPath, string pattern, int keepCount)
    {
        try
        {
            if (!Directory.Exists(folderPath)) return;
            var files = Directory.GetFiles(folderPath, pattern);
            if (files.Length <= keepCount) return;
            var ordered = files
                .Select(f => new FileInfo(f))
                .OrderByDescending(fi => fi.CreationTimeUtc)
                .ToList();
            foreach (var fi in ordered.Skip(keepCount))
            {
                try { fi.Delete(); } catch { /* ignore */ }
            }
        }
        catch { /* ignore */ }
    }
}
