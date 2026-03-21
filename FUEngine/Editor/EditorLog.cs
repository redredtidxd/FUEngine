using System.Collections.ObjectModel;

namespace FUEngine;

/// <summary>
/// Nivel de mensaje del log del editor.
/// </summary>
public enum LogLevel
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Entrada del log (mensaje con nivel, origen y opcionalmente archivo/línea para abrir al hacer clic).
/// </summary>
public class LogEntry
{
    public DateTime Time { get; set; }
    public LogLevel Level { get; set; }
    public string Message { get; set; } = "";
    public string? Source { get; set; }
    /// <summary>Ruta relativa al proyecto (ej: Scripts/player.lua) para abrir en el editor.</summary>
    public string? FilePath { get; set; }
    /// <summary>Línea del error (1-based) para posicionar el cursor.</summary>
    public int? Line { get; set; }
}

/// <summary>
/// Log central del motor/editor: advertencias y errores de mapa, objetos, animaciones, carga, etc.
/// </summary>
public static class EditorLog
{
    private static readonly ObservableCollection<LogEntry> _entries = new();
    public static ReadOnlyObservableCollection<LogEntry> Entries { get; } = new(_entries);

    public static int MaxEntries { get; set; } = 400;

    public static event EventHandler<LogEntry>? EntryAdded;

    /// <summary>Notificación breve tipo toast (mensaje no bloqueante). También se añade al log.</summary>
    public static event EventHandler<(string Message, LogLevel Level)>? ToastRequested;

    private static string? _lastToastMessage;
    private static LogLevel _lastToastLevel;
    private static DateTime _lastToastTime = DateTime.MinValue;
    private static int _lastToastCount;

    /// <summary>Muestra un toast y registra el mensaje en el log. Mensajes repetidos en 2,5 s se agrupan (ej. "Escena creada (x3)"). No bloquea el hilo llamador.</summary>
    public static void Toast(string message, LogLevel level = LogLevel.Info, string? source = null)
    {
        Add(level, message, source);
        System.Windows.Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            var now = DateTime.UtcNow;
            var same = string.Equals(_lastToastMessage, message, StringComparison.Ordinal) && _lastToastLevel == level && (now - _lastToastTime).TotalSeconds < 2.5;
            if (same)
            {
                _lastToastCount++;
                _lastToastTime = now;
                message = _lastToastCount > 1 ? $"{message} (x{_lastToastCount})" : message;
            }
            else
            {
                _lastToastMessage = message;
                _lastToastLevel = level;
                _lastToastTime = now;
                _lastToastCount = 1;
            }
            ToastRequested?.Invoke(null, (message, level));
        });
    }

    /// <summary>Se invoca cuando el usuario solicita abrir un archivo en una línea (ej: doble clic en error de consola). (FilePath relativo, Line 1-based).</summary>
    public static event EventHandler<(string FilePath, int Line)>? RequestOpenFileAtLine;

    /// <summary>Dispara RequestOpenFileAtLine (solo desde la consola al hacer doble clic en una entrada con archivo/línea).</summary>
    public static void RaiseRequestOpenFileAtLine(string filePath, int line)
    {
        RequestOpenFileAtLine?.Invoke(null, (filePath, line));
    }

    public static void Info(string message, string? source = null)
    {
        Add(LogLevel.Info, message, source);
    }

    public static void Warning(string message, string? source = null)
    {
        Add(LogLevel.Warning, message, source);
    }

    public static void Error(string message, string? source = null)
    {
        Add(LogLevel.Error, message, source);
    }

    /// <summary>Registra un error con archivo y línea para poder abrirlo al hacer clic en la consola.</summary>
    public static void Error(string message, string? source, string? filePath, int? line)
    {
        Add(LogLevel.Error, message, source, filePath, line);
    }

    public static void Add(LogLevel level, string message, string? source = null, string? filePath = null, int? line = null)
    {
        var entry = new LogEntry { Time = DateTime.Now, Level = level, Message = message, Source = source, FilePath = filePath, Line = line };
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            _entries.Add(entry);
            while (_entries.Count > MaxEntries)
                _entries.RemoveAt(0);
            EntryAdded?.Invoke(null, entry);
        });
    }

    public static void Clear()
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() => _entries.Clear());
    }
}
