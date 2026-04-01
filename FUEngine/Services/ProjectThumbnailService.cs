using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FUEngine;

/// <summary>Guarda una miniatura PNG del lienzo del mapa para el Hub (<see cref="FUEngineAppPaths.ProjectThumbnailsDirectory"/>).</summary>
public static class ProjectThumbnailService
{
    /// <summary>Intenta capturar el elemento (p. ej. <c>MapCanvas</c>) y guardar PNG. Requiere tamaño &gt; 0.</summary>
    public static bool TrySaveHubThumbnail(FrameworkElement? visual, string projectJsonPath)
    {
        if (visual == null || string.IsNullOrWhiteSpace(projectJsonPath)) return false;
        try
        {
            visual.UpdateLayout();
            var srcW = (int)Math.Max(1, visual.ActualWidth);
            var srcH = (int)Math.Max(1, visual.ActualHeight);
            if (visual.ActualWidth <= 0 || visual.ActualHeight <= 0) return false;

            FUEngineAppPaths.EnsureLayout();
            var outPath = FUEngineAppPaths.GetThumbnailPathForProjectJson(projectJsonPath);
            var dir = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var full = new RenderTargetBitmap(srcW, srcH, 96, 96, PixelFormats.Pbgra32);
            full.Render(visual);

            const int maxSide = 512;
            BitmapSource toEncode = full;
            if (srcW > maxSide || srcH > maxSide)
            {
                var scale = Math.Min((double)maxSide / srcW, (double)maxSide / srcH);
                var scaled = new TransformedBitmap(full, new ScaleTransform(scale, scale));
                if (scaled.CanFreeze) scaled.Freeze();
                toEncode = scaled;
            }
            else if (full.CanFreeze)
                full.Freeze();

            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(toEncode));
            using var fs = File.Create(outPath);
            enc.Save(fs);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
