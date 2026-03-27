using System;
using System.Collections.Generic;
using System.Linq;
using FUEngine.Spotlight;

namespace FUEngine;

/// <summary>
/// Sugerencias para el mini-IDE Lua (AvalonEdit). Palabras clave desde <see cref="LuaLanguageKeywords"/>; miembros tras «tabla.» vía reflexión
/// (<see cref="LuaVisibleAttribute"/> en <see cref="FUEngine.Runtime"/>). <see cref="MergeDynamic"/> añade globales extra.
/// Caché en disco reservada bajo <see cref="FUEngineAppPaths.LuaMetadataCacheDirectory"/> para futuras optimizaciones.
/// </summary>
public static class LuaEditorCompletionCatalog
{
    /// <summary>Tablas globales típicas (coinciden con <see cref="FUEngine.Runtime.ScriptBindings"/>).</summary>
    public static readonly string[] Globals =
    {
        "self", "layer", "world", "input", "time", "audio", "physics", "ui", "game", "Debug", "Key", "Mouse", "ads"
    };

    /// <summary>Misma lista que <see cref="LuaLanguageKeywords.Entries"/> (23 reservadas, manual Lua 5.5 §3.1).</summary>
    public static readonly string[] Keywords = Array.ConvertAll(LuaLanguageKeywords.Entries, static e => e.Word);

    private static readonly Dictionary<string, string[]> _memberMap = LuaEditorApiReflection.BuildMemberMap();

    private static List<string>? _dynamicExtra;

    /// <summary>Nombres extra (p. ej. proyecto). Llamar desde el host del editor si aplica.</summary>
    public static void MergeDynamic(IEnumerable<string>? extraGlobals)
    {
        _dynamicExtra = extraGlobals?.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Miembros tras "tabla." según el prefijo de línea.</summary>
    public static IReadOnlyList<string>? GetMembersAfterDot(string linePrefixTrimmed)
    {
        foreach (var kv in _memberMap)
        {
            if (linePrefixTrimmed.EndsWith(kv.Key, StringComparison.OrdinalIgnoreCase))
                return kv.Value;
        }
        return null;
    }

    /// <summary>Candidatos al escribir un identificador (sin punto): palabras clave, globales, snippets.</summary>
    public static IEnumerable<LuaCompletionEntry> FilterWordPrefix(string prefix, IReadOnlyList<(string Trigger, string Template)> snippets)
    {
        if (string.IsNullOrEmpty(prefix)) yield break;
        var p = prefix;
        int n = 0;
        const int max = 48;

        foreach (var kw in Keywords)
        {
            if (!kw.StartsWith(p, StringComparison.OrdinalIgnoreCase)) continue;
            yield return new LuaCompletionEntry(kw, kw, "Palabra clave Lua", LuaCompletionIconKind.Keyword);
            if (++n >= max) yield break;
        }
        foreach (var g in Globals)
        {
            if (!g.StartsWith(p, StringComparison.OrdinalIgnoreCase)) continue;
            var kind = g.Equals("world", StringComparison.OrdinalIgnoreCase) || g.Equals("self", StringComparison.OrdinalIgnoreCase)
                ? LuaCompletionIconKind.EntityGlobal
                : g.Equals("ads", StringComparison.OrdinalIgnoreCase)
                    ? LuaCompletionIconKind.Ads
                    : LuaCompletionIconKind.GlobalTable;
            yield return new LuaCompletionEntry(g, g, "Tabla global FUEngine", kind);
            if (++n >= max) yield break;
        }
        if (_dynamicExtra != null)
        {
            foreach (var g in _dynamicExtra)
            {
                if (!g.StartsWith(p, StringComparison.OrdinalIgnoreCase)) continue;
                yield return new LuaCompletionEntry(g, g, "Proyecto / API", LuaCompletionIconKind.GlobalTable);
                if (++n >= max) yield break;
            }
        }
        foreach (var (trigger, template) in snippets)
        {
            if (!trigger.StartsWith(p, StringComparison.OrdinalIgnoreCase)) continue;
            var insert = template.StartsWith("function ", StringComparison.Ordinal) ? (template.Split('\n').FirstOrDefault()?.TrimEnd() ?? trigger) : trigger;
            yield return new LuaCompletionEntry(trigger, insert, "Snippet", LuaCompletionIconKind.Snippet);
            if (++n >= max) yield break;
        }
    }

    public readonly struct LuaCompletionEntry
    {
        public LuaCompletionEntry(string text, string insertText, string? description, LuaCompletionIconKind iconKind = LuaCompletionIconKind.Default)
        {
            Text = text;
            InsertText = insertText;
            Description = description;
            IconKind = iconKind;
        }
        public string Text { get; }
        public string InsertText { get; }
        public string? Description { get; }
        public LuaCompletionIconKind IconKind { get; }
    }
}
