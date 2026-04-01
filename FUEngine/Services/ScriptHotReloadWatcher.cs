using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FUEngine;

/// <summary>
/// Vigila el proyecto en busca de cambios en <c>.lua</c> (p. ej. guardado desde VS Code) y notifica rutas relativas con debounce.
/// La invocación del callback ocurre en el dispatcher de WPF.
/// </summary>
public sealed class ScriptHotReloadWatcher : IDisposable
{
    private const int DebounceMs = 250;

    private readonly string _projectFullPath;
    private readonly Action<string> _onLuaChanged;
    private readonly System.Windows.Threading.Dispatcher _dispatcher;
    private FileSystemWatcher? _watcher;
    private System.Windows.Threading.DispatcherTimer? _debounceTimer;
    private readonly HashSet<string> _pending = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _pendingLock = new();
    private bool _disposed;

    public ScriptHotReloadWatcher(string projectDirectory, Action<string> onLuaChanged, System.Windows.Threading.Dispatcher? dispatcher = null)
    {
        _onLuaChanged = onLuaChanged ?? throw new ArgumentNullException(nameof(onLuaChanged));
        _dispatcher = dispatcher ?? System.Windows.Application.Current?.Dispatcher
            ?? throw new InvalidOperationException("No hay Dispatcher WPF para hot reload.");
        try
        {
            _projectFullPath = Path.GetFullPath(projectDirectory ?? "");
        }
        catch
        {
            _projectFullPath = "";
        }
    }

    public void Start()
    {
        StopInternal();
        if (string.IsNullOrEmpty(_projectFullPath) || !Directory.Exists(_projectFullPath)) return;

        _debounceTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(DebounceMs) };
        _debounceTimer.Tick += DebounceTimer_OnTick;

        _watcher = new FileSystemWatcher(_projectFullPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
            Filter = "*.lua",
            InternalBufferSize = 65536,
        };
        _watcher.Changed += OnWatcherEvent;
        _watcher.Created += OnWatcherEvent;
        _watcher.Renamed += OnWatcherRenamed;
        _watcher.EnableRaisingEvents = true;
    }

    private void OnWatcherRenamed(object sender, RenamedEventArgs e)
    {
        if (e.FullPath != null && e.FullPath.EndsWith(".lua", StringComparison.OrdinalIgnoreCase))
            EnqueuePath(e.FullPath);
    }

    private void OnWatcherEvent(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType == WatcherChangeTypes.Deleted) return;
        if (e.FullPath != null && e.FullPath.EndsWith(".lua", StringComparison.OrdinalIgnoreCase))
            EnqueuePath(e.FullPath);
    }

    private void EnqueuePath(string fullPath)
    {
        try
        {
            var full = Path.GetFullPath(fullPath);
            if (!full.StartsWith(_projectFullPath, StringComparison.OrdinalIgnoreCase)) return;
            var rel = Path.GetRelativePath(_projectFullPath, full).Replace('\\', '/');
            lock (_pendingLock)
            {
                _pending.Add(rel);
            }

            _dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
            {
                if (_debounceTimer != null)
                {
                    _debounceTimer.Stop();
                    _debounceTimer.Start();
                }
            });
        }
        catch
        {
            /* rutas inválidas u IO */
        }
    }

    private void DebounceTimer_OnTick(object? sender, EventArgs e)
    {
        _debounceTimer?.Stop();
        string[] batch;
        lock (_pendingLock)
        {
            batch = _pending.ToArray();
            _pending.Clear();
        }

        foreach (var rel in batch)
        {
            try
            {
                _onLuaChanged(rel);
            }
            catch
            {
                /* no tumbar el juego por un script suelto */
            }
        }
    }

    private void StopInternal()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnWatcherEvent;
            _watcher.Created -= OnWatcherEvent;
            _watcher.Renamed -= OnWatcherRenamed;
            _watcher.Dispose();
            _watcher = null;
        }

        if (_debounceTimer != null)
        {
            _debounceTimer.Stop();
            _debounceTimer.Tick -= DebounceTimer_OnTick;
            _debounceTimer = null;
        }

        lock (_pendingLock)
        {
            _pending.Clear();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopInternal();
    }
}
