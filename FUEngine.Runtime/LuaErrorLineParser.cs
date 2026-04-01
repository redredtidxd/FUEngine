// -----------------------------------------------------------------------------
// FUEngine (FUEngine.Runtime) — Condiciones: LICENSE.md en la raíz del repositorio.
// Copyright (c) Red Redtid. No es licencia MIT/Apache ni "open source" OSI.
// Productos comerciales: revenue share según LICENSE.md. Prohibido integrar
// publicidad de terceros en forks GitHub sin autorización escrita. Plugins
// gratuitos (no venta de extensiones). Build pública sin garantías ni servidores
// de validación del titular.
// -----------------------------------------------------------------------------
using System.Text.RegularExpressions;

namespace FUEngine.Runtime;

/// <summary>Extrae el número de línea de mensajes de error Lua/NLua (p. ej. <c>archivo.lua:42: …</c>).</summary>
public static class LuaErrorLineParser
{
    private static readonly Regex LineRegex = new(@":(\d+):", RegexOptions.Compiled);

    public static int TryParseLine(string? message)
    {
        if (string.IsNullOrEmpty(message)) return 0;
        var match = LineRegex.Match(message);
        return match.Success && int.TryParse(match.Groups[1].Value, out var line) ? line : 0;
    }
}
