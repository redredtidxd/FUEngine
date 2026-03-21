using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FUEngine.Rendering;

/// <summary>Multiplica RGB de un bitmap (visor WPF / luces de color).</summary>
public static class SpriteBitmapTint
{
    public static BitmapSource? MultiplyRgb(BitmapSource? source, double mulR, double mulG, double mulB)
    {
        if (source == null) return null;
        if (mulR >= 0.99 && mulR <= 1.01 && mulG >= 0.99 && mulG <= 1.01 && mulB >= 0.99 && mulB <= 1.01)
            return source;
        int w = source.PixelWidth;
        int h = source.PixelHeight;
        if (w <= 0 || h <= 0) return source;
        var fmt = PixelFormats.Bgra32;
        int stride = w * 4;
        var px = new byte[stride * h];
        source.CopyPixels(px, stride, 0);
        for (int i = 0; i < px.Length; i += 4)
        {
            px[i] = (byte)Math.Clamp(px[i] * mulB, 0, 255);
            px[i + 1] = (byte)Math.Clamp(px[i + 1] * mulG, 0, 255);
            px[i + 2] = (byte)Math.Clamp(px[i + 2] * mulR, 0, 255);
        }
        var wb = new WriteableBitmap(w, h, 96, 96, fmt, null);
        wb.WritePixels(new Int32Rect(0, 0, w, h), px, stride, 0);
        wb.Freeze();
        return wb;
    }
}
