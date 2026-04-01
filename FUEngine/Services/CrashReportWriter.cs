using System.IO;
using System.Text;

namespace FUEngine;

/// <summary>Escribe un .txt de «autopsia» en <see cref="FUEngineAppPaths.LogsDirectory"/> para informes de bug (GitHub).</summary>
public static class CrashReportWriter
{
    public static void TryWrite(string source, Exception? ex, bool isTerminating = false)
    {
        try
        {
            FUEngineAppPaths.EnsureLayout();
            var name = $"crash_{DateTime.Now:yyyyMMdd_HHmmss}_{SanitizeSource(source)}.txt";
            var path = Path.Combine(FUEngineAppPaths.LogsDirectory, name);
            var sb = new StringBuilder(2048);
            sb.AppendLine("FUEngine — informe de fallo");
            sb.AppendLine("---");
            sb.AppendLine($"Origen: {source}");
            sb.AppendLine($"Hora local: {DateTime.Now:O}");
            sb.AppendLine($"Proceso terminando: {isTerminating}");
            sb.AppendLine($"SO: {Environment.OSVersion}");
            sb.AppendLine($"CLR: {Environment.Version}");
            sb.AppendLine($"64-bit proceso: {Environment.Is64BitProcess}");
            sb.AppendLine($"Directorio base: {AppContext.BaseDirectory}");
            sb.AppendLine();
            if (ex != null)
            {
                sb.AppendLine($"{ex.GetType().FullName}: {ex.Message}");
                sb.AppendLine(ex.StackTrace);
                var inner = ex.InnerException;
                var depth = 0;
                while (inner != null && depth++ < 8)
                {
                    sb.AppendLine();
                    sb.AppendLine($"--- Inner ({depth}) ---");
                    sb.AppendLine($"{inner.GetType().FullName}: {inner.Message}");
                    sb.AppendLine(inner.StackTrace);
                    inner = inner.InnerException;
                }
            }
            else
                sb.AppendLine("(sin excepción asociada)");
            sb.AppendLine();
            sb.AppendLine("Adjunta este archivo o el session_*.log más reciente al reportar el bug.");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }
        catch
        {
            /* no bloquear */
        }
    }

    private static string SanitizeSource(string source)
    {
        if (string.IsNullOrEmpty(source)) return "unknown";
        var sb = new StringBuilder(source.Length);
        foreach (var c in source)
        {
            if (char.IsLetterOrDigit(c)) sb.Append(c);
            else if (c is ' ' or '-' or '_') sb.Append('_');
        }
        var s = sb.ToString();
        return s.Length > 48 ? s[..48] : s;
    }
}
