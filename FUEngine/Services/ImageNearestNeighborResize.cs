using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FUEngine;

/// <summary>Reescala imágenes raster con <see cref="BitmapScalingMode.NearestNeighbor"/> (pixel-art / sin suavizado).</summary>
public static class ImageNearestNeighborResize
{
    public const int MaxDimension = 8192;

    /// <summary>Reescala y guarda en disco. PNG/JPEG según extensión de <paramref name="destinationPath"/>.</summary>
    public static bool TryResizeToFile(string sourcePath, string destinationPath, int width, int height, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            error = "No se encuentra el archivo de origen.";
            return false;
        }

        if (width < 1 || height < 1 || width > MaxDimension || height > MaxDimension)
        {
            error = $"El tamaño debe estar entre 1 y {MaxDimension} px.";
            return false;
        }

        BitmapSource? source;
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(Path.GetFullPath(sourcePath), UriKind.Absolute);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bmp.EndInit();
            if (bmp.CanFreeze) bmp.Freeze();
            source = bmp;
        }
        catch (Exception ex)
        {
            error = $"No se pudo cargar la imagen: {ex.Message}";
            return false;
        }

        try
        {
            var converted = new FormatConvertedBitmap(source, PixelFormats.Pbgra32, null, 0);
            if (converted.CanFreeze) converted.Freeze();

            var visual = new DrawingVisual();
            RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.NearestNeighbor);
            using (var dc = visual.RenderOpen())
            {
                dc.DrawImage(converted, new Rect(0, 0, width, height));
            }

            var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);
            if (rtb.CanFreeze) rtb.Freeze();

            var ext = Path.GetExtension(destinationPath).ToLowerInvariant();
            BitmapEncoder encoder = ext switch
            {
                ".jpg" or ".jpeg" => new JpegBitmapEncoder { QualityLevel = 95 },
                ".bmp" => new BmpBitmapEncoder(),
                _ => new PngBitmapEncoder()
            };
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            var dir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using (var stream = File.Create(destinationPath))
                encoder.Save(stream);

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
