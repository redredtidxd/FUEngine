using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using FUEngine;
using FUEngine.Core;
using CoreUiElement = FUEngine.Core.UIElement;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfColor = System.Windows.Media.Color;
using WpfBrush = System.Windows.Media.Brush;
using WpfImage = System.Windows.Controls.Image;
using MediaBrushes = System.Windows.Media.Brushes;

namespace FUEngine.Rendering;

public readonly record struct UiTextLinkLayoutRect(string LinkId, double X, double Y, double Width, double Height);

public sealed class UiTextBuildResult
{
    public required FrameworkElement Root { get; init; }
    public IReadOnlyList<UiTextLinkLayoutRect> LinkRects { get; init; } = Array.Empty<UiTextLinkLayoutRect>();
    public double ContentLayoutScale { get; init; } = 1;
}

/// <summary>Construye el texto UI (Text/Button) en WPF para viewport Play y editor.</summary>
public static class UiTextRenderer
{
    public const double InnerPadding = 4;

    public sealed class RenderArgs
    {
        public required CoreUiElement Element { get; init; }
        public required UIRect CanvasRect { get; init; }
        public required string ProjectRoot { get; init; }
        public double PixelsPerDip { get; init; } = 1.0;
        public double GameTimeSeconds { get; init; }
        public LocalizationRuntime? Localization { get; init; }
        /// <summary>Caracteres visibles (texto plano tras quitar tags). int.MaxValue = todo.</summary>
        public int VisiblePlainCharCount { get; init; } = int.MaxValue;
        /// <summary>Tiempo de juego en que se reveló cada carácter visible (fade-in).</summary>
        public IReadOnlyList<double>? CharRevealGameTimes { get; init; }
        public bool TypewriterFadeInActive { get; init; }
        public double FadeInDurationSeconds { get; init; } = 0.08;
    }

    public static UiTextBuildResult? Build(RenderArgs args)
    {
        var el = args.Element;
        if (el.Kind is not (UIElementKind.Text or UIElementKind.Button)) return null;

        var resolved = UiTextResolve.Resolve(el, args.ProjectRoot, args.Localization);
        var displayText = resolved.DisplayText;
        if (string.IsNullOrEmpty(displayText)) return null;

        var style = resolved.Style;
        var layout = resolved.Layout;

        var innerW = Math.Max(1, args.CanvasRect.Width - InnerPadding * 2);
        var innerH = Math.Max(1, args.CanvasRect.Height - InnerPadding * 2);

        var font = ResolveWpfFontFamily(style.FontFamily, args.ProjectRoot);
        var baseSize = FontSizeToWpfPoints(style.FontSize, style.FontSizeUnit, args.PixelsPerDip);

        var fullPlain = UiRichText.StripTags(displayText);
        var visCount = args.VisiblePlainCharCount >= fullPlain.Length ? fullPlain.Length : Math.Max(0, args.VisiblePlainCharCount);

        string sourceForChunks;
        if (style.RichTextEnabled)
            sourceForChunks = TruncateTaggedSourceByPlainLength(displayText, visCount);
        else
            sourceForChunks = fullPlain[..visCount];

        List<RichChunk> chunks;
        if (style.RichTextEnabled && !string.IsNullOrEmpty(sourceForChunks))
            chunks = UiRichText.ParseChunks(sourceForChunks);
        else
            chunks = new List<RichChunk> { new() { Text = sourceForChunks } };

        var baseColor = ParseColorWithOpacity(style.Color, style.Opacity);
        foreach (var c in chunks)
        {
            if (!c.Foreground.HasValue)
                c.Foreground = baseColor;
        }

        var lines = UiTextLayoutEngine.BuildLines(
            chunks, innerW, innerH, font, baseSize, layout, args.PixelsPerDip, style.LetterSpacing, style.LineSpacing,
            out _, out var overflowed);

        var scale = 1.0;
        if (overflowed && layout.OverflowMode == UITextOverflowMode.ScaleToFit)
        {
            scale = UiTextLayoutEngine.BinarySearchScaleToFit(lines, innerW, innerH, font, baseSize, args.PixelsPerDip,
                style.LetterSpacing, style.LineSpacing);
        }

        var em = baseSize * scale;
        var lineHeight = UiTextLayoutEngine.GetLineHeight(font, em, args.PixelsPerDip) * style.LineSpacing;

        var linkRects = new List<UiTextLinkLayoutRect>();
        double yCursor = 0;
        foreach (var line in lines)
        {
            var lineW = line.Pieces.Sum(p => MeasurePieceWidth(p, font, em, args.PixelsPerDip, style.LetterSpacing));
            var startX = style.Alignment switch
            {
                UITextAlignmentKind.Center => Math.Max(0, (innerW - lineW) / 2),
                UITextAlignmentKind.Right => Math.Max(0, innerW - lineW),
                _ => 0.0
            };
            var x = startX;
            foreach (var piece in line.Pieces)
            {
                var pw = MeasurePieceWidth(piece, font, em, args.PixelsPerDip, style.LetterSpacing);
                if (!string.IsNullOrEmpty(piece.Style.LinkId))
                    linkRects.Add(new UiTextLinkLayoutRect(piece.Style.LinkId!, x, yCursor, pw, lineHeight));
                x += pw;
            }
            yCursor += lineHeight;
        }

        double pivotOx = 0, pivotOy = 0;
        if (el.TextAnchor != null)
        {
            var (px, py) = UITextAnchorSettings.ToPivotFractions(el.TextAnchor.PivotPreset);
            pivotOx = -innerW * px;
            pivotOy = -innerH * py;
        }

        if (Math.Abs(pivotOx) > 1e-9 || Math.Abs(pivotOy) > 1e-9)
        {
            for (var i = 0; i < linkRects.Count; i++)
            {
                var r = linkRects[i];
                linkRects[i] = new UiTextLinkLayoutRect(r.LinkId, r.X + pivotOx, r.Y + pivotOy, r.Width, r.Height);
            }
        }

        var ge = style.GlyphEffects;
        var glyphMotion = ge != null && (ge.ShakeEnabled || ge.WaveEnabled || ge.RainbowEnabled);
        var usePerCharFade = args.TypewriterFadeInActive && args.CharRevealGameTimes != null;

        var linesPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Vertical };
        var flatCharIndex = 0;

        foreach (var line in lines)
        {
            var row = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
            foreach (var piece in line.Pieces)
            {
                if (!string.IsNullOrEmpty(piece.Style.IconName))
                {
                    flatCharIndex += AddIconPiece(row, piece, font, em, args, style, baseColor, lineHeight, ge, glyphMotion,
                        usePerCharFade, flatCharIndex);
                    continue;
                }

                if (piece.Text.Length == 0) continue;

                if (usePerCharFade || glyphMotion)
                {
                    flatCharIndex = AddTextPiecePerChar(row, piece, font, em, args, style, baseColor, ge, glyphMotion,
                        usePerCharFade, flatCharIndex);
                }
                else
                {
                    row.Children.Add(BuildPieceTextBlock(piece, font, em, style.EnableKerning));
                    flatCharIndex += piece.Text.Length;
                }
            }
            linesPanel.Children.Add(row);
        }

        var content = new Grid();
        if (layout.OverflowMode == UITextOverflowMode.Clip)
            content.ClipToBounds = true;

        var outline = style.OutlineThickness;
        if (outline > 0.25)
        {
            var outlineBrush = new SolidColorBrush(ParseColor(style.OutlineColor));
            outlineBrush.Freeze();
            var steps = Math.Min(8, (int)Math.Ceiling(outline) + 4);
            for (var ring = 0; ring < steps; ring++)
            {
                var ang = ring * (Math.PI * 2 / steps);
                var dx = Math.Cos(ang) * outline;
                var dy = Math.Sin(ang) * outline;
                var ghost = CloneLinesVisual(linesPanel, outlineBrush, em * 0.98);
                ghost.Margin = new Thickness(dx, dy, -dx, -dy);
                ghost.IsHitTestVisible = false;
                ghost.Opacity = 0.85;
                content.Children.Add(ghost);
            }
        }

        var fgContainer = new Grid();
        if (style.ShadowBlur > 0.5 || Math.Abs(style.ShadowOffsetX) > 0.5 || Math.Abs(style.ShadowOffsetY) > 0.5)
        {
            var eff = new DropShadowEffect
            {
                Color = ParseColor(style.ShadowColor),
                BlurRadius = style.ShadowBlur,
                ShadowDepth = 0,
                Direction = 0,
                Opacity = 0.9
            };
            fgContainer.Effect = eff;
            fgContainer.Margin = new Thickness(style.ShadowOffsetX, style.ShadowOffsetY, 0, 0);
        }

        linesPanel.HorizontalAlignment = style.Alignment switch
        {
            UITextAlignmentKind.Center => System.Windows.HorizontalAlignment.Center,
            UITextAlignmentKind.Right => System.Windows.HorizontalAlignment.Right,
            UITextAlignmentKind.Justify => System.Windows.HorizontalAlignment.Left,
            _ => System.Windows.HorizontalAlignment.Left
        };

        fgContainer.Children.Add(linesPanel);
        content.Children.Add(fgContainer);

        var scaleTf = scale < 0.999 ? new ScaleTransform(scale, scale) : null;
        var needPivot = Math.Abs(pivotOx) > 1e-9 || Math.Abs(pivotOy) > 1e-9;
        if (needPivot)
        {
            var pivotWrap = new Grid { RenderTransform = new TranslateTransform(pivotOx, pivotOy) };
            pivotWrap.Children.Add(content);
            content = pivotWrap;
        }

        if (scaleTf != null)
        {
            var scaled = new Grid { RenderTransformOrigin = new System.Windows.Point(0, 0), RenderTransform = scaleTf };
            scaled.Children.Add(content);
            content = scaled;
        }

        var host = new Grid { Margin = new Thickness(InnerPadding) };
        host.Children.Add(content);
        return new UiTextBuildResult
        {
            Root = host,
            LinkRects = linkRects,
            ContentLayoutScale = scale
        };
    }

    private static double MeasurePieceWidth(TextLinePiece piece, WpfFontFamily font, double em, double ppd, double letterSp) =>
        UiTextLayoutEngine.MeasureWord(
            new RichChunk
            {
                Text = piece.Text,
                Bold = piece.Style.Bold,
                Italic = piece.Style.Italic,
                Foreground = piece.Style.Foreground,
                FontSizeOverride = piece.Style.FontSizeOverride,
                IconName = piece.Style.IconName,
                LinkId = piece.Style.LinkId
            },
            font, em, ppd, letterSp);

    private static int AddIconPiece(System.Windows.Controls.Panel row, TextLinePiece piece, WpfFontFamily font, double em, RenderArgs args,
        UITextStyle style, WpfColor baseColor, double lineHeight, UITextGlyphEffects? ge, bool glyphMotion, bool usePerCharFade,
        int flatCharIndex)
    {
        var path = TryFindIconPath(args.ProjectRoot, piece.Style.IconName!);
        var pw = MeasurePieceWidth(piece, font, em, args.PixelsPerDip, style.LetterSpacing);
        var ph = lineHeight * 0.92;
        FrameworkElement visual;
        if (path != null)
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(path, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                var img = new WpfImage { Source = bmp, Stretch = Stretch.Uniform, Width = pw, Height = ph };
                visual = img;
            }
            catch
            {
                visual = PlaceholderIcon(piece, font, em, pw, ph);
            }
        }
        else
            visual = PlaceholderIcon(piece, font, em, pw, ph);

        double dx = 0, dy = 0;
        if (ge != null && glyphMotion)
        {
            var i = flatCharIndex;
            if (ge.ShakeEnabled)
            {
                dx += Math.Sin(args.GameTimeSeconds * 47.1 + i * 12.989) * ge.ShakeIntensityPixels;
                dy += Math.Cos(args.GameTimeSeconds * 43.7 + i * 11.17) * ge.ShakeIntensityPixels;
            }
            if (ge.WaveEnabled)
                dy += Math.Sin(args.GameTimeSeconds * 3.0 + i * ge.WaveFrequency) * ge.WaveAmplitudePixels;
        }

        if (Math.Abs(dx) > 0.01 || Math.Abs(dy) > 0.01)
        {
            var b = new Border { Child = visual, Background = MediaBrushes.Transparent };
            b.RenderTransform = new TranslateTransform(dx, dy);
            row.Children.Add(b);
        }
        else
            row.Children.Add(visual);

        return 1;
    }

    private static TextBlock PlaceholderIcon(TextLinePiece piece, WpfFontFamily font, double em, double w, double h) =>
        new()
        {
            Text = "□",
            FontFamily = font,
            FontSize = piece.Style.FontSizeOverride ?? em,
            FontWeight = piece.Style.Bold ? FontWeights.Bold : FontWeights.Normal,
            Width = w,
            Height = h,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(piece.Style.Foreground ?? System.Windows.Media.Colors.Gray)
        };

    private static int AddTextPiecePerChar(System.Windows.Controls.Panel row, TextLinePiece piece, WpfFontFamily font, double em, RenderArgs args,
        UITextStyle style, WpfColor baseColor, UITextGlyphEffects? ge, bool glyphMotion, bool usePerCharFade, int flatCharIndex)
    {
        for (var i = 0; i < piece.Text.Length; i++)
        {
            var ch = piece.Text[i].ToString();
            var idx = flatCharIndex + i;
            var alpha = 1.0;
            if (usePerCharFade && args.CharRevealGameTimes != null && idx < args.CharRevealGameTimes.Count)
            {
                var t0 = args.CharRevealGameTimes[idx];
                var dt = args.GameTimeSeconds - t0;
                alpha = args.FadeInDurationSeconds > 1e-6
                    ? Math.Clamp(dt / args.FadeInDurationSeconds, 0, 1)
                    : 1;
            }

            var fgCol = piece.Style.Foreground ?? baseColor;
            if (ge != null && ge.RainbowEnabled)
            {
                var shift = args.GameTimeSeconds * ge.RainbowCyclesPerSecond + idx * 0.07;
                shift -= Math.Floor(shift);
                fgCol = ShiftHue(fgCol, shift);
            }

            var brush = new SolidColorBrush(WpfColor.FromArgb(
                (byte)(255 * alpha * (fgCol.A / 255.0)),
                fgCol.R, fgCol.G, fgCol.B));
            brush.Freeze();

            var tb = new TextBlock
            {
                FontFamily = font,
                TextWrapping = TextWrapping.NoWrap,
                Text = ch,
                FontSize = piece.Style.FontSizeOverride ?? em,
                FontWeight = piece.Style.Bold ? FontWeights.Bold : FontWeights.Normal,
                FontStyle = piece.Style.Italic ? FontStyles.Italic : FontStyles.Normal,
                Foreground = brush,
                VerticalAlignment = VerticalAlignment.Top
            };
            ApplyTypography(tb, style.EnableKerning);

            double dx = 0, dy = 0;
            if (ge != null && glyphMotion)
            {
                if (ge.ShakeEnabled)
                {
                    dx += Math.Sin(args.GameTimeSeconds * 47.1 + idx * 12.989) * ge.ShakeIntensityPixels;
                    dy += Math.Cos(args.GameTimeSeconds * 43.7 + idx * 11.17) * ge.ShakeIntensityPixels;
                }
                if (ge.WaveEnabled)
                    dy += Math.Sin(args.GameTimeSeconds * 3.0 + idx * ge.WaveFrequency) * ge.WaveAmplitudePixels;
            }

            if (Math.Abs(dx) > 0.01 || Math.Abs(dy) > 0.01)
            {
                var b = new Border { Child = tb, Background = MediaBrushes.Transparent };
                b.RenderTransform = new TranslateTransform(dx, dy);
                row.Children.Add(b);
            }
            else
                row.Children.Add(tb);
        }

        return flatCharIndex + piece.Text.Length;
    }

    private static WpfColor ShiftHue(WpfColor rgb, double hue01)
    {
        double r = rgb.R / 255.0, g = rgb.G / 255.0, b = rgb.B / 255.0;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        double h, s, v = max;
        var d = max - min;
        s = max > 1e-6 ? d / max : 0;
        if (d < 1e-6) h = 0;
        else if (Math.Abs(max - r) < 1e-6) h = ((g - b) / d % 6 + 6) % 6 / 6.0;
        else if (Math.Abs(max - g) < 1e-6) h = ((b - r) / d + 2) / 6.0;
        else h = ((r - g) / d + 4) / 6.0;

        h = (h + hue01) % 1.0;
        if (h < 0) h += 1;

        var c = v * s;
        var x = c * (1 - Math.Abs(h * 6 % 2 - 1));
        double r1 = 0, g1 = 0, b1 = 0;
        var hp = h * 6;
        if (hp < 1) { r1 = c; g1 = x; }
        else if (hp < 2) { r1 = x; g1 = c; }
        else if (hp < 3) { g1 = c; b1 = x; }
        else if (hp < 4) { g1 = x; b1 = c; }
        else if (hp < 5) { r1 = x; b1 = c; }
        else { r1 = c; b1 = x; }
        var m = v - c;
        r1 += m; g1 += m; b1 += m;
        return WpfColor.FromArgb(rgb.A,
            (byte)Math.Clamp(r1 * 255, 0, 255),
            (byte)Math.Clamp(g1 * 255, 0, 255),
            (byte)Math.Clamp(b1 * 255, 0, 255));
    }

    private static string? TryFindIconPath(string projectRoot, string name)
    {
        if (string.IsNullOrWhiteSpace(projectRoot) || string.IsNullOrWhiteSpace(name)) return null;
        var dir = Path.Combine(projectRoot, "Assets", "UI", "Icons");
        foreach (var ext in new[] { ".png", ".jpg", ".jpeg", ".webp" })
        {
            var p = Path.Combine(dir, name + ext);
            if (File.Exists(p)) return Path.GetFullPath(p);
        }
        return null;
    }

    private static TextBlock BuildPieceTextBlock(TextLinePiece piece, WpfFontFamily font, double em, bool kerning)
    {
        var fg = piece.Style.Foreground ?? System.Windows.Media.Colors.White;
        var tb = new TextBlock
        {
            FontFamily = font,
            FontSize = piece.Style.FontSizeOverride ?? em,
            FontWeight = piece.Style.Bold ? FontWeights.Bold : FontWeights.Normal,
            FontStyle = piece.Style.Italic ? FontStyles.Italic : FontStyles.Normal,
            Foreground = new SolidColorBrush(fg),
            Text = piece.Text,
            TextWrapping = TextWrapping.NoWrap,
            VerticalAlignment = VerticalAlignment.Top
        };
        ApplyTypography(tb, kerning);
        return tb;
    }

    private static Grid CloneLinesVisual(StackPanel linesPanel, WpfBrush strokeColor, double em)
    {
        var g = new Grid();
        var sp = new StackPanel { Orientation = System.Windows.Controls.Orientation.Vertical };
        foreach (var lineRow in linesPanel.Children.OfType<StackPanel>())
        {
            var row = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
            foreach (var cell in lineRow.Children)
            {
                if (cell is TextBlock tb0)
                {
                    row.Children.Add(CloneTextBlockStroke(tb0, strokeColor, em));
                    continue;
                }
                if (cell is Border bd && bd.Child is TextBlock tb1)
                {
                    var inner = CloneTextBlockStroke(tb1, strokeColor, em);
                    var nb = new Border { Child = inner, Background = MediaBrushes.Transparent };
                    if (bd.RenderTransform is TranslateTransform tt)
                        nb.RenderTransform = new TranslateTransform(tt.X, tt.Y);
                    row.Children.Add(nb);
                    continue;
                }
                if (cell is WpfImage img)
                {
                    var ph = new TextBlock { Text = " ", Foreground = strokeColor, Width = img.Width > 0 ? img.Width : 8 };
                    row.Children.Add(ph);
                }
                else if (cell is Border bdi && bdi.Child is WpfImage)
                {
                    var ph = new TextBlock { Text = " ", Foreground = strokeColor, Width = 8 };
                    row.Children.Add(ph);
                }
            }
            sp.Children.Add(row);
        }
        g.Children.Add(sp);
        return g;
    }

    private static TextBlock CloneTextBlockStroke(TextBlock tb, WpfBrush strokeColor, double em)
    {
        var clone = new TextBlock
        {
            FontFamily = tb.FontFamily,
            FontSize = tb.FontSize > 0 ? tb.FontSize : em,
            FontWeight = tb.FontWeight,
            FontStyle = tb.FontStyle,
            Foreground = strokeColor,
            Text = tb.Text,
            TextWrapping = TextWrapping.NoWrap
        };
        if (tb.Inlines.Count > 0)
        {
            clone.Text = "";
            foreach (var inline in tb.Inlines)
            {
                if (inline is Run r)
                {
                    clone.Inlines.Add(new Run(r.Text)
                    {
                        FontSize = r.FontSize,
                        FontWeight = r.FontWeight,
                        FontStyle = r.FontStyle,
                        Foreground = strokeColor
                    });
                }
            }
        }
        return clone;
    }

    private static void ApplyTypography(TextBlock tb, bool kerning)
    {
        try
        {
            Typography.SetKerning(tb, kerning);
        }
        catch
        {
            /* Typography.Kerning puede no estar en todas las versiones */
        }
    }

    public static WpfFontFamily ResolveWpfFontFamily(string? fontSpec, string projectRoot)
    {
        if (string.IsNullOrWhiteSpace(fontSpec))
            return System.Windows.SystemFonts.MessageFontFamily;
        var spec = fontSpec.Trim().Replace('/', Path.DirectorySeparatorChar);
        var ext = Path.GetExtension(spec).ToLowerInvariant();
        if (ext is ".ttf" or ".otf")
        {
            var path = Path.IsPathRooted(spec) ? spec : Path.GetFullPath(Path.Combine(projectRoot, spec));
            if (File.Exists(path))
            {
                try
                {
                    var uri = new Uri(path, UriKind.Absolute);
                    foreach (var f in Fonts.GetFontFamilies(uri))
                        return f;
                }
                catch
                {
                    /* fall through */
                }
            }
            global::FUEngine.EditorLog.Warning($"Fuente UI no encontrada o inválida: {spec}. Se usa la fuente del sistema.", "UI");
            return System.Windows.SystemFonts.MessageFontFamily;
        }
        try
        {
            return new WpfFontFamily(spec);
        }
        catch
        {
            global::FUEngine.EditorLog.Warning($"Familia de fuente UI no reconocida: {spec}. Se usa la fuente del sistema.", "UI");
            return System.Windows.SystemFonts.MessageFontFamily;
        }
    }

    public static double FontSizeToWpfPoints(double value, UITextFontUnit unit, double pixelsPerDip)
    {
        if (unit == UITextFontUnit.Points)
            return value;
        var dpi = 96.0 * pixelsPerDip;
        return value * 72.0 / dpi;
    }

    public static WpfColor ParseColor(string hex)
    {
        if (UiRichText.TryParseHexColor(hex, out var c))
            return c;
        return System.Windows.Media.Colors.White;
    }

    public static WpfColor ParseColorWithOpacity(string hex, double opacityMul)
    {
        var c = ParseColor(hex);
        var a = (byte)Math.Clamp(c.A * opacityMul, 0, 255);
        return WpfColor.FromArgb(a, c.R, c.G, c.B);
    }

    /// <summary>Recorta el string con tags de modo que solo queden <paramref name="maxPlainChars"/> caracteres visibles (icono = 1).</summary>
    public static string TruncateTaggedSourceByPlainLength(string tagged, int maxPlainChars)
    {
        if (maxPlainChars <= 0) return "";
        var plain = 0;
        var i = 0;
        while (i < tagged.Length)
        {
            if (tagged[i] == '<')
            {
                var end = tagged.IndexOf('>', i);
                if (end < 0) break;
                var inner = tagged.AsSpan(i + 1, end - i - 1).Trim();
                if (inner.StartsWith("icon=", StringComparison.OrdinalIgnoreCase) ||
                    inner.StartsWith("icon =", StringComparison.OrdinalIgnoreCase))
                {
                    plain++;
                    i = end + 1;
                    if (plain >= maxPlainChars)
                        break;
                    continue;
                }
                i = end + 1;
                continue;
            }
            plain++;
            i++;
            if (plain >= maxPlainChars)
                break;
        }
        return tagged[..i];
    }
}
