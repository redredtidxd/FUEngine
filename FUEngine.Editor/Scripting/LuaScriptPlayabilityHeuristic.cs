using System.Text.RegularExpressions;

namespace FUEngine.Editor;

/// <summary>
/// Comprobación ligera (no ejecuta Lua) para avisar si faltan ganchos típicos del motor.
/// </summary>
public static class LuaScriptPlayabilityHeuristic
{
    private static readonly Regex FunctionOnStart = new(@"\bfunction\s+onStart\b", RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    private static readonly Regex FunctionOnUpdate = new(@"\bfunction\s+onUpdate\b", RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    private static readonly Regex AssignOnStart = new(@"\bonStart\s*=\s*function\b", RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    private static readonly Regex AssignOnUpdate = new(@"\bonUpdate\s*=\s*function\b", RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Devuelve un texto de aviso si no se detectan <c>onStart</c> y <c>onUpdate</c> (no bloquea asignar el script).
    /// </summary>
    public static string? TryGetLifecycleWarning(string? luaSource)
    {
        if (string.IsNullOrWhiteSpace(luaSource))
            return "Script vacío: no se detectó código.";

        var hasStart = FunctionOnStart.IsMatch(luaSource) || AssignOnStart.IsMatch(luaSource);
        var hasUpdate = FunctionOnUpdate.IsMatch(luaSource) || AssignOnUpdate.IsMatch(luaSource);
        if (hasStart && hasUpdate)
            return null;

        return "Aviso: no se detectaron onStart y onUpdate como funciones (el motor las llama en Play si existen). Puedes seguir usando el script.";
    }
}
