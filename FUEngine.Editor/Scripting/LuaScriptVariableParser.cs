using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace FUEngine.Editor;

/// <summary>
/// Detecta asignaciones globales en el ámbito raíz del script (fuera de bloques <c>function … end</c>).
/// Ignora <c>local</c>. No sustituye a un parser Lua completo; cubre el estilo típico de scripts de juego.
/// </summary>
public static class LuaScriptVariableParser
{
    private static readonly Regex GlobalAssignRegex = new(
        @"^\s*([a-zA-Z_][a-zA-Z0-9_]*)\s*=\s*(.+)$",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(200));

    private static readonly Regex FunctionLineRegex = new(
        @"^\s*(local\s+)?function\s",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(50));

    private static readonly Regex EndLineRegex = new(
        @"^\s*end\b",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(50));

    private static readonly Regex IfThenLineRegex = new(
        @"^\s*if\b.+\bthen\b",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(50));

    private static readonly Regex ForDoLineRegex = new(
        @"^\s*for\b.+\bdo\b",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(50));

    private static readonly Regex WhileDoLineRegex = new(
        @"^\s*while\b.+\bdo\b",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(50));

    private static readonly Regex RepeatLineRegex = new(
        @"^\s*repeat\b",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(50));

    private static readonly Regex UntilLineRegex = new(
        @"^\s*until\b",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(50));

    private static readonly Regex LocalLineRegex = new(
        @"^\s*local\b",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(50));

    private static readonly Regex InspectorPropRegex = new(
        @"^\s*--\s*@prop\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*:\s*(\w+)\s*=\s*(.+)$",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(200));

    /// <summary>
    /// Formato corto: <c>-- @prop nombre = valor</c> (tipo inferido: número, bool, cadena entre comillas).
    /// </summary>
    private static readonly Regex InspectorPropShortRegex = new(
        @"^\s*--\s*@prop\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*=\s*(.+)$",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(200));

    /// <summary>
    /// Líneas <c>-- @prop nombre: tipo = valor</c> (tipos: number, int, float, string, bool, object).
    /// También acepta <c>-- @prop nombre = valor</c> con tipo inferido.
    /// </summary>
    public static List<(string Name, string Type, string DefaultValue)> ParseInspectorProps(string luaCode)
    {
        var result = new List<(string Name, string Type, string DefaultValue)>();
        if (string.IsNullOrWhiteSpace(luaCode)) return result;

        foreach (var raw in luaCode.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var m = InspectorPropRegex.Match(raw);
            if (m.Success)
            {
                var name = m.Groups[1].Value.Trim();
                var typeRaw = m.Groups[2].Value.Trim().ToLowerInvariant();
                var valueRaw = m.Groups[3].Value.Trim();
                if (string.IsNullOrEmpty(name)) continue;
                var normalizedType = typeRaw switch
                {
                    "number" => "float",
                    "double" => "float",
                    _ => typeRaw
                };
                var defaultVal = ParseDefaultForDeclaredType(normalizedType, valueRaw);
                result.Add((name, normalizedType, defaultVal));
                continue;
            }

            var m2 = InspectorPropShortRegex.Match(raw);
            if (!m2.Success) continue;
            var name2 = m2.Groups[1].Value.Trim();
            var valueRaw2 = m2.Groups[2].Value.Trim();
            if (string.IsNullOrEmpty(name2)) continue;
            var inferred = InferTypeAndValue(valueRaw2);
            result.Add((name2, inferred.Type, inferred.DefaultValue));
        }

        return result;
    }

    private static string ParseDefaultForDeclaredType(string type, string valueRaw)
    {
        valueRaw = valueRaw.Trim();
        if (string.Equals(type, "object", StringComparison.OrdinalIgnoreCase))
        {
            if (valueRaw.Equals("nil", StringComparison.OrdinalIgnoreCase)) return "";
            if (valueRaw.Length >= 2 && (valueRaw[0] == '"' || valueRaw[0] == '\''))
            {
                var q = valueRaw[0];
                if (valueRaw[^1] == q && valueRaw.Length >= 2)
                    return UnescapeLuaString(valueRaw[1..^1], q);
            }

            return valueRaw;
        }

        if (string.Equals(type, "bool", StringComparison.OrdinalIgnoreCase))
            return valueRaw.Equals("true", StringComparison.OrdinalIgnoreCase) ? "true" : "false";

        if (string.Equals(type, "string", StringComparison.OrdinalIgnoreCase))
        {
            var (t, v) = InferTypeAndValue(valueRaw);
            return t == "string" ? v : valueRaw;
        }

        if (string.Equals(type, "int", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(valueRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)) return i.ToString(CultureInfo.InvariantCulture);
            return "0";
        }

        if (string.Equals(type, "float", StringComparison.OrdinalIgnoreCase) || string.Equals(type, "number", StringComparison.OrdinalIgnoreCase))
        {
            if (double.TryParse(valueRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) return d.ToString(CultureInfo.InvariantCulture);
            return "0";
        }

        var inferred = InferTypeAndValue(valueRaw);
        return inferred.DefaultValue;
    }

    /// <summary>
    /// Primero <see cref="ParseInspectorProps"/>; luego asignaciones globales de <see cref="Parse"/> sin pisar nombres ya definidos por <c>@prop</c>.
    /// Las asignaciones en la raíz con comentario final <c>-- [Editable]</c> se incluyen igual que las demás globales (el comentario es documentación explícita).
    /// </summary>
    public static List<(string Name, string Type, string DefaultValue)> ParseMergedForInspector(string luaCode)
    {
        var list = new List<(string Name, string Type, string DefaultValue)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in ParseInspectorProps(luaCode))
        {
            if (!seen.Add(p.Name)) continue;
            list.Add(p);
        }

        foreach (var p in Parse(luaCode))
        {
            if (!seen.Add(p.Name)) continue;
            list.Add(p);
        }

        return list;
    }

    /// <summary>
    /// Extrae variables globales de nivel raíz (asignaciones cuando la profundidad de <c>function</c> es 0).
    /// </summary>
    public static List<(string Name, string Type, string DefaultValue)> Parse(string luaCode)
    {
        var result = new List<(string Name, string Type, string DefaultValue)>();
        var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(luaCode)) return result;

        var lines = luaCode.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var depth = 0;
        foreach (var raw in lines)
        {
            var line = StripTrailingLineComment(raw);
            var trimmed = line.TrimStart();
            if (trimmed.Length == 0) continue;

            if (FunctionLineRegex.IsMatch(trimmed))
                depth++;
            else if (IfThenLineRegex.IsMatch(trimmed) || ForDoLineRegex.IsMatch(trimmed) || WhileDoLineRegex.IsMatch(trimmed))
                depth++;
            else if (RepeatLineRegex.IsMatch(trimmed))
                depth++;
            else if (UntilLineRegex.IsMatch(trimmed) && depth > 0)
                depth--;
            else if (EndLineRegex.IsMatch(trimmed) && depth > 0)
                depth--;

            if (depth > 0) continue;
            if (LocalLineRegex.IsMatch(trimmed)) continue;

            var m = GlobalAssignRegex.Match(line);
            if (!m.Success) continue;
            var name = m.Groups[1].Value.Trim();
            var valueStr = m.Groups[2].Value.Trim();
            if (string.IsNullOrEmpty(name) || !seen.Add(name)) continue;
            var (type, defaultValue) = InferTypeAndValue(valueStr);
            result.Add((name, type, defaultValue));
        }

        return result;
    }

    /// <summary>Quita comentario de línea <c>--</c> respetando comillas simples/dobles.</summary>
    private static string StripTrailingLineComment(string line)
    {
        var inString = '\0';
        for (var i = 0; i < line.Length - 1; i++)
        {
            var c = line[i];
            if (inString != '\0')
            {
                if (c == '\\' && i + 1 < line.Length) { i++; continue; }
                if (c == inString) inString = '\0';
                continue;
            }

            if (c is '"' or '\'') { inString = c; continue; }
            if (c == '-' && line[i + 1] == '-')
                return line[..i].TrimEnd();
        }

        return line;
    }

    private static (string Type, string DefaultValue) InferTypeAndValue(string valueStr)
    {
        valueStr = valueStr.TrimEnd(',', ';').Trim();
        if (valueStr.Length >= 2 && valueStr.StartsWith("[[", StringComparison.Ordinal))
        {
            var end = valueStr.IndexOf("]]", 2, StringComparison.Ordinal);
            var inner = end >= 0 ? valueStr[2..end] : valueStr[2..];
            return ("string", inner.Replace("\r\n", "\n", StringComparison.Ordinal));
        }

        if (valueStr.Length >= 1 && (valueStr[0] == '"' || valueStr[0] == '\''))
        {
            var q = valueStr[0];
            if (valueStr.Length >= 2 && valueStr[^1] == q)
                return ("string", UnescapeLuaString(valueStr[1..^1], q));
            return ("string", valueStr.Length > 1 ? UnescapeLuaString(valueStr[1..], q) : "");
        }

        if (valueStr.Equals("true", StringComparison.OrdinalIgnoreCase)) return ("bool", "true");
        if (valueStr.Equals("false", StringComparison.OrdinalIgnoreCase)) return ("bool", "false");
        if (valueStr.Equals("nil", StringComparison.OrdinalIgnoreCase)) return ("string", "");

        if (double.TryParse(valueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            return (valueStr.Contains('.') || valueStr.Contains('e', StringComparison.OrdinalIgnoreCase) || valueStr.Contains('E')
                ? "float"
                : "int", valueStr);

        return ("string", valueStr);
    }

    private static string UnescapeLuaString(string s, char quote)
    {
        return quote == '"' ? s.Replace("\\\"", "\"", StringComparison.Ordinal) : s.Replace("\\'", "'", StringComparison.Ordinal);
    }
}
