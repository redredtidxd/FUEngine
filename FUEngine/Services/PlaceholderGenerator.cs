using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FUEngine;

/// <summary>Genera PNG mínimos para pruebas sin arte final (explorador del proyecto).</summary>
public static class PlaceholderGenerator
{
    /// <summary>Crea un PNG en <c>Assets/&lt;baseName&gt;.png</c> con un patrón de tablero.</summary>
    /// <returns>Ruta absoluta del archivo o null si falla.</returns>
    public static string? GenerateSpritePlaceholder(string projectDirectory, string baseName, int size)
    {
        if (string.IsNullOrEmpty(projectDirectory) || string.IsNullOrEmpty(baseName) || size < 1)
            return null;

        var assets = Path.Combine(projectDirectory, "Assets");
        if (!Directory.Exists(assets))
            Directory.CreateDirectory(assets);

        var path = Path.Combine(assets, baseName + ".png");
        try
        {
            var bytes = new byte[size * size * 4];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool dark = ((x / 8) + (y / 8)) % 2 == 0;
                    int i = (y * size + x) * 4;
                    bytes[i] = dark ? (byte)96 : (byte)64;
                    bytes[i + 1] = dark ? (byte)180 : (byte)120;
                    bytes[i + 2] = dark ? (byte)255 : (byte)200;
                    bytes[i + 3] = 255;
                }
            }

            var bmp = new WriteableBitmap(size, size, 96, 96, PixelFormats.Bgra32, null);
            bmp.WritePixels(new Int32Rect(0, 0, size, size), bytes, size * 4, 0);
            bmp.Freeze();

            using var stream = File.Create(path);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            encoder.Save(stream);
            return path;
        }
        catch
        {
            return null;
        }
    }
}
