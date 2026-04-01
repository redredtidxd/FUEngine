// -----------------------------------------------------------------------------
// FUEngine (FUEngine.Runtime) — Condiciones: LICENSE.md en la raíz del repositorio.
// Copyright (c) Red Redtid. No es licencia MIT/Apache ni "open source" OSI.
// Productos comerciales: revenue share según LICENSE.md. Prohibido integrar
// publicidad de terceros en forks GitHub sin autorización escrita. Plugins
// gratuitos (no venta de extensiones). Build pública sin garantías ni servidores
// de validación del titular.
// -----------------------------------------------------------------------------
using NLua;
using NLua.Exceptions;

namespace FUEngine.Runtime;

/// <summary>
/// Comprueba si el texto compila como chunk Lua (solo sintaxis, sin ejecutar).
/// Usa el mismo <c>load(..., 't', env)</c> que el motor; el entorno vacío basta para el parseo.
/// </summary>
public static class LuaScriptSyntaxChecker
{
    private static readonly object Gate = new();
    private static LuaEnvironment? _shared;

    /// <returns><c>true</c> si no hay error de sintaxis; si no, <paramref name="errorLine"/> puede ser 0.</returns>
    public static bool TryValidate(string source, string chunkDisplayName, out int errorLine, out string? errorMessage)
    {
        errorLine = 0;
        errorMessage = null;
        var name = string.IsNullOrWhiteSpace(chunkDisplayName) ? "script.lua" : chunkDisplayName;

        lock (Gate)
        {
            _shared ??= new LuaEnvironment();
            _shared.State["__scriptSource"] = source ?? "";
            _shared.State["__scriptName"] = name;
            try
            {
                _shared.State.DoString(@"
                    local src, name = __scriptSource, __scriptName
                    local fn, err = load(src, name, 't', {})
                    if not fn then error(err or 'load failed', 0) end
                ");
                return true;
            }
            catch (Exception ex)
            {
                var full = ex.ToString();
                errorLine = LuaErrorLineParser.TryParseLine(full);
                if (errorLine == 0)
                    errorLine = LuaErrorLineParser.TryParseLine(ex.Message);
                errorMessage = ex is LuaException lx ? lx.Message : ex.Message;
                return false;
            }
        }
    }
}
