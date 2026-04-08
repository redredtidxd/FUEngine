using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FUEngine;

/// <summary>Opciones para generar un atlas de textura SDF (distancia con signo) a partir de un .ttf/.otf.</summary>
public sealed class FontSdfAtlasGeneratorOptions
{
    /// <summary>Tamaño de celda en píxeles (cada glifo centrado).</summary>
    public int CellSize { get; set; } = 56;

    /// <summary>Márgenes internos entre glifos en el atlas.</summary>
    public int Padding { get; set; } = 2;

    /// <summary>Distancia máxima codificada en el campo (píxeles); influye en el suavizado al escalar.</summary>
    public int SdfSpread { get; set; } = 10;

    public int FirstCodepoint { get; set; } = 32;
    public int LastCodepoint { get; set; } = 255;

    /// <summary>Tamaño em usado al rasterizar cada carácter.</summary>
    public double RenderEm { get; set; } = 36;
}

/// <summary>Genera PNG + JSON de manifiesto para render SDF futuro (Vulkan/shaders). WPF sigue usando vectores.</summary>
public static class FontSdfAtlasGenerator
{
    public static bool TryGenerate(string ttfAbsolutePath, string outputDirectory, FontSdfAtlasGeneratorOptions opt, out string? errorMessage)
    {
        errorMessage = null;
        try
        {
            if (!File.Exists(ttfAbsolutePath))
            {
                errorMessage = "No existe el archivo de fuente.";
                return false;
            }
            Directory.CreateDirectory(outputDirectory);
            var uri = new Uri(ttfAbsolutePath, UriKind.Absolute);
            var families = Fonts.GetFontFamilies(uri);
            using var en = families.GetEnumerator();
            if (!en.MoveNext())
            {
                errorMessage = "No se pudo leer ninguna familia desde el archivo.";
                return false;
            }
            var family = en.Current;
            var typeface = new Typeface(family, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            var cell = Math.Max(16, opt.CellSize);
            var pad = Math.Max(0, opt.Padding);
            var spread = Math.Max(2, opt.SdfSpread);
            var glyphs = new List<GlyphEntry>();
            var codepoints = new List<int>();
            for (var cp = opt.FirstCodepoint; cp <= opt.LastCodepoint; cp++)
            {
                if (char.IsSurrogate((char)cp)) continue;
                codepoints.Add(cp);
            }
            var cols = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(codepoints.Count)));
            var rows = (int)Math.Ceiling(codepoints.Count / (double)cols);
            var atlasW = cols * (cell + pad);
            var atlasH = rows * (cell + pad);
            var pixels = new byte[atlasW * atlasH];
            Array.Fill(pixels, (byte)128);
            for (var i = 0; i < codepoints.Count; i++)
            {
                var cp = codepoints[i];
                var ch = char.ConvertFromUtf32(cp);
                var col = i % cols;
                var row = i / cols;
                var ox = col * (cell + pad);
                var oy = row * (cell + pad);
                if (!TryRasterizeGlyph(typeface, ch, opt.RenderEm, cell, out var inside, out var adv))
                {
                    adv = cell * 0.35;
                    glyphs.Add(new GlyphEntry(cp, ox, oy, cell, cell, adv));
                    continue;
                }
                FillSdfCell(pixels, atlasW, atlasH, ox, oy, cell, inside, spread);
                glyphs.Add(new GlyphEntry(cp, ox, oy, cell, cell, adv));
            }
            var pngPath = Path.Combine(outputDirectory, "atlas_sdf.png");
            SaveGrayscalePng(pngPath, pixels, atlasW, atlasH);
            var jsonPath = Path.Combine(outputDirectory, "atlas_sdf.json");
            var jsonObj = new
            {
                version = 1,
                sourceFont = Path.GetFileName(ttfAbsolutePath),
                cellSize = cell,
                padding = pad,
                spread = spread,
                atlasWidth = atlasW,
                atlasHeight = atlasH,
                glyphs = glyphs.Select(g => new { codepoint = g.Codepoint, x = g.X, y = g.Y, w = g.W, h = g.H, advance = g.Advance }).ToList()
            };
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(jsonObj, new JsonSerializerOptions { WriteIndented = true }));
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    private sealed record GlyphEntry(int Codepoint, int X, int Y, int W, int H, double Advance);

    private static bool TryRasterizeGlyph(Typeface typeface, string ch, double em, int cellSize, out bool[,] inside, out double advance)
    {
        inside = new bool[cellSize, cellSize];
        advance = em * 0.5;
        try
        {
            var ft = new FormattedText(
                ch,
                CultureInfo.InvariantCulture,
                System.Windows.FlowDirection.LeftToRight,
                typeface,
                em,
                System.Windows.Media.Brushes.White,
                1.0);
            advance = Math.Max(2, ft.Width);
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawRectangle(System.Windows.Media.Brushes.Black, null, new Rect(0, 0, cellSize, cellSize));
                dc.DrawText(ft, new System.Windows.Point(2, 2));
            }
            var rtb = new RenderTargetBitmap(cellSize, cellSize, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            var stride = cellSize * 4;
            var buf = new byte[stride * cellSize];
            rtb.CopyPixels(buf, stride, 0);
            for (var y = 0; y < cellSize; y++)
            for (var x = 0; x < cellSize; x++)
            {
                var a = buf[y * stride + x * 4 + 3];
                inside[x, y] = a > 40;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void FillSdfCell(byte[] dest, int atlasW, int atlasH, int ox, int oy, int cell, bool[,] inside, int spread)
    {
        var boundary = new List<(int x, int y)>();
        for (var y = 0; y < cell; y++)
        for (var x = 0; x < cell; x++)
        {
            if (!In(inside, x, y, cell)) continue;
            if (IsBoundary(inside, x, y, cell))
                boundary.Add((x, y));
        }
        if (boundary.Count == 0)
        {
            for (var y = 0; y < cell; y++)
            for (var x = 0; x < cell; x++)
            {
                var dx = ox + x;
                var dy = oy + y;
                if ((uint)dx < (uint)atlasW && (uint)dy < (uint)atlasH)
                    dest[dy * atlasW + dx] = 128;
            }
            return;
        }
        for (var y = 0; y < cell; y++)
        for (var x = 0; x < cell; x++)
        {
            var dx = ox + x;
            var dy = oy + y;
            if (dx < 0 || dy < 0 || dx >= atlasW || dy >= atlasH) continue;
            var ins = In(inside, x, y, cell);
            double minD = double.MaxValue;
            foreach (var (bx, by) in boundary)
            {
                var d = Math.Sqrt((x - bx) * (x - bx) + (y - by) * (y - by));
                if (d < minD) minD = d;
            }
            var sdf = ins ? minD : -minD;
            var t = sdf / spread;
            var b = (byte)Math.Clamp(128 + t * 127, 0, 255);
            dest[dy * atlasW + dx] = b;
        }
    }

    private static bool In(bool[,] inside, int x, int y, int n) =>
        (uint)x < (uint)n && (uint)y < (uint)n && inside[x, y];

    private static bool IsBoundary(bool[,] inside, int x, int y, int n)
    {
        if (!inside[x, y]) return false;
        return !In(inside, x - 1, y, n) || !In(inside, x + 1, y, n) ||
               !In(inside, x, y - 1, n) || !In(inside, x, y + 1, n);
    }

    private static void SaveGrayscalePng(string path, byte[] gray, int w, int h)
    {
        var bmp = BitmapSource.Create(w, h, 96, 96, PixelFormats.Gray8, null, gray, w);
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(bmp));
        using var fs = File.Create(path);
        enc.Save(fs);
    }
}

/// <summary>Descubre fuentes del proyecto y dispara la generación de atlas SDF.</summary>
public static class UiFontManager
{
    /// <summary>Rutas relativas al proyecto de archivos .ttf / .otf bajo Assets (recursivo).</summary>
    public static IReadOnlyList<string> FindProjectFontRelativePaths(string projectRoot)
    {
        if (string.IsNullOrWhiteSpace(projectRoot) || !Directory.Exists(projectRoot))
            return Array.Empty<string>();
        var assets = Path.Combine(projectRoot, "Assets");
        if (!Directory.Exists(assets)) return Array.Empty<string>();
        var list = new List<string>();
        foreach (var f in Directory.EnumerateFiles(assets, "*.*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(f).ToLowerInvariant();
            if (ext is not (".ttf" or ".otf")) continue;
            list.Add(Path.GetRelativePath(projectRoot, f).Replace('\\', '/'));
        }
        list.Sort(StringComparer.OrdinalIgnoreCase);
        return list;
    }

    /// <summary>Genera atlas en <c>Assets/Generated/FontSDF/&lt;nombre_fuente&gt;/</c>.</summary>
    public static bool TryGenerateSdfAtlasForFont(string projectRoot, string fontRelativePath, FontSdfAtlasGeneratorOptions? options, out string relativeOutDir, out string? error)
    {
        relativeOutDir = "";
        error = null;
        options ??= new FontSdfAtlasGeneratorOptions();
        var absFont = Path.GetFullPath(Path.Combine(projectRoot, fontRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!File.Exists(absFont))
        {
            error = "Archivo no encontrado.";
            return false;
        }
        var safeName = SanitizeFolderName(Path.GetFileNameWithoutExtension(absFont));
        relativeOutDir = $"Assets/Generated/FontSDF/{safeName}";
        var outAbs = Path.Combine(projectRoot, relativeOutDir.Replace('/', Path.DirectorySeparatorChar));
        if (!FontSdfAtlasGenerator.TryGenerate(absFont, outAbs, options, out error))
            return false;
        var readme = Path.Combine(outAbs, "README.txt");
        File.WriteAllText(readme,
            "Atlas SDF generado por FUEngine (preparación para render por shader).\n" +
            "- atlas_sdf.png : canal de distancia con signo (128 = borde del glifo).\n" +
            "- atlas_sdf.json : UVs y avances.\n" +
            "El visor WPF actual sigue usando texto vectorial; usa este atlas en Vulkan/custom.\n",
            Encoding.UTF8);
        return true;
    }

    private static string SanitizeFolderName(string name)
    {
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            if (char.IsLetterOrDigit(c) || c is '_' or '-') sb.Append(c);
            else sb.Append('_');
        }
        return sb.Length > 0 ? sb.ToString() : "font";
    }
}
