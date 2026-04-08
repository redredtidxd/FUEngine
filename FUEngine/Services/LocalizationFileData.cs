using System.IO;
using System.Text;
using System.Text.Json;

namespace FUEngine;

/// <summary>Modelo de <c>Data/localization.json</c> compartido por el editor y el runtime.</summary>
public sealed class LocalizationFileData
{
    public string DefaultLocale { get; set; } = "es";
    public string FallbackLocale { get; set; } = "en";

    /// <summary>Clave → (código idioma → texto).</summary>
    public Dictionary<string, Dictionary<string, string>> Entries { get; } = new(StringComparer.OrdinalIgnoreCase);

    public static string DefaultRelativePath => Path.Combine("Data", "localization.json");

    public static bool TryLoad(string absolutePath, out LocalizationFileData data, out string? error)
    {
        data = new LocalizationFileData();
        error = null;
        if (!File.Exists(absolutePath)) return true;
        try
        {
            var json = File.ReadAllText(absolutePath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("defaultLocale", out var dl))
                data.DefaultLocale = NormLocale(dl.GetString());
            if (root.TryGetProperty("fallbackLocale", out var fl))
                data.FallbackLocale = NormLocale(fl.GetString());
            if (root.TryGetProperty("entries", out var ent) && ent.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in ent.EnumerateObject())
                {
                    var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    if (p.Value.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var loc in p.Value.EnumerateObject())
                            map[NormLocale(loc.Name)] = loc.Value.GetString() ?? "";
                    }
                    else if (p.Value.ValueKind == JsonValueKind.String)
                        map[data.DefaultLocale] = p.Value.GetString() ?? "";
                    if (map.Count > 0)
                        data.Entries[p.Name] = map;
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public void Save(string absolutePath)
    {
        var dir = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        var entriesObj = new Dictionary<string, object>();
        foreach (var kv in Entries.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            var inner = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in kv.Value.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                inner[t.Key] = t.Value ?? "";
            entriesObj[kv.Key] = inner;
        }
        var root = new
        {
            defaultLocale = DefaultLocale,
            fallbackLocale = FallbackLocale,
            entries = entriesObj
        };
        var json = JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(absolutePath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    public HashSet<string> CollectAllLocaleCodes()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var map in Entries.Values)
        {
            foreach (var k in map.Keys)
                set.Add(NormLocale(k));
        }
        foreach (var x in new[] { DefaultLocale, FallbackLocale, "es", "en" })
            set.Add(NormLocale(x));
        return set;
    }

    private static string NormLocale(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "en";
        var t = s.Trim().ToLowerInvariant();
        return t.Length >= 2 ? t[..2] : t;
    }
}

/// <summary>Importación/exportación CSV (UTF-8 con BOM para Excel).</summary>
public static class LocalizationCsvIO
{
    public static bool TryExport(string path, LocalizationFileData data, IReadOnlyList<string> columnLocales, out string? error)
    {
        error = null;
        try
        {
            var locales = columnLocales.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim().ToLowerInvariant()[..2]).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (locales.Count == 0)
                locales = new List<string> { "es", "en" };
            var sb = new StringBuilder();
            sb.Append(EscapeCsv("Key"));
            foreach (var loc in locales)
                sb.Append(',').Append(EscapeCsv(loc.ToUpperInvariant()));
            sb.AppendLine();
            foreach (var key in data.Entries.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                sb.Append(EscapeCsv(key));
                data.Entries.TryGetValue(key, out var map);
                map ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var loc in locales)
                {
                    map.TryGetValue(loc, out var txt);
                    sb.Append(',').Append(EscapeCsv(txt ?? ""));
                }
                sb.AppendLine();
            }
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool TryImport(string path, LocalizationFileData into, out string? error)
    {
        error = null;
        try
        {
            var lines = File.ReadAllLines(path);
            if (lines.Length == 0) return true;
            var header = ParseCsvLine(lines[0]);
            if (header.Count < 2 || !header[0].Equals("Key", StringComparison.OrdinalIgnoreCase))
            {
                error = "La primera columna debe llamarse Key.";
                return false;
            }
            var locCols = header.Skip(1).Select((h, i) => (norm: Norm(h), idx: i + 1)).Where(x => x.norm.Length > 0).ToList();
            for (var li = 1; li < lines.Length; li++)
            {
                if (string.IsNullOrWhiteSpace(lines[li])) continue;
                var cells = ParseCsvLine(lines[li]);
                if (cells.Count == 0) continue;
                var key = cells[0].Trim();
                if (string.IsNullOrEmpty(key)) continue;
                if (!into.Entries.TryGetValue(key, out var map))
                {
                    map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    into.Entries[key] = map;
                }
                foreach (var (norm, idx) in locCols)
                {
                    if (idx < cells.Count)
                        map[norm] = cells[idx];
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static string Norm(string h)
    {
        var t = h.Trim().ToLowerInvariant();
        return t.Length >= 2 ? t[..2] : t;
    }

    private static string EscapeCsv(string? s)
    {
        s ??= "";
        if (s.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0)
            return '"' + s.Replace("\"", "\"\"") + '"';
        return s;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var list = new List<string>();
        var cur = new StringBuilder();
        var inQ = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQ)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        cur.Append('"');
                        i++;
                    }
                    else inQ = false;
                }
                else cur.Append(c);
            }
            else
            {
                if (c == '"') inQ = true;
                else if (c == ',')
                {
                    list.Add(cur.ToString());
                    cur.Clear();
                }
                else cur.Append(c);
            }
        }
        list.Add(cur.ToString());
        return list;
    }
}
