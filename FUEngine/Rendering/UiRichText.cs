using System.Globalization;
using System.Text;
using System.Windows.Media;
using WpfColor = System.Windows.Media.Color;

namespace FUEngine.Rendering;

internal sealed class RichChunk
{
    public string Text { get; set; } = "";
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public WpfColor? Foreground { get; set; }
    public double? FontSizeOverride { get; set; }
    /// <summary>Si no vacío, ocupa un slot de ancho ~1em y se dibuja como imagen.</summary>
    public string? IconName { get; set; }
    /// <summary>Región clicable; proviene de <c>&lt;link=id&gt;</c>.</summary>
    public string? LinkId { get; set; }
}

internal static class UiRichText
{
    private const char ObjectReplacement = '\uFFFC';

    public static string StripTags(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new StringBuilder(s.Length);
        var i = 0;
        while (i < s.Length)
        {
            if (s[i] == '<')
            {
                var close = s.IndexOf('>', i);
                if (close < 0) break;
                var inner = s.AsSpan(i + 1, close - i - 1).Trim();
                if (IsIconTagOpen(inner))
                {
                    sb.Append(ObjectReplacement);
                    i = close + 1;
                    continue;
                }
                i = close + 1;
                continue;
            }
            sb.Append(s[i++]);
        }
        return sb.ToString();
    }

    public static List<RichChunk> ParseChunks(string? source)
    {
        var list = new List<RichChunk>();
        if (string.IsNullOrEmpty(source)) return list;

        var bold = false;
        var italic = false;
        WpfColor? fg = null;
        double? size = null;
        var linkStack = new Stack<string>();
        var pos = 0;

        string? ActiveLink() => linkStack.Count > 0 ? linkStack.Peek() : null;

        void Flush(ReadOnlySpan<char> span)
        {
            if (span.IsEmpty) return;
            list.Add(new RichChunk
            {
                Text = span.ToString(),
                Bold = bold,
                Italic = italic,
                Foreground = fg,
                FontSizeOverride = size,
                LinkId = ActiveLink()
            });
        }

        while (pos < source.Length)
        {
            if (source[pos] == '<')
            {
                var end = source.IndexOf('>', pos);
                if (end < 0) break;
                var tagSpan = source.AsSpan(pos + 1, end - pos - 1).Trim();
                pos = end + 1;

                if (tagSpan.Length == 0) continue;

                if (tagSpan.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                {
                    var name = tagSpan[1..].ToString().Trim();
                    switch (name.ToLowerInvariant())
                    {
                        case "b": bold = false; break;
                        case "i": italic = false; break;
                        case "color": fg = null; break;
                        case "size": size = null; break;
                        case "link":
                            if (linkStack.Count > 0) linkStack.Pop();
                            break;
                    }
                    continue;
                }

                if (IsIconTagOpen(tagSpan))
                {
                    if (TryParseIconName(tagSpan, out var iconName))
                    {
                        list.Add(new RichChunk
                        {
                            Text = "",
                            IconName = iconName,
                            Bold = bold,
                            Italic = italic,
                            Foreground = fg,
                            FontSizeOverride = size,
                            LinkId = ActiveLink()
                        });
                    }
                    continue;
                }

                var tagName = GetTagName(tagSpan).ToString();
                switch (tagName.ToLowerInvariant())
                {
                    case "link":
                        if (TryParseLinkId(tagSpan, out var lid))
                            linkStack.Push(lid);
                        break;
                    case "b":
                        bold = true;
                        break;
                    case "i":
                        italic = true;
                        break;
                    case "color":
                        if (TryParseColorAttr(tagSpan, out var c))
                            fg = c;
                        break;
                    case "size":
                        if (TryParseSizeAttr(tagSpan, out var sz))
                            size = sz;
                        break;
                }
                continue;
            }

            var start = pos;
            while (pos < source.Length && source[pos] != '<')
                pos++;
            Flush(source.AsSpan(start, pos - start));
        }

        return MergeAdjacent(list);
    }

    private static bool IsIconTagOpen(ReadOnlySpan<char> tagSpan)
    {
        if (tagSpan.Length < 5) return false;
        return tagSpan.StartsWith("icon=", StringComparison.OrdinalIgnoreCase) ||
               tagSpan.StartsWith("icon =", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseIconName(ReadOnlySpan<char> tagSpan, out string iconName)
    {
        iconName = "";
        var eq = tagSpan.IndexOf('=');
        if (eq < 0) return false;
        var rest = tagSpan[(eq + 1)..].Trim();
        if (rest.Length == 0) return false;
        // quita cierre /
        while (rest.Length > 0 && rest[^1] == '/')
            rest = rest[..^1];
        iconName = rest.Trim().ToString();
        return iconName.Length > 0;
    }

    private static bool TryParseLinkId(ReadOnlySpan<char> tagSpan, out string linkId)
    {
        linkId = "";
        var eq = tagSpan.IndexOf('=');
        if (eq < 0) return false;
        linkId = tagSpan[(eq + 1)..].Trim().ToString();
        return linkId.Length > 0;
    }

    private static List<RichChunk> MergeAdjacent(List<RichChunk> raw)
    {
        if (raw.Count == 0) return raw;
        var merged = new List<RichChunk> { raw[0] };
        for (var i = 1; i < raw.Count; i++)
        {
            var prev = merged[^1];
            var cur = raw[i];
            if (string.IsNullOrEmpty(prev.IconName) && string.IsNullOrEmpty(cur.IconName) &&
                prev.Bold == cur.Bold && prev.Italic == cur.Italic &&
                Nullable.Equals(prev.Foreground, cur.Foreground) &&
                Nullable.Equals(prev.FontSizeOverride, cur.FontSizeOverride) &&
                string.Equals(prev.LinkId, cur.LinkId, StringComparison.Ordinal))
            {
                prev.Text += cur.Text;
            }
            else
                merged.Add(cur);
        }
        return merged;
    }

    private static ReadOnlySpan<char> GetTagName(ReadOnlySpan<char> tag)
    {
        var space = tag.IndexOf(' ');
        var slice = space < 0 ? tag : tag[..space];
        var eq = slice.IndexOf('=');
        return eq < 0 ? slice : slice[..eq];
    }

    private static bool TryParseColorAttr(ReadOnlySpan<char> tag, out WpfColor color)
    {
        color = default;
        var eq = tag.IndexOf('=');
        if (eq < 0) return false;
        var val = tag[(eq + 1)..].Trim();
        if (val.Length >= 2 && val[0] == '#')
            return TryParseHexColor(val.ToString(), out color);
        return false;
    }

    private static bool TryParseSizeAttr(ReadOnlySpan<char> tag, out double sizeWpf)
    {
        sizeWpf = 0;
        var eq = tag.IndexOf('=');
        if (eq < 0) return false;
        var val = tag[(eq + 1)..].Trim();
        return double.TryParse(val.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out sizeWpf) && sizeWpf > 0.5;
    }

    public static bool TryParseHexColor(string hex, out WpfColor color)
    {
        color = System.Windows.Media.Colors.White;
        if (string.IsNullOrWhiteSpace(hex)) return false;
        var h = hex.Trim();
        if (h.StartsWith('#')) h = h[1..];
        try
        {
            if (h.Length == 6)
            {
                var r = byte.Parse(h[..2], NumberStyles.HexNumber);
                var g = byte.Parse(h[2..4], NumberStyles.HexNumber);
                var b = byte.Parse(h[4..6], NumberStyles.HexNumber);
                color = WpfColor.FromRgb(r, g, b);
                return true;
            }
            if (h.Length == 8)
            {
                var a = byte.Parse(h[..2], NumberStyles.HexNumber);
                var r = byte.Parse(h[2..4], NumberStyles.HexNumber);
                var g = byte.Parse(h[4..6], NumberStyles.HexNumber);
                var b = byte.Parse(h[6..8], NumberStyles.HexNumber);
                color = WpfColor.FromArgb(a, r, g, b);
                return true;
            }
        }
        catch { /* ignore */ }
        return false;
    }
}
