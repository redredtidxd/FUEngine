namespace FUEngine.Runtime;

/// <summary>Formato uniforme de errores de script para consola y <see cref="LuaScriptRuntime.ScriptError"/>.</summary>
public static class LuaErrorFormatter
{
    public static string Format(string path, int line, string message)
    {
        var p = path ?? "";
        var m = message?.Trim() ?? "";
        if (line > 0)
        {
            if (!string.IsNullOrEmpty(m) && m.StartsWith(p, System.StringComparison.OrdinalIgnoreCase) && m.Length > p.Length && m[p.Length] == ':')
                return m;
            return string.IsNullOrEmpty(m) ? $"{p}:{line}" : $"{p}:{line}: {m}";
        }
        return string.IsNullOrEmpty(m) ? p : $"{p}: {m}";
    }
}
