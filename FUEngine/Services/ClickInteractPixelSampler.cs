using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FUEngine.Core;

namespace FUEngine;

/// <summary>Muestra un píxel del sprite en coordenadas mundo para hit-test «pixel perfect» (alpha).</summary>
public static class ClickInteractPixelSampler
{
    const int AlphaThreshold = 24;

    /// <summary>
    /// Comprueba si <paramref name="worldX"/>/<paramref name="worldY"/> cae en un píxel opaco del sprite de la definición.
    /// Usa el rectángulo de la definición (ancho×alto en casillas) centrado en <see cref="Transform"/> con rotación/escala.
    /// </summary>
    public static bool TryHitOpaque(GameObject go, ObjectDefinition? def, ObjectInstance? inst, TextureAssetCache? cache, double worldX, double worldY)
    {
        if (go?.Transform == null || def == null || cache == null) return false;
        var rel = TextureAssetCache.NormalizeRelativePath(def.SpritePath);
        if (string.IsNullOrEmpty(rel)) return false;
        var bmp = cache.GetOrLoad(rel);
        if (bmp == null) return false;

        double cx = go.Transform.X;
        double cy = go.Transform.Y;
        double dx = worldX - cx;
        double dy = worldY - cy;
        double rad = -go.Transform.RotationDegrees * (Math.PI / 180.0);
        double cos = Math.Cos(rad);
        double sin = Math.Sin(rad);
        double lx = dx * cos - dy * sin;
        double ly = dx * sin + dy * cos;

        double sx = Math.Abs(go.Transform.ScaleX) > 1e-6 ? go.Transform.ScaleX : 1f;
        double sy = Math.Abs(go.Transform.ScaleY) > 1e-6 ? go.Transform.ScaleY : 1f;
        double hw = Math.Max(1e-4, def.Width * Math.Abs(sx) * 0.5);
        double hh = Math.Max(1e-4, def.Height * Math.Abs(sy) * 0.5);
        if (lx < -hw || lx > hw || ly < -hh || ly > hh) return false;

        double u = (lx + hw) / (2.0 * hw);
        double v = (ly + hh) / (2.0 * hh);
        if (inst?.SpriteFlipX == true) u = 1.0 - u;
        if (inst?.SpriteFlipY == true) v = 1.0 - v;
        u = Math.Clamp(u, 0, 1);
        v = Math.Clamp(v, 0, 1);

        return SampleAlpha(bmp, u, v) >= AlphaThreshold;
    }

    static int SampleAlpha(BitmapSource bmp, double u, double v)
    {
        int w = bmp.PixelWidth;
        int h = bmp.PixelHeight;
        if (w <= 0 || h <= 0) return 0;
        int x = (int)Math.Floor(u * (w - 1));
        int y = (int)Math.Floor((1.0 - v) * (h - 1));
        x = Math.Clamp(x, 0, w - 1);
        y = Math.Clamp(y, 0, h - 1);

        try
        {
            var rect = new Int32Rect(x, y, 1, 1);
            var fmt = bmp.Format;
            if (fmt != PixelFormats.Bgra32 && fmt != PixelFormats.Pbgra32)
            {
                var conv = new FormatConvertedBitmap(bmp, PixelFormats.Bgra32, null, 0);
                conv.Freeze();
                bmp = conv;
            }
            var px = new byte[4];
            bmp.CopyPixels(rect, px, 4, 1);
            return px[3];
        }
        catch
        {
            return 0;
        }
    }
}
