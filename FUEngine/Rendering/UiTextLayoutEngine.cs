using System.Globalization;
using System.Windows;
using System.Windows.Media;
using FUEngine.Core;
using WpfFontFamily = System.Windows.Media.FontFamily;

namespace FUEngine.Rendering;

/// <summary>Un fragmento de línea con estilo homogéneo.</summary>
internal sealed class TextLinePiece
{
    public RichChunk Style { get; }
    public string Text { get; }

    public TextLinePiece(RichChunk style, string text)
    {
        Style = style;
        Text = text;
    }
}

internal sealed class TextLayoutLine
{
    public List<TextLinePiece> Pieces { get; } = new();
}

internal static class UiTextLayoutEngine
{
    public static List<TextLayoutLine> BuildLines(
        IReadOnlyList<RichChunk> chunks,
        double maxWidth,
        double maxHeight,
        WpfFontFamily fontFamily,
        double baseFontSizeWpf,
        UITextLayoutSettings layout,
        double pixelsPerDip,
        double letterSpacingPx,
        double lineSpacingMul,
        out double usedHeight,
        out bool overflowed)
    {
        usedHeight = 0;
        overflowed = false;
        var lines = new List<TextLayoutLine>();
        if (maxWidth <= 1 || maxHeight <= 1)
            return lines;

        var minPrefix = Math.Max(1, layout.HyphenMinPrefixChars);

        if (!layout.WordWrap)
        {
            var single = new TextLayoutLine();
            foreach (var c in chunks)
            {
                if (!string.IsNullOrEmpty(c.IconName))
                    single.Pieces.Add(new TextLinePiece(CloneChunk(c), ""));
                else if (c.Text.Length == 0)
                    continue;
                else
                    single.Pieces.Add(new TextLinePiece(CloneChunk(c), c.Text));
            }
            if (single.Pieces.Count > 0)
                lines.Add(single);
            usedHeight = MeasureLinesHeight(lines, fontFamily, baseFontSizeWpf, pixelsPerDip, lineSpacingMul);
            overflowed = usedHeight > maxHeight + 0.5 || MeasureLineWidth(single, fontFamily, baseFontSizeWpf, pixelsPerDip, letterSpacingPx) > maxWidth + 0.5;
            return lines;
        }

        TextLayoutLine? current = new();
        double lineWidth = 0;
        var lineHeight = GetLineHeight(fontFamily, baseFontSizeWpf, pixelsPerDip);

        foreach (var chunk in chunks)
        {
            if (!string.IsNullOrEmpty(chunk.IconName))
            {
                var ic = CloneChunk(chunk);
                ic.Text = "";
                var iw = MeasureWord(ic, fontFamily, baseFontSizeWpf, pixelsPerDip, letterSpacingPx);
                PlaceAtomicWord(ref current, ref lineWidth, ic, iw, maxWidth, lines, fontFamily, baseFontSizeWpf, pixelsPerDip, letterSpacingPx, lineHeight, lineSpacingMul);
                continue;
            }
            if (chunk.Text.Length == 0) continue;
            var words = SplitWordsPreserve(chunk.Text);
            foreach (var w in words)
            {
                if (w.Length == 0) continue;
                var wTrim = w.TrimEnd();
                var trailingSpaces = w.Length - wTrim.Length;
                var measureChunk = CloneChunk(chunk);
                measureChunk.Text = wTrim;
                var wWidth = MeasureWord(measureChunk, fontFamily, baseFontSizeWpf, pixelsPerDip, letterSpacingPx);

                if (wTrim.Length == 0)
                {
                    if (trailingSpaces > 0)
                        TryAppendSpaces(ref current, ref lineWidth, chunk, trailingSpaces, maxWidth, lines, fontFamily, baseFontSizeWpf, pixelsPerDip, letterSpacingPx, lineHeight);
                    continue;
                }

                if (lineWidth + wWidth <= maxWidth || current!.Pieces.Count == 0)
                {
                    current!.Pieces.Add(new TextLinePiece(CloneChunkWithText(chunk, wTrim), wTrim));
                    lineWidth += wWidth;
                    if (trailingSpaces > 0)
                        TryAppendSpaces(ref current, ref lineWidth, chunk, trailingSpaces, maxWidth, lines, fontFamily, baseFontSizeWpf, pixelsPerDip, letterSpacingPx, lineHeight);
                    continue;
                }

                NewLine(lines, ref current, ref lineWidth);
                if (wWidth <= maxWidth)
                {
                    current!.Pieces.Add(new TextLinePiece(CloneChunkWithText(chunk, wTrim), wTrim));
                    lineWidth += wWidth;
                    if (trailingSpaces > 0)
                        TryAppendSpaces(ref current, ref lineWidth, chunk, trailingSpaces, maxWidth, lines, fontFamily, baseFontSizeWpf, pixelsPerDip, letterSpacingPx, lineHeight);
                    continue;
                }

                if (layout.HyphenationEnabled)
                {
                    BreakLongWord(chunk, wTrim, maxWidth, minPrefix, fontFamily, baseFontSizeWpf, pixelsPerDip, letterSpacingPx,
                        lines, ref current, ref lineWidth, lineHeight);
                    if (trailingSpaces > 0)
                        TryAppendSpaces(ref current, ref lineWidth, chunk, trailingSpaces, maxWidth, lines, fontFamily, baseFontSizeWpf, pixelsPerDip, letterSpacingPx, lineHeight);
                }
                else
                {
                    current!.Pieces.Add(new TextLinePiece(CloneChunkWithText(chunk, wTrim), wTrim));
                    lineWidth += wWidth;
                    if (trailingSpaces > 0)
                        TryAppendSpaces(ref current, ref lineWidth, chunk, trailingSpaces, maxWidth, lines, fontFamily, baseFontSizeWpf, pixelsPerDip, letterSpacingPx, lineHeight);
                }
            }
        }

        if (current != null && current.Pieces.Count > 0)
            lines.Add(current);

        usedHeight = lines.Count * lineHeight * lineSpacingMul;
        overflowed = usedHeight > maxHeight + 0.5;

        if (overflowed && layout.OverflowMode == UITextOverflowMode.Ellipsis)
            ApplyEllipsis(lines, maxWidth, fontFamily, baseFontSizeWpf, pixelsPerDip, letterSpacingPx);

        return lines;
    }

    private static void TryAppendSpaces(ref TextLayoutLine? current, ref double lineWidth, RichChunk chunk, int count, double maxWidth,
        List<TextLayoutLine> lines, WpfFontFamily fontFamily, double baseSize, double ppd, double letterSp, double lineHeight)
    {
        var s = new string(' ', count);
        var sp = CloneChunkWithText(chunk, s);
        var sw = MeasureWord(sp, fontFamily, baseSize, ppd, letterSp);
        if (lineWidth + sw <= maxWidth)
        {
            current ??= new TextLayoutLine();
            current.Pieces.Add(new TextLinePiece(sp, s));
            lineWidth += sw;
            return;
        }
        NewLine(lines, ref current, ref lineWidth);
        current ??= new TextLayoutLine();
        current.Pieces.Add(new TextLinePiece(sp, s));
        lineWidth = sw;
    }

    private static void BreakLongWord(RichChunk chunk, string word, double maxWidth, int minPrefix, WpfFontFamily ff, double baseSize, double ppd, double letterSp,
        List<TextLayoutLine> lines, ref TextLayoutLine? current, ref double lineWidth, double lineHeight)
    {
        var rest = word;
        while (rest.Length > 0)
        {
            var room = maxWidth - lineWidth;
            if (room < 1)
            {
                NewLine(lines, ref current, ref lineWidth);
                room = maxWidth;
            }

            var maxFit = FitPrefixLength(chunk, rest, room, ff, baseSize, ppd, letterSp);
            if (maxFit <= 0)
            {
                NewLine(lines, ref current, ref lineWidth);
                maxFit = FitPrefixLength(chunk, rest, maxWidth, ff, baseSize, ppd, letterSp);
                if (maxFit <= 0) maxFit = 1;
            }

            var take = maxFit;
            var hyphen = false;
            if (take < rest.Length && take >= minPrefix)
            {
                var withHyphen = rest[..take] + "-";
                if (MeasureWord(CloneChunkWithText(chunk, withHyphen), ff, baseSize, ppd, letterSp) <= maxWidth - lineWidth + 0.01)
                {
                    hyphen = true;
                }
                else
                {
                    while (take > minPrefix)
                    {
                        take--;
                        withHyphen = rest[..take] + "-";
                        if (MeasureWord(CloneChunkWithText(chunk, withHyphen), ff, baseSize, ppd, letterSp) <= maxWidth - lineWidth + 0.01)
                        {
                            hyphen = true;
                            break;
                        }
                    }
                }
            }

            if (hyphen && take < rest.Length && take >= minPrefix)
            {
                var piece = rest[..take] + "-";
                current ??= new TextLayoutLine();
                current.Pieces.Add(new TextLinePiece(CloneChunkWithText(chunk, piece), piece));
                lineWidth += MeasureWord(CloneChunkWithText(chunk, piece), ff, baseSize, ppd, letterSp);
                rest = rest[take..];
                NewLine(lines, ref current, ref lineWidth);
            }
            else
            {
                if (take < rest.Length && take < minPrefix)
                    take = Math.Min(rest.Length, Math.Max(1, FitPrefixLength(chunk, rest, maxWidth, ff, baseSize, ppd, letterSp)));
                var piece = rest[..take];
                current ??= new TextLayoutLine();
                current.Pieces.Add(new TextLinePiece(CloneChunkWithText(chunk, piece), piece));
                lineWidth += MeasureWord(CloneChunkWithText(chunk, piece), ff, baseSize, ppd, letterSp);
                rest = rest[take..];
                if (rest.Length > 0)
                    NewLine(lines, ref current, ref lineWidth);
            }
        }
    }

    private static int FitPrefixLength(RichChunk chunk, string word, double maxW, WpfFontFamily ff, double baseSize, double ppd, double letterSp)
    {
        if (word.Length == 0) return 0;
        var lo = 1;
        var hi = word.Length;
        var best = 0;
        while (lo <= hi)
        {
            var mid = (lo + hi) / 2;
            var sub = word[..mid];
            var w = MeasureWord(CloneChunkWithText(chunk, sub), ff, baseSize, ppd, letterSp);
            if (w <= maxW)
            {
                best = mid;
                lo = mid + 1;
            }
            else
                hi = mid - 1;
        }
        return best;
    }

    private static void ApplyEllipsis(List<TextLayoutLine> lines, double maxWidth, WpfFontFamily ff, double baseSize, double ppd, double letterSp)
    {
        if (lines.Count == 0) return;
        const string ell = "…";
        var last = lines[^1];
        while (last.Pieces.Count > 0)
        {
            var combined = string.Concat(last.Pieces.Select(p => p.Text));
            var test = combined + ell;
            var firstStyle = last.Pieces[0].Style;
            if (MeasureWord(CloneChunkWithText(firstStyle, test), ff, baseSize, ppd, letterSp) <= maxWidth)
                break;
            if (last.Pieces[^1].Text.Length <= 1)
            {
                last.Pieces.RemoveAt(last.Pieces.Count - 1);
                if (last.Pieces.Count == 0)
                {
                    lines.RemoveAt(lines.Count - 1);
                    if (lines.Count == 0) return;
                    last = lines[^1];
                }
            }
            else
            {
                var lp = last.Pieces[^1];
                var t = lp.Text[..^1];
                last.Pieces[^1] = new TextLinePiece(CloneChunkWithText(lp.Style, t), t);
            }
        }
        if (lines.Count == 0) return;
        last = lines[^1];
        if (last.Pieces.Count == 0) return;
        var st = last.Pieces[^1].Style;
        var lastText = last.Pieces[^1].Text + ell;
        last.Pieces[^1] = new TextLinePiece(CloneChunkWithText(st, lastText), lastText);
    }

    private static void NewLine(List<TextLayoutLine> lines, ref TextLayoutLine? current, ref double lineWidth)
    {
        if (current != null && current.Pieces.Count > 0)
            lines.Add(current);
        current = new TextLayoutLine();
        lineWidth = 0;
    }

    /// <summary>Coloca un glifo atómico (icono) con las mismas reglas de salto de línea que una palabra.</summary>
    private static void PlaceAtomicWord(ref TextLayoutLine? current, ref double lineWidth, RichChunk chunk, double wWidth, double maxWidth,
        List<TextLayoutLine> lines, WpfFontFamily fontFamily, double baseSize, double ppd, double letterSp, double lineHeight, double lineSpacingMul)
    {
        if (wWidth <= maxWidth || current == null || current.Pieces.Count == 0)
        {
            if (lineWidth + wWidth <= maxWidth || current == null || current.Pieces.Count == 0)
            {
                current ??= new TextLayoutLine();
                current.Pieces.Add(new TextLinePiece(chunk, ""));
                lineWidth += wWidth;
                return;
            }
        }
        NewLine(lines, ref current, ref lineWidth);
        current ??= new TextLayoutLine();
        current.Pieces.Add(new TextLinePiece(chunk, ""));
        lineWidth = wWidth;
    }

    private static List<string> SplitWordsPreserve(string text)
    {
        var list = new List<string>();
        var i = 0;
        while (i < text.Length)
        {
            if (char.IsWhiteSpace(text[i]))
            {
                var s = i;
                while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
                list.Add(text[s..i]);
                continue;
            }
            var w = i;
            while (i < text.Length && !char.IsWhiteSpace(text[i])) i++;
            list.Add(text[w..i]);
        }
        return list;
    }

    private static RichChunk CloneChunk(RichChunk c) => new()
    {
        Text = c.Text,
        Bold = c.Bold,
        Italic = c.Italic,
        Foreground = c.Foreground,
        FontSizeOverride = c.FontSizeOverride,
        IconName = c.IconName,
        LinkId = c.LinkId
    };

    private static RichChunk CloneChunkWithText(RichChunk c, string t) => new()
    {
        Text = t,
        Bold = c.Bold,
        Italic = c.Italic,
        Foreground = c.Foreground,
        FontSizeOverride = c.FontSizeOverride,
        IconName = c.IconName,
        LinkId = c.LinkId
    };

    public static double MeasureWord(RichChunk chunk, WpfFontFamily fontFamily, double baseFontWpf, double pixelsPerDip, double letterSpacingPx)
    {
        if (!string.IsNullOrEmpty(chunk.IconName))
        {
            var em = chunk.FontSizeOverride ?? baseFontWpf;
            var typeface = new Typeface(fontFamily, chunk.Italic ? FontStyles.Italic : FontStyles.Normal,
                chunk.Bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal);
            var ft = new FormattedText("M", CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight, typeface, em,
                System.Windows.Media.Brushes.Black, pixelsPerDip);
            return Math.Max(em * 0.5, ft.Height * 0.95);
        }
        var size = chunk.FontSizeOverride ?? baseFontWpf;
        var typeface2 = new Typeface(fontFamily, chunk.Italic ? FontStyles.Italic : FontStyles.Normal,
            chunk.Bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal);
        if (chunk.Text.Length == 0) return 0;
        var ftText = new FormattedText(
            chunk.Text,
            CultureInfo.CurrentCulture,
            System.Windows.FlowDirection.LeftToRight,
            typeface2,
            size,
            System.Windows.Media.Brushes.Black,
            pixelsPerDip)
        {
            Trimming = TextTrimming.None
        };
        var w = ftText.Width;
        if (letterSpacingPx > 0 && chunk.Text.Length > 1)
            w += letterSpacingPx * (chunk.Text.Length - 1);
        return w;
    }

    private static double MeasureLineWidth(TextLayoutLine line, WpfFontFamily ff, double baseSize, double ppd, double letterSp) =>
        line.Pieces.Sum(p => MeasureWord(CloneChunkWithText(p.Style, p.Text), ff, baseSize, ppd, letterSp));

    public static double MeasureLinesHeight(IReadOnlyList<TextLayoutLine> lines, WpfFontFamily ff, double baseSize, double ppd, double lineSpacingMul)
    {
        if (lines.Count == 0) return 0;
        var h = GetLineHeight(ff, baseSize, ppd);
        return lines.Count * h * lineSpacingMul;
    }

    public static double GetLineHeight(WpfFontFamily ff, double baseSize, double ppd)
    {
        var typeface = new Typeface(ff, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        var ft = new FormattedText("Mg", CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight, typeface, baseSize, System.Windows.Media.Brushes.Black, ppd);
        return ft.Height * 1.15;
    }

    public static double BinarySearchScaleToFit(List<TextLayoutLine> lines, double maxW, double maxH, WpfFontFamily ff, double baseSize, double ppd, double letterSp, double lineSpacingMul)
    {
        var h = MeasureLinesHeight(lines, ff, baseSize, ppd, lineSpacingMul);
        var w = lines.Count == 0 ? 0 : lines.Max(l => MeasureLineWidth(l, ff, baseSize, ppd, letterSp));
        if (h <= maxH && w <= maxW) return 1;
        var lo = 0.05;
        var hi = 1.0;
        for (var iter = 0; iter < 24; iter++)
        {
            var mid = (lo + hi) / 2;
            var ok = true;
            var th = MeasureLinesHeight(lines, ff, baseSize * mid, ppd, lineSpacingMul);
            var tw = lines.Count == 0 ? 0 : lines.Max(l => MeasureLineWidth(l, ff, baseSize * mid, ppd, letterSp * mid));
            if (th > maxH || tw > maxW) ok = false;
            if (ok) lo = mid;
            else hi = mid;
        }
        return lo;
    }
}
