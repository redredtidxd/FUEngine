using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FUEngine;

/// <summary>
/// Carga una imagen (PNG, JPG, etc.) a RGBA con tamaño de salida opcional.
/// </summary>
public static class TileImageLoader
{
    /// <summary>
    /// Carga la imagen y devuelve píxeles RGBA (R, G, B, A por píxel, row-major).
    /// Si outWidth/outHeight se especifican y difieren del tamaño de la imagen, se muestrea.
    /// Devuelve null si falla la carga.
    /// </summary>
    public static byte[]? LoadToRgba(string fullPath, int? outWidth = null, int? outHeight = null)
    {
        if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath)) return null;
        try
        {
            var uri = new Uri(fullPath, UriKind.Absolute);
            var decoder = BitmapDecoder.Create(uri, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            var frame = decoder.Frames[0];
            if (frame == null) return null;
            int srcW = frame.PixelWidth;
            int srcH = frame.PixelHeight;
            if (srcW <= 0 || srcH <= 0) return null;
            var converted = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
            int stride = srcW * 4;
            var srcPixels = new byte[srcH * stride];
            converted.CopyPixels(srcPixels, stride, 0);
            int dstW = outWidth ?? srcW;
            int dstH = outHeight ?? srcH;
            if (dstW == srcW && dstH == srcH)
            {
                var rgba = new byte[srcW * srcH * 4];
                for (int i = 0; i < srcPixels.Length; i += 4)
                {
                    rgba[i] = srcPixels[i + 2];     // R
                    rgba[i + 1] = srcPixels[i + 1]; // G
                    rgba[i + 2] = srcPixels[i];     // B
                    rgba[i + 3] = srcPixels[i + 3]; // A
                }
                return rgba;
            }
            var result = new byte[dstW * dstH * 4];
            for (int dy = 0; dy < dstH; dy++)
                for (int dx = 0; dx < dstW; dx++)
                {
                    int sx = (dx * srcW) / dstW;
                    int sy = (dy * srcH) / dstH;
                    if (sx >= srcW) sx = srcW - 1;
                    if (sy >= srcH) sy = srcH - 1;
                    int si = (sy * srcW + sx) * 4;
                    int di = (dy * dstW + dx) * 4;
                    result[di] = srcPixels[si + 2];
                    result[di + 1] = srcPixels[si + 1];
                    result[di + 2] = srcPixels[si];
                    result[di + 3] = srcPixels[si + 3];
                }
            return result;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Carga una imagen spritesheet (tira horizontal de frames) y devuelve los píxeles RGBA del frame indicado.
    /// frameIndex debe estar en [0, frameCount). La imagen debe tener ancho &gt;= frameW * frameCount y alto &gt;= frameH.
    /// </summary>
    public static byte[]? LoadFrameToRgba(string fullPath, int frameW, int frameH, int frameIndex, int frameCount)
    {
        if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath) || frameCount <= 0 || frameIndex < 0 || frameIndex >= frameCount) return null;
        try
        {
            var uri = new Uri(fullPath, UriKind.Absolute);
            var decoder = BitmapDecoder.Create(uri, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            var frame = decoder.Frames[0];
            if (frame == null) return null;
            int srcW = frame.PixelWidth;
            int srcH = frame.PixelHeight;
            if (srcW < frameW * frameCount || srcH < frameH) return null;
            var converted = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
            int srcStride = srcW * 4;
            var srcPixels = new byte[srcH * srcStride];
            converted.CopyPixels(srcPixels, srcStride, 0);
            int srcX0 = frameIndex * frameW;
            var result = new byte[frameW * frameH * 4];
            for (int dy = 0; dy < frameH; dy++)
            for (int dx = 0; dx < frameW; dx++)
            {
                int si = (dy * srcW + srcX0 + dx) * 4;
                int di = (dy * frameW + dx) * 4;
                result[di] = srcPixels[si + 2];
                result[di + 1] = srcPixels[si + 1];
                result[di + 2] = srcPixels[si];
                result[di + 3] = srcPixels[si + 3];
            }
            return result;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Recorta una celda del atlas (grid cellW×cellH, id en orden fila mayor) y opcionalmente escala a outW×outH.
    /// </summary>
    public static byte[]? LoadAtlasTileToRgba(string fullPath, int cellW, int cellH, int tileId, int? outWidth = null, int? outHeight = null)
    {
        if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath) || cellW <= 0 || cellH <= 0 || tileId < 0)
            return null;
        try
        {
            var uri = new Uri(fullPath, UriKind.Absolute);
            var decoder = BitmapDecoder.Create(uri, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            var frame = decoder.Frames[0];
            if (frame == null) return null;
            int imgW = frame.PixelWidth;
            int imgH = frame.PixelHeight;
            if (imgW <= 0 || imgH <= 0) return null;
            int cols = Math.Max(1, imgW / cellW);
            int rows = Math.Max(1, imgH / cellH);
            int col = tileId % cols;
            int row = tileId / cols;
            if (row >= rows || col >= cols) return null;
            int x0 = col * cellW;
            int y0 = row * cellH;
            var converted = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
            int srcStride = imgW * 4;
            var srcPixels = new byte[imgH * srcStride];
            converted.CopyPixels(srcPixels, srcStride, 0);
            var cellPixels = new byte[cellH * cellW * 4];
            for (int dy = 0; dy < cellH; dy++)
            for (int dx = 0; dx < cellW; dx++)
            {
                int sx = x0 + dx;
                int sy = y0 + dy;
                if (sx >= imgW) sx = imgW - 1;
                if (sy >= imgH) sy = imgH - 1;
                int si = (sy * imgW + sx) * 4;
                int di = (dy * cellW + dx) * 4;
                cellPixels[di] = srcPixels[si + 2];
                cellPixels[di + 1] = srcPixels[si + 1];
                cellPixels[di + 2] = srcPixels[si];
                cellPixels[di + 3] = srcPixels[si + 3];
            }
            int dstW = outWidth ?? cellW;
            int dstH = outHeight ?? cellH;
            if (dstW == cellW && dstH == cellH)
                return cellPixels;
            var result = new byte[dstW * dstH * 4];
            for (int dy = 0; dy < dstH; dy++)
            for (int dx = 0; dx < dstW; dx++)
            {
                int sx = (dx * cellW) / dstW;
                int sy = (dy * cellH) / dstH;
                if (sx >= cellW) sx = cellW - 1;
                if (sy >= cellH) sy = cellH - 1;
                int si = (sy * cellW + sx) * 4;
                int di = (dy * dstW + dx) * 4;
                result[di] = cellPixels[si];
                result[di + 1] = cellPixels[si + 1];
                result[di + 2] = cellPixels[si + 2];
                result[di + 3] = cellPixels[si + 3];
            }
            return result;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Recorta un rectángulo arbitrario del atlas (píxeles fuente) y escala a outW×outH.</summary>
    public static byte[]? LoadAtlasSubRectToRgba(string fullPath, int srcX, int srcY, int srcW, int srcH, int outW, int outH)
    {
        if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath) || srcW <= 0 || srcH <= 0 || outW <= 0 || outH <= 0)
            return null;
        try
        {
            var uri = new Uri(fullPath, UriKind.Absolute);
            var decoder = BitmapDecoder.Create(uri, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            var frame = decoder.Frames[0];
            if (frame == null) return null;
            int imgW = frame.PixelWidth;
            int imgH = frame.PixelHeight;
            if (imgW <= 0 || imgH <= 0) return null;
            srcX = Math.Clamp(srcX, 0, Math.Max(0, imgW - 1));
            srcY = Math.Clamp(srcY, 0, Math.Max(0, imgH - 1));
            srcW = Math.Min(srcW, imgW - srcX);
            srcH = Math.Min(srcH, imgH - srcY);
            if (srcW <= 0 || srcH <= 0) return null;
            var converted = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
            int stride = imgW * 4;
            var srcPixels = new byte[imgH * stride];
            converted.CopyPixels(srcPixels, stride, 0);
            var crop = new byte[srcW * srcH * 4];
            for (int dy = 0; dy < srcH; dy++)
            for (int dx = 0; dx < srcW; dx++)
            {
                int sx = srcX + dx;
                int sy = srcY + dy;
                int si = (sy * imgW + sx) * 4;
                int di = (dy * srcW + dx) * 4;
                crop[di] = srcPixels[si + 2];
                crop[di + 1] = srcPixels[si + 1];
                crop[di + 2] = srcPixels[si];
                crop[di + 3] = srcPixels[si + 3];
            }
            if (outW == srcW && outH == srcH)
                return crop;
            var result = new byte[outW * outH * 4];
            for (int dy = 0; dy < outH; dy++)
            for (int dx = 0; dx < outW; dx++)
            {
                int sx = (dx * srcW) / outW;
                int sy = (dy * srcH) / outH;
                if (sx >= srcW) sx = srcW - 1;
                if (sy >= srcH) sy = srcH - 1;
                int si = (sy * srcW + sx) * 4;
                int di = (dy * outW + dx) * 4;
                result[di] = crop[si];
                result[di + 1] = crop[si + 1];
                result[di + 2] = crop[si + 2];
                result[di + 3] = crop[si + 3];
            }
            return result;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Obtiene el color del píxel (dx, dy) desde un buffer RGBA de tamaño (width, height).
    /// </summary>
    public static System.Windows.Media.Color GetPixelColor(byte[] rgba, int width, int height, int dx, int dy)
    {
        if (rgba == null || dx < 0 || dx >= width || dy < 0 || dy >= height) return Colors.Black;
        int i = (dy * width + dx) * 4;
        if (i + 3 >= rgba.Length) return Colors.Black;
        return System.Windows.Media.Color.FromArgb(rgba[i + 3], rgba[i], rgba[i + 1], rgba[i + 2]);
    }
}
