using System.Collections.ObjectModel;
using System.IO;

namespace FUEngine;

/// <summary>Nivel de mensaje en la consola del editor.</summary>
public enum LogLevel
{
    Info,
    Warning,
    Error,
    /// <summary>Fallos graves (JSON corrupto, invariantes rotas).</summary>
    Critical,
    /// <summary>Salida de <c>print()</c> en Lua (filtrable aparte de Info).</summary>
    Lua
}

/// <summary>Entrada del log (mensaje con nivel, categoría y opcionalmente archivo/línea).</summary>
public class LogEntry
{
    public DateTime Time { get; set; }
    public LogLevel Level { get; set; }
    /// <summary>Texto del mensaje sin prefijo de tiempo/categoría.</summary>
    public string Message { get; set; } = "";
    /// <summary>Categoría u origen (antes «Source»): Lua, Play, Editor, IO, …</summary>
    public string? Source { get; set; }
    public string? FilePath { get; set; }
    public int? Line { get; set; }

    /// <summary>Línea completa para copiar o depuración: [HH:mm:ss][categoría][nivel] mensaje</summary>
    public string FormattedLine =>
        $"[{Time:HH:mm:ss}][{Source ?? "General"}][{Level}] {Message}";
}

/// <summary>
/// Único punto de entrada recomendado para escribir en la consola del editor.
/// </summary>
public static class EditorLog
{
    private static readonly ObservableCollection<LogEntry> _entries = new();
    public static ReadOnlyObservableCollection<LogEntry> Entries { get; } = new(_entries);

    public static int MaxEntries { get; set; } = 500;

    /// <summary>Si true, añade cada línea a <see cref="SessionLogFilePath"/> (errores y crashes).</summary>
    public static bool EnableFileLogging { get; set; } = true;

    /// <summary>Ruta del .log de sesión (LocalApplicationData/FUEngine/logs).</summary>
    public static string SessionLogFilePath { get; private set; } = "";

    /// <summary>Carpeta donde se guardan los .log de sesión (<c>LocalApplicationData/FUEngine/logs</c>).</summary>
    public static string LogsDirectory
    {
        get
        {
            if (!string.IsNullOrEmpty(SessionLogFilePath))
            {
                var d = Path.GetDirectoryName(SessionLogFilePath);
                if (!string.IsNullOrEmpty(d)) return d;
            }
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FUEngine", "logs");
        }
    }

    /// <summary>Vacía el archivo de log de sesión del día en disco (la consola del editor en memoria no se modifica).</summary>
    public static bool TryClearSessionLogFile()
    {
        try
        {
            if (string.IsNullOrEmpty(SessionLogFilePath)) return false;
            var dir = Path.GetDirectoryName(SessionLogFilePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(SessionLogFilePath, "");
            return true;
        }
        catch
        {
            return false;
        }
    }

    static EditorLog()
    {
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FUEngine", "logs");
            Directory.CreateDirectory(dir);
            SessionLogFilePath = Path.Combine(dir, $"session_{DateTime.Now:yyyyMMdd}.log");
        }
        catch
        {
            SessionLogFilePath = "";
        }
    }

    public static event EventHandler<LogEntry>? EntryAdded;
    public static event EventHandler<(string Message, LogLevel Level)>? ToastRequested;

    private static string? _lastToastMessage;
    private static LogLevel _lastToastLevel;
    private static DateTime _lastToastTime = DateTime.MinValue;
    private static int _lastToastCount;

    /// <summary>Se invoca al solicitar abrir un archivo en una línea (doble clic en consola).</summary>
    public static event EventHandler<(string FilePath, int Line)>? RequestOpenFileAtLine;

    public static void RaiseRequestOpenFileAtLine(string filePath, int line) =>
        RequestOpenFileAtLine?.Invoke(null, (filePath, line));

    /// <summary>
    /// Punto de entrada único: formatea, añade a la colección de la UI y opcionalmente al archivo de sesión.
    /// </summary>
    public static void Log(string message, LogLevel level = LogLevel.Info, string category = "General")
    {
        AddCore(level, message, string.IsNullOrWhiteSpace(category) ? "General" : category.Trim(), null, null);
    }

    /// <summary>Error con ruta y línea (abrir en editor desde la consola).</summary>
    public static void Log(string message, LogLevel level, string category, string? filePath, int? line)
    {
        AddCore(level, message, string.IsNullOrWhiteSpace(category) ? "General" : category.Trim(), filePath, line);
    }

    public static void Info(string message, string? source = null) =>
        Log(message, LogLevel.Info, source ?? "General");

    public static void Warning(string message, string? source = null) =>
        Log(message, LogLevel.Warning, source ?? "General");

    public static void Error(string message, string? source = null) =>
        Log(message, LogLevel.Error, source ?? "General");

    public static void Error(string message, string? source, string? filePath, int? line) =>
        Log(message, LogLevel.Error, source ?? "General", filePath, line);

    public static void Critical(string message, string? source = null) =>
        Log(message, LogLevel.Critical, source ?? "General");

    /// <summary>Muestra toast y registra en el log (agrupa repeticiones breves).</summary>
    public static void Toast(string message, LogLevel level = LogLevel.Info, string? source = null)
    {
        Log(message, level, source ?? "General");
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

    private static void AddCore(LogLevel level, string message, string source, string? filePath, int? line)
    {
        var entry = new LogEntry
        {
            Time = DateTime.Now,
            Level = level,
            Message = message ?? "",
            Source = source,
            FilePath = filePath,
            Line = line
        };

        var formatted = entry.FormattedLine;
        TryAppendFile(formatted);

        void AddToUi()
        {
            _entries.Add(entry);
            while (_entries.Count > MaxEntries)
                _entries.RemoveAt(0);
            EntryAdded?.Invoke(null, entry);
        }

        var disp = System.Windows.Application.Current?.Dispatcher;
        if (disp == null)
            AddToUi();
        else if (disp.CheckAccess())
            AddToUi();
        else
            disp.Invoke(AddToUi);
    }

    private static void TryAppendFile(string line)
    {
        if (!EnableFileLogging || string.IsNullOrEmpty(SessionLogFilePath)) return;
        try
        {
            File.AppendAllText(SessionLogFilePath, line + Environment.NewLine);
        }
        catch
        {
            /* no bloquear el editor si el disco falla */
        }
    }

    public static void Clear()
    {
        void DoClear() => _entries.Clear();
        var disp = System.Windows.Application.Current?.Dispatcher;
        if (disp == null)
            DoClear();
        else if (disp.CheckAccess())
            DoClear();
        else
            disp.Invoke(DoClear);
    }
}
