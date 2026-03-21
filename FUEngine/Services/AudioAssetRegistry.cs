using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FUEngine;

/// <summary>Entrada del registro de audio: ID (ruta relativa a assets/audio), path, nombre, duración, tipo.</summary>
public sealed record AudioAssetEntry(string Id, string FullPath, string Name, double DurationSeconds, string Type)
{
    public string TypeIcon => Type?.ToLowerInvariant() switch { "music" => "🎵", "ambient" => "🌫", _ => "🔊" };
}

/// <summary>
/// Registro de assets de audio. Se construye al abrir el proyecto y se actualiza con FileSystemWatcher (debounce).
/// ID = ruta relativa dentro de assets/audio (ej. sfx/jump, music/theme).
/// </summary>
public sealed class AudioAssetRegistry
{
    public const string AudioFolderName = "assets/audio";
    private const int DebounceMs = 200;

    private readonly Dictionary<string, AudioAssetEntry> _byId = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();
    private string? _projectDirectory;
    private FileSystemWatcher? _watcher;
    private System.Windows.Threading.DispatcherTimer? _debounceTimer;

    /// <summary>Se dispara cuando el registro se ha actualizado (debounce aplicado).</summary>
    public event Action? RegistryChanged;

    public IReadOnlyList<AudioAssetEntry> GetAll()
    {
        lock (_lock)
            return _byId.Values.OrderBy(x => x.Id).ToList();
    }

    public AudioAssetEntry? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        lock (_lock)
            return _byId.TryGetValue(id, out var e) ? e : null;
    }

    public bool TryGetPath(string id, out string? fullPath)
    {
        var e = GetById(id);
        fullPath = e?.FullPath;
        return fullPath != null;
    }

    /// <summary>Inicializa y escanea la carpeta de audio. Llamar al abrir el proyecto.</summary>
    public void Initialize(string projectDirectory)
    {
        _projectDirectory = projectDirectory ?? "";
        StopWatching();
        Refresh();
        StartWatching();
    }

    public void Dispose()
    {
        StopWatching();
    }

    private void StartWatching()
    {
        if (string.IsNullOrEmpty(_projectDirectory)) return;
        var audioPath = Path.Combine(_projectDirectory, AudioFolderName);
        if (!Directory.Exists(audioPath)) return;

        _watcher = new FileSystemWatcher(audioPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
        };
        _watcher.Created += OnFileSystemEvent;
        _watcher.Deleted += OnFileSystemEvent;
        _watcher.Renamed += OnFileSystemEvent;
        _watcher.EnableRaisingEvents = true;

        _debounceTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(DebounceMs)
        };
        _debounceTimer.Tick += (_, _) =>
        {
            _debounceTimer?.Stop();
            Refresh();
            RegistryChanged?.Invoke();
        };
    }

    private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
    {
        // Siempre rescan completo: renombrar/mover carpetas dispara eventos raros; un solo refresh total es más robusto.
        _debounceTimer?.Stop();
        _debounceTimer?.Start();
    }

    private void StopWatching()
    {
        _watcher?.Dispose();
        _watcher = null;
        _debounceTimer?.Stop();
        _debounceTimer = null;
    }

    /// <summary>Rescan completo del directorio (siempre, no solo el archivo cambiado) para ser robusto ante rename/move.</summary>
    private void Refresh()
    {
        var projectDir = _projectDirectory ?? "";
        var audioPath = Path.Combine(projectDir, AudioFolderName);
        var newEntries = new Dictionary<string, AudioAssetEntry>(StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(audioPath))
        {
            lock (_lock)
            {
                _byId.Clear();
            }
            return;
        }

        var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".wav", ".ogg", ".mp3" };
        foreach (var file in Directory.EnumerateFiles(audioPath, "*.*", SearchOption.AllDirectories))
        {
            if (!extensions.Contains(Path.GetExtension(file))) continue;
            var relativeToAudio = Path.GetRelativePath(audioPath, file);
            var id = relativeToAudio.Replace('\\', '/');
            var withoutExt = Path.GetFileNameWithoutExtension(file);
            if (string.IsNullOrEmpty(id)) continue;
            var type = InferType(relativeToAudio);
            var entry = new AudioAssetEntry(id, file, Path.GetFileName(file), 0, type);
            newEntries[entry.Id] = entry;
        }

        lock (_lock)
        {
            _byId.Clear();
            foreach (var kv in newEntries)
                _byId[kv.Key] = kv.Value;
        }
    }

    private static string InferType(string relativePath)
    {
        var parts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (parts.Length > 1)
        {
            var first = parts[0].ToLowerInvariant();
            if (first == "sfx") return "sfx";
            if (first == "music") return "music";
            if (first == "ambient") return "ambient";
        }
        return "sfx";
    }
}
