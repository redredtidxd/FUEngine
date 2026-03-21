namespace FUEngine.Core;

/// <summary>
/// Resolución efectiva del viewport de juego (píxeles lógicos), alineada con el visor en Play y el overlay del editor.
/// </summary>
public static class GameViewportMath
{
    /// <summary>
    /// Si <see cref="ProjectInfo.GameResolutionWidth"/> y Height son &gt; 0, son la resolución fija.
    /// Si es "Auto" (0), coincide con el visor de Play: ~12×10 casillas en unidades de <see cref="ProjectInfo.TileSize"/> px.
    /// </summary>
    public static void GetEffectiveResolutionPixels(ProjectInfo p, out int pixelWidth, out int pixelHeight)
    {
        int ts = Math.Max(1, p.TileSize);
        if (p.GameResolutionWidth > 0 && p.GameResolutionHeight > 0)
        {
            pixelWidth = p.GameResolutionWidth;
            pixelHeight = p.GameResolutionHeight;
            return;
        }
        int tilesW = Math.Max(12, p.GameResolutionWidth / ts);
        int tilesH = Math.Max(10, p.GameResolutionHeight / ts);
        pixelWidth = tilesW * ts;
        pixelHeight = tilesH * ts;
    }

    /// <summary>Extensión del marco visible en casillas de mundo (float, puede ser fracción si la resolución no es múltiplo exacto del tile).</summary>
    public static void GetViewportSizeInWorldTiles(ProjectInfo p, out double widthTiles, out double heightTiles)
    {
        GetEffectiveResolutionPixels(p, out var pxW, out var pxH);
        int ts = Math.Max(1, p.TileSize);
        widthTiles = pxW / (double)ts;
        heightTiles = pxH / (double)ts;
    }
}
