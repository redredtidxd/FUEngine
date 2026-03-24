namespace FUEngine.Core;

/// <summary>
/// Resolución efectiva del viewport de juego (píxeles lógicos), alineada con el visor en Play y el overlay del editor.
/// </summary>
public static class GameViewportMath
{
    /// <summary>
    /// Si <see cref="ProjectInfo.GameResolutionWidth"/> y Height son &gt; 0, son la resolución fija (redondeada a múltiplos de <see cref="ProjectInfo.TileSize"/> para alinear al grid).
    /// Si alguno es 0 («Auto»), se deriva: con viewport del editor (px del área de scroll / zoom) se usa ese tamaño en casillas; sin eso, mínimos ~12×10 casillas.
    /// </summary>
    public static void GetEffectiveResolutionPixels(ProjectInfo p, out int pixelWidth, out int pixelHeight,
        double editorViewportW = 0, double editorViewportH = 0, double editorZoom = 1)
    {
        int ts = Math.Max(1, p.TileSize);
        if (p.GameResolutionWidth > 0 && p.GameResolutionHeight > 0)
        {
            pixelWidth = Math.Max(ts, (p.GameResolutionWidth / ts) * ts);
            pixelHeight = Math.Max(ts, (p.GameResolutionHeight / ts) * ts);
            return;
        }

        double z = editorZoom > 0 ? editorZoom : 1.0;
        double visW = editorViewportW > 0 ? editorViewportW / z : 0;
        double visH = editorViewportH > 0 ? editorViewportH / z : 0;
        int tilesW;
        int tilesH;
        if (visW > 0 && visH > 0)
        {
            tilesW = Math.Max(4, (int)Math.Floor(visW / ts));
            tilesH = Math.Max(4, (int)Math.Floor(visH / ts));
        }
        else
        {
            tilesW = Math.Max(12, p.GameResolutionWidth / ts);
            tilesH = Math.Max(10, p.GameResolutionHeight / ts);
            if (tilesW == 0) tilesW = 12;
            if (tilesH == 0) tilesH = 10;
        }

        pixelWidth = tilesW * ts;
        pixelHeight = tilesH * ts;
    }

    /// <summary>Extensión del marco visible en casillas de mundo (coincide con píxeles / tile size cuando la resolución está alineada a tiles).</summary>
    public static void GetViewportSizeInWorldTiles(ProjectInfo p, out double widthTiles, out double heightTiles,
        double editorViewportW = 0, double editorViewportH = 0, double editorZoom = 1)
    {
        GetEffectiveResolutionPixels(p, out var pxW, out var pxH, editorViewportW, editorViewportH, editorZoom);
        int ts = Math.Max(1, p.TileSize);
        widthTiles = pxW / (double)ts;
        heightTiles = pxH / (double)ts;
    }

    /// <summary>Centro geométrico del mapa finito en casillas mundo (rectángulo [Origin, Origin+Size) en X/Y).</summary>
    public static void GetFiniteMapCenterWorldTile(ProjectInfo p, out double centerWx, out double centerWy)
    {
        int ox = p.MapBoundsOriginWorldTileX;
        int oy = p.MapBoundsOriginWorldTileY;
        int mw = Math.Max(1, p.MapWidth);
        int mh = Math.Max(1, p.MapHeight);
        centerWx = ox + mw * 0.5;
        centerWy = oy + mh * 0.5;
    }

    /// <summary>
    /// Coordenadas de HUD del editor: desplazamiento entero en casillas respecto al centro del rectángulo de juego (0,0 = centro aproximado en enteros).
    /// </summary>
    public static void GetTileCoordsRelativeToFiniteMapCenter(ProjectInfo p, int worldTx, int worldTy, out int relX, out int relY)
    {
        int mw = Math.Max(1, p.MapWidth);
        int mh = Math.Max(1, p.MapHeight);
        int ox = p.MapBoundsOriginWorldTileX;
        int oy = p.MapBoundsOriginWorldTileY;
        int cx = ox + mw / 2;
        int cy = oy + mh / 2;
        relX = worldTx - cx;
        relY = worldTy - cy;
    }

    /// <summary>Rectángulo de vista lógica en coordenadas mundo (casillas): esquina superior izquierda y tamaño.</summary>
    public static void GetVisibleWorldRectFromCenter(ProjectInfo p, double centerWorldX, double centerWorldY,
        out double leftWx, out double topWy, out double widthTiles, out double heightTiles,
        double editorViewportW = 0, double editorViewportH = 0, double editorZoom = 1)
    {
        GetViewportSizeInWorldTiles(p, out widthTiles, out heightTiles, editorViewportW, editorViewportH, editorZoom);
        leftWx = centerWorldX - widthTiles * 0.5;
        topWy = centerWorldY - heightTiles * 0.5;
    }

    /// <summary>
    /// Rectángulo de la <strong>cámara / render</strong> del juego en coordenadas del <strong>lienzo del mapa</strong> (px sin zoom de editor):
    /// tamaño = resolución interna lógica (<see cref="GetEffectiveResolutionPixels"/>), nunca el viewport WPF ni la pantalla.
    /// El zoom del editor aplica vía LayoutTransform sobre todo el MapCanvas.
    /// </summary>
    public static void GetCameraViewportRectInEditorCanvasPixels(ProjectInfo p,
        double centerWorldTilesX, double centerWorldTilesY,
        int canvasMinWxTiles, int canvasMinWyTiles,
        out double leftCanvasPx, out double topCanvasPx, out int widthPx, out int heightPx,
        double editorViewportW = 0, double editorViewportH = 0, double editorZoom = 1)
    {
        int ts = Math.Max(1, p.TileSize);
        GetEffectiveResolutionPixels(p, out widthPx, out heightPx, editorViewportW, editorViewportH, editorZoom);
        double wTiles = widthPx / (double)ts;
        double hTiles = heightPx / (double)ts;
        double leftWx = centerWorldTilesX - wTiles * 0.5;
        double topWy = centerWorldTilesY - hTiles * 0.5;
        leftCanvasPx = Math.Round((leftWx - canvasMinWxTiles) * ts);
        topCanvasPx = Math.Round((topWy - canvasMinWyTiles) * ts);
    }

    /// <summary>Ajusta el centro del marco «área visible» para que el rectángulo de vista quede dentro del mapa finito en casillas mundo.</summary>
    public static void ClampViewportCenterToFiniteMap(ProjectInfo p,
        double editorViewportW = 0, double editorViewportH = 0, double editorZoom = 1)
    {
        if (p.Infinite) return;
        GetViewportSizeInWorldTiles(p, out var wt, out var ht, editorViewportW, editorViewportH, editorZoom);
        var ox = p.MapBoundsOriginWorldTileX;
        var oy = p.MapBoundsOriginWorldTileY;
        var mw = Math.Max(1, p.MapWidth);
        var mh = Math.Max(1, p.MapHeight);
        var cx = p.EditorViewportCenterWorldX;
        var cy = p.EditorViewportCenterWorldY;
        var halfW = wt * 0.5;
        var halfH = ht * 0.5;
        var leftBound = (double)ox;
        var rightBound = ox + mw;
        var topBound = (double)oy;
        var bottomBound = oy + mh;
        if (wt >= mw)
            cx = ox + mw * 0.5;
        else
        {
            var left = cx - halfW;
            var right = cx + halfW;
            if (left < leftBound) cx = leftBound + halfW;
            if (right > rightBound) cx = rightBound - halfW;
        }
        if (ht >= mh)
            cy = oy + mh * 0.5;
        else
        {
            var top = cy - halfH;
            var bottom = cy + halfH;
            if (top < topBound) cy = topBound + halfH;
            if (bottom > bottomBound) cy = bottomBound - halfH;
        }
        p.EditorViewportCenterWorldX = cx;
        p.EditorViewportCenterWorldY = cy;
    }
}
