using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FUEngine;

/// <summary>
/// Genera sprites placeholder (colores sólidos o gradiente) para testear sin arte final.
/// Integración con IA: configurar EngineSettings.AiIntegrationPath para llamar a un script/API externo.
/// </summary>
public static class PlaceholderGenerator
{
    public static string? GenerateSpritePlaceholder(string projectDirectory, string name = "placeholder", int size = 32, byte r = 0x58, byte g = 0xa6, byte b = 0xff)
    {
        if (string.IsNullOrEmpty(projectDirectory)) return null;
        var assetsDir = Path.Combine(projectDirectory, "assets");
        if (!Directory.Exists(assetsDir)) Directory.CreateDirectory(assetsDir);
        var fileName = $"{name}.png";
        var path = Path.Combine(assetsDir, fileName);
        var bmp = new WriteableBitmap(size, size, 96, 96, PixelFormats.Bgr32, null);
        int stride = size * 4;
        var pixels = new byte[size * size * 4];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int i = (y * size + x) * 4;
                pixels[i] = b;
                pixels[i + 1] = g;
                pixels[i + 2] = r;
                pixels[i + 3] = 255;
            }
        bmp.WritePixels(new System.Windows.Int32Rect(0, 0, size, size), pixels, stride, 0);
        using var stream = File.Create(path);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bmp));
        encoder.Save(stream);
        return path;
    }
}
