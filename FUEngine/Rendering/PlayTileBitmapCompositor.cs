using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FUEngine.Core;
using MediaColor = System.Windows.Media.Color;

namespace FUEngine.Rendering;

/// <summary>Genera bitmaps de tile para el viewport de juego (incluye atlas por <see cref="TileData.CatalogTileId"/>).</summary>
public static class PlayTileBitmapCompositor
{
    public static BitmapSource? CreateCompositedTileBitmap(string? projectDir, TileData data, double tileSizePx, double totalSeconds = 0)
    {
        int w = (int)tileSizePx;
        int h = (int)tileSizePx;
        if (w <= 0 || h <= 0) return null;
        var baseColor = GetBaseColorForTileType(data.TipoTile);
        byte[]? baseRgba = null;

        if (!string.IsNullOrWhiteSpace(data.SourceImagePath) && !string.IsNullOrWhiteSpace(projectDir))
        {
            var fullPath = Path.Combine(projectDir, data.SourceImagePath);
            if (data.AtlasSubRectW > 0 && data.AtlasSubRectH > 0 && File.Exists(fullPath))
            {
                baseRgba = TileImageLoader.LoadAtlasSubRectToRgba(fullPath, data.AtlasSubRectX, data.AtlasSubRectY, data.AtlasSubRectW, data.AtlasSubRectH, w, h);
            }
            else if (data.CatalogTileId > 0 && data.CatalogGridTileWidth > 0 && data.CatalogGridTileHeight > 0 && File.Exists(fullPath))
            {
                baseRgba = TileImageLoader.LoadAtlasTileToRgba(fullPath, data.CatalogGridTileWidth, data.CatalogGridTileHeight, data.CatalogTileId, w, h);
            }
            if (baseRgba == null && File.Exists(fullPath))
            {
                var tiledataPath = TileDataFile.GetTileDataPath(fullPath);
                var dto = TileDataFile.Load(tiledataPath);
                int frameCount = dto?.FrameCount ?? 1;
                int fps = dto?.Fps ?? 8;
                if (frameCount > 1 && fps > 0)
                {
                    int frameIndex = (int)(totalSeconds * fps) % frameCount;
                    if (frameIndex < 0) frameIndex = 0;
                    baseRgba = TileImageLoader.LoadFrameToRgba(fullPath, w, h, frameIndex, frameCount);
                }
                if (baseRgba == null)
                    baseRgba = TileImageLoader.LoadToRgba(fullPath, w, h);
            }
        }

        var overlay = data.PixelOverlay;
        var bgra = new byte[w * h * 4];
        int overlayStride = (overlay?.RgbaData != null && overlay.Width > 0 && overlay.Height > 0 && overlay.RgbaData.Length >= overlay.Width * overlay.Height * 4)
            ? overlay.Width * overlay.Height * 4 : 0;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            byte r = 0, g = 0, b = 0, a = 0;
            if (overlayStride > 0 && overlay != null)
            {
                int ox = (x * overlay.Width) / w;
                int oy = (y * overlay.Height) / h;
                if (ox >= overlay.Width) ox = overlay.Width - 1;
                if (oy >= overlay.Height) oy = overlay.Height - 1;
                (r, g, b, a) = overlay.GetPixel(ox, oy);
            }
            int i = (y * w + x) * 4;
            if (a > 0)
            {
                bgra[i] = b;
                bgra[i + 1] = g;
                bgra[i + 2] = r;
                bgra[i + 3] = a;
            }
            else if (baseRgba != null && baseRgba.Length >= (y * w + x) * 4 + 4)
            {
                int bi = (y * w + x) * 4;
                bgra[i] = baseRgba[bi + 2];
                bgra[i + 1] = baseRgba[bi + 1];
                bgra[i + 2] = baseRgba[bi];
                bgra[i + 3] = baseRgba[bi + 3];
            }
            else
            {
                bgra[i] = baseColor.B;
                bgra[i + 1] = baseColor.G;
                bgra[i + 2] = baseColor.R;
                bgra[i + 3] = 255;
            }
        }
        var bitmap = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgra32, null);
        bitmap.WritePixels(new Int32Rect(0, 0, w, h), bgra, w * 4, 0);
        return bitmap;
    }

    private static MediaColor GetBaseColorForTileType(TileType tipo)
    {
        return tipo switch
        {
            TileType.Suelo => MediaColor.FromRgb(80, 80, 80),
            TileType.Pared => MediaColor.FromRgb(120, 80, 60),
            TileType.Objeto => MediaColor.FromRgb(90, 90, 120),
            TileType.Especial => MediaColor.FromRgb(100, 60, 100),
            _ => MediaColor.FromRgb(80, 80, 80)
        };
    }
}
