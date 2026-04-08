using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Rectangle = System.Windows.Shapes.Rectangle;
using Brushes = System.Windows.Media.Brushes;
using Pen = System.Windows.Media.Pen;
using FUEngine.Core;
using FUEngine.Editor;
using FUEngine.Rendering;

namespace FUEngine;

/// <summary>
/// Renders the map canvas: tiles, objects, triggers, grid, and tool overlays.
/// </summary>
public sealed class MapRenderer
{
    /// <summary>
    /// Draws the full map to the canvas. Sets canvas size and updates context.CanvasMinWx/Wy.
    /// </summary>
    public void Draw(Canvas canvas, MapRenderContext ctx)
    {
        if (canvas == null || ctx?.TileMap == null || ctx.Project == null) return;

        var tileSize = ctx.Project.TileSize;
        ComputeCanvasBounds(ctx, out int canvasW, out int canvasH);
        ctx.CanvasMinWx = _lastMinWx;
        ctx.CanvasMinWy = _lastMinWy;

        canvas.Width = canvasW;
        canvas.Height = canvasH;
        canvas.Children.Clear();

        if (ctx.GridVisible)
            DrawGrid(canvas, ctx, tileSize);

        DrawBackgroundLayers(canvas, ctx);
        DrawTiles(canvas, ctx, tileSize);
        if (ctx.ShowColliders)
            DrawCollisionOverlay(canvas, ctx, tileSize);
        DrawObjects(canvas, ctx, tileSize);
        DrawTriggers(canvas, ctx, tileSize);
        DrawSelectionOverlay(canvas, ctx, tileSize);
        DrawToolOverlays(canvas, ctx, tileSize);
        if (!ctx.Project.Infinite)
            DrawFiniteMapBorderAndExpandZones(canvas, ctx, tileSize);
        if (ctx.ShowVisibleArea)
            DrawVisibleAreaFrame(canvas, ctx, tileSize);
        if (ctx.ShowStreamingGizmos && ctx.Project.ChunkSize > 0)
            DrawStreamingGizmos(canvas, ctx, tileSize);
        if (ctx.Project.ShowChunkBounds && ctx.Project.ChunkSize > 0)
            DrawChunkBounds(canvas, ctx, tileSize);
    }

    private void DrawBackgroundLayers(Canvas canvas, MapRenderContext ctx)
    {
        if (ctx.Project == null) return;
        var projectDir = ctx.Project.ProjectDirectory;
        if (string.IsNullOrEmpty(projectDir)) return;
        double ts = Math.Max(1, ctx.Project.TileSize);
        for (int i = 0; i < ctx.TileMap.Layers.Count; i++)
        {
            var descriptor = ctx.TileMap.Layers[i];
            if (string.IsNullOrWhiteSpace(descriptor.BackgroundTexturePath) || !descriptor.IsVisible) continue;
            var fullPath = System.IO.Path.Combine(projectDir, descriptor.BackgroundTexturePath);
            if (!File.Exists(fullPath)) continue;
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new System.Uri(fullPath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                double imgW = canvas.Width;
                double imgH = canvas.Height;
                double left = 0;
                double top = 0;
                if (!ctx.Project.Infinite)
                {
                    int ox = ctx.Project.MapBoundsOriginWorldTileX;
                    int oy = ctx.Project.MapBoundsOriginWorldTileY;
                    int mw = Math.Max(1, ctx.Project.MapWidth);
                    int mh = Math.Max(1, ctx.Project.MapHeight);
                    imgW = mw * ts;
                    imgH = mh * ts;
                    left = (ox - ctx.CanvasMinWx) * ts;
                    top = (oy - ctx.CanvasMinWy) * ts;
                }
                var img = new System.Windows.Controls.Image
                {
                    Source = bitmap,
                    Stretch = Stretch.UniformToFill,
                    Width = imgW,
                    Height = imgH,
                    Opacity = descriptor.Opacity / 100.0,
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(img, left);
                Canvas.SetTop(img, top);
                canvas.Children.Add(img);
            }
            catch { /* ignore load errors */ }
        }
    }

    private void DrawStreamingGizmos(Canvas canvas, MapRenderContext ctx, double tileSize)
    {
        int cs = Math.Max(1, ctx.Project.ChunkSize);
        int tx = 0, ty = 0;
        if (!string.IsNullOrEmpty(ctx.Project.ProtagonistInstanceId))
        {
            var hero = ctx.ObjectLayer.Instances.FirstOrDefault(i =>
                string.Equals(i.InstanceId, ctx.Project.ProtagonistInstanceId, StringComparison.Ordinal));
            if (hero != null)
            {
                tx = (int)Math.Floor(hero.X);
                ty = (int)Math.Floor(hero.Y);
            }
        }
        var (ccx, ccy) = ctx.TileMap.WorldTileToChunk(tx, ty);
        int rGameplay = Math.Max(0, ctx.Project.ChunkLoadRadius);
        int rKeep = rGameplay + Math.Max(0, ctx.Project.ChunkStreamEvictMargin);
        void strokeRect(int r, System.Windows.Media.Color color, double thickness, bool dash)
        {
            int minCx = ccx - r, maxCx = ccx + r, minCy = ccy - r, maxCy = ccy + r;
            int minWx = minCx * cs, minWy = minCy * cs;
            int span = (maxCx - minCx + 1) * cs;
            double left = (minWx - ctx.CanvasMinWx) * tileSize;
            double top = (minWy - ctx.CanvasMinWy) * tileSize;
            double w = span * tileSize;
            double h = (maxCy - minCy + 1) * cs * tileSize;
            var rect = new Rectangle
            {
                Width = w,
                Height = h,
                Fill = Brushes.Transparent,
                Stroke = new SolidColorBrush(System.Windows.Media.Color.FromArgb(220, color.R, color.G, color.B)),
                StrokeThickness = thickness,
                IsHitTestVisible = false
            };
            if (dash)
                rect.StrokeDashArray = new System.Windows.Media.DoubleCollection(new[] { 6.0, 4.0 });
            Canvas.SetLeft(rect, left);
            Canvas.SetTop(rect, top);
            canvas.Children.Add(rect);
        }
        strokeRect(rKeep, System.Windows.Media.Color.FromRgb(0xd2, 0x99, 0x22), 2, true);
        strokeRect(rGameplay, System.Windows.Media.Color.FromRgb(0x3f, 0xb9, 0x50), 2, false);
    }

    private void DrawChunkBounds(Canvas canvas, MapRenderContext ctx, double tileSize)
    {
        int cs = ctx.Project.ChunkSize;
        if (cs <= 0) return;
        GetClampedTileRangeForDrawing(canvas, ctx, tileSize, out int minTx, out int minTy, out int maxTx, out int maxTy);
        int startCx = (int)Math.Floor((double)minTx / cs);
        int endCx = (int)Math.Ceiling((double)maxTx / cs);
        int startCy = (int)Math.Floor((double)minTy / cs);
        int endCy = (int)Math.Ceiling((double)maxTy / cs);
        var lineBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(120, 0x58, 0xa6, 0xff));
        for (int cx = startCx; cx <= endCx; cx++)
        {
            int wx = cx * cs;
            double x = (wx - ctx.CanvasMinWx) * tileSize;
            var line = new System.Windows.Shapes.Line
            {
                X1 = x, Y1 = 0,
                X2 = x, Y2 = canvas.Height,
                Stroke = lineBrush,
                StrokeThickness = 1,
                IsHitTestVisible = false
            };
            canvas.Children.Add(line);
        }
        for (int cy = startCy; cy <= endCy; cy++)
        {
            int wy = cy * cs;
            double y = (wy - ctx.CanvasMinWy) * tileSize;
            var line = new System.Windows.Shapes.Line
            {
                X1 = 0, Y1 = y,
                X2 = canvas.Width, Y2 = y,
                Stroke = lineBrush,
                StrokeThickness = 1,
                IsHitTestVisible = false
            };
            canvas.Children.Add(line);
        }
    }

    private void DrawVisibleAreaFrame(Canvas canvas, MapRenderContext ctx, double tileSize)
    {
        double camX = ctx.Project.EditorViewportCenterWorldX;
        double camY = ctx.Project.EditorViewportCenterWorldY;
        GameViewportMath.GetCameraViewportRectInEditorCanvasPixels(ctx.Project, camX, camY, ctx.CanvasMinWx, ctx.CanvasMinWy,
            out double leftRaw, out double topRaw, out int gwPx, out int ghPx,
            ctx.EditorViewportWidth, ctx.EditorViewportHeight, ctx.EditorZoom);
        // Marco = resolución interna del juego (cámara/render); en Auto usa el viewport del scroll del editor.
        double left = leftRaw;
        double top = topRaw;
        double widthCanvas = Math.Max(1, gwPx);
        double heightCanvas = Math.Max(1, ghPx);
        GameViewportMath.GetVisibleWorldRectFromCenter(ctx.Project, camX, camY, out double leftWx, out double topWy, out double worldWTiles, out double worldHTiles,
            ctx.EditorViewportWidth, ctx.EditorViewportHeight, ctx.EditorZoom);
        var rect = new Rectangle
        {
            Width = widthCanvas,
            Height = heightCanvas,
            Fill = Brushes.Transparent,
            Stroke = new SolidColorBrush(System.Windows.Media.Color.FromArgb(220, 0x58, 0xa6, 0xff)),
            StrokeThickness = 1,
            StrokeDashArray = new System.Windows.Media.DoubleCollection(new[] { 4.0, 4.0 }),
            IsHitTestVisible = false,
            SnapsToDevicePixels = true
        };
        RenderOptions.SetEdgeMode(rect, EdgeMode.Aliased);
        Canvas.SetLeft(rect, left);
        Canvas.SetTop(rect, top);
        canvas.Children.Add(rect);

        string line = $"{gwPx}×{ghPx} px  ·  {worldWTiles:F1}×{worldHTiles:F1} casillas";
        var lbl = new TextBlock
        {
            Text = line,
            FontSize = 10,
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(230, 0xe6, 0xed, 0xf3)),
            Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(180, 0x13, 0x17, 0x1c)),
            Padding = new Thickness(4, 2, 4, 2),
            TextWrapping = TextWrapping.NoWrap,
            MaxWidth = Math.Max(160, widthCanvas - 8),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(lbl, left + 4);
        Canvas.SetTop(lbl, top + 4);
        canvas.Children.Add(lbl);
    }

    private int _lastMinWx;
    private int _lastMinWy;
    private readonly HashSet<(int cx, int cy)> _expandTargetChunksScratch = new();
    /// <summary>Mapas infinitos: el tamaño del lienzo no se reduce para evitar que el thumb del scroll cambie de tamaño al moverse.</summary>
    private int _infiniteStickyCanvasW;
    private int _infiniteStickyCanvasH;

    /// <summary>Reinicia la extensión acumulada del lienzo (p. ej. al cargar otro proyecto).</summary>
    public void ResetInfiniteScrollExtent()
    {
        _infiniteStickyCanvasW = 0;
        _infiniteStickyCanvasH = 0;
    }

    private void ComputeCanvasBounds(MapRenderContext ctx, out int canvasW, out int canvasH)
    {
        var tileSize = Math.Max(1, ctx.Project.TileSize);
        var tileMap = ctx.TileMap;
        if (ctx.Project.Infinite)
        {
            // Ventana deslizante: solo viewport (scroll) + cámara (marco azul) + padding por chunks.
            // Nunca unir todo el bounding box del mapa (provoca lienzos gigantes y cuelgue de WPF).
            int cs = Math.Max(1, tileMap.ChunkSize);
            int margin = Math.Max(1, cs / 4);
            int padChunks = Math.Clamp(ctx.Project.ChunkLoadRadius, 2, 8);
            int padTiles = padChunks * cs;
            const int MaxTilesPerAxis = 384;

            double z = ctx.EditorZoom > 0 ? ctx.EditorZoom : 1.0;
            double leftPx = ctx.EditorScrollHorizontalOffset / z;
            double topPx = ctx.EditorScrollVerticalOffset / z;
            double visW = Math.Max(1, ctx.EditorViewportWidth / z);
            double visH = Math.Max(1, ctx.EditorViewportHeight / z);

            // Esquina superior izquierda del viewport en coordenadas mundo (float), sin depender de desbordes int.
            double worldLeftX = ctx.PreviousCanvasMinWx + leftPx / tileSize;
            double worldTopY = ctx.PreviousCanvasMinWy + topPx / tileSize;

            int vLeftTx = (int)Math.Floor(worldLeftX);
            int vTopTy = (int)Math.Floor(worldTopY);
            int vRightTx = (int)Math.Floor(worldLeftX + visW / tileSize - 1e-6);
            int vBottomTy = (int)Math.Floor(worldTopY + visH / tileSize - 1e-6);
            if (vRightTx < vLeftTx) vRightTx = vLeftTx;
            if (vBottomTy < vTopTy) vBottomTy = vTopTy;

            int worldMinX = vLeftTx - padTiles;
            int worldMinY = vTopTy - padTiles;
            int worldMaxX = vRightTx + padTiles;
            int worldMaxY = vBottomTy + padTiles;

            GameViewportMath.GetVisibleWorldRectFromCenter(ctx.Project,
                ctx.Project.EditorViewportCenterWorldX, ctx.Project.EditorViewportCenterWorldY,
                out double camLeftWx, out double camTopWy, out double camWTiles, out double camHTiles,
                ctx.EditorViewportWidth, ctx.EditorViewportHeight, ctx.EditorZoom);
            int camMinX = (int)Math.Floor(camLeftWx) - padTiles;
            int camMinY = (int)Math.Floor(camTopWy) - padTiles;
            int camMaxX = (int)Math.Ceiling(camLeftWx + camWTiles) - 1 + padTiles;
            int camMaxY = (int)Math.Ceiling(camTopWy + camHTiles) - 1 + padTiles;

            worldMinX = Math.Min(worldMinX, camMinX);
            worldMinY = Math.Min(worldMinY, camMinY);
            worldMaxX = Math.Max(worldMaxX, camMaxX);
            worldMaxY = Math.Max(worldMaxY, camMaxY);

            int spanX = worldMaxX - worldMinX + 1;
            int spanY = worldMaxY - worldMinY + 1;
            // Si el unión viewport+cámara supera el tope, no centrar en el "medio global" (desplaza viewport y rompe el scroll).
            // Prioridad: ventana centrada en lo que el usuario está viendo; luego desplazar lo mínimo para incluir el marco de cámara.
            if (spanX > MaxTilesPerAxis)
            {
                int half = MaxTilesPerAxis / 2;
                int vMid = (vLeftTx + vRightTx) / 2;
                worldMinX = vMid - half;
                worldMaxX = worldMinX + MaxTilesPerAxis - 1;
                if (camMinX < worldMinX)
                {
                    int shift = camMinX - worldMinX;
                    worldMinX += shift;
                    worldMaxX += shift;
                }
                else if (camMaxX > worldMaxX)
                {
                    int shift = camMaxX - worldMaxX;
                    worldMinX += shift;
                    worldMaxX += shift;
                }
            }
            if (spanY > MaxTilesPerAxis)
            {
                int half = MaxTilesPerAxis / 2;
                int vMid = (vTopTy + vBottomTy) / 2;
                worldMinY = vMid - half;
                worldMaxY = worldMinY + MaxTilesPerAxis - 1;
                if (camMinY < worldMinY)
                {
                    int shift = camMinY - worldMinY;
                    worldMinY += shift;
                    worldMaxY += shift;
                }
                else if (camMaxY > worldMaxY)
                {
                    int shift = camMaxY - worldMaxY;
                    worldMinY += shift;
                    worldMaxY += shift;
                }
            }

            _lastMinWx = worldMinX - margin;
            _lastMinWy = worldMinY - margin;
            int rangeX = (worldMaxX - worldMinX + 1) + margin * 2;
            int rangeY = (worldMaxY - worldMinY + 1) + margin * 2;
            int minW = (int)Math.Ceiling(visW);
            int minH = (int)Math.Ceiling(visH);
            int computedW = Math.Max(rangeX * tileSize, minW);
            int computedH = Math.Max(rangeY * tileSize, minH);
            canvasW = Math.Max(computedW, _infiniteStickyCanvasW);
            canvasH = Math.Max(computedH, _infiniteStickyCanvasH);
            _infiniteStickyCanvasW = canvasW;
            _infiniteStickyCanvasH = canvasH;
        }
        else
        {
            // Un chunk de margen alrededor para dibujar bordes y botones «+» fuera del área jugable.
            int cs = Math.Max(1, ctx.Project.ChunkSize);
            int ox = ctx.Project.MapBoundsOriginWorldTileX;
            int oy = ctx.Project.MapBoundsOriginWorldTileY;
            int mw = Math.Max(1, ctx.Project.MapWidth);
            int mh = Math.Max(1, ctx.Project.MapHeight);
            _lastMinWx = ox - cs;
            _lastMinWy = oy - cs;
            canvasW = Math.Max((mw + 2 * cs) * tileSize, 800);
            canvasH = Math.Max((mh + 2 * cs) * tileSize, 600);
        }
    }

    /// <summary>Marco del mapa finito y una zona «+» por chunk en la frontera: cada clic añade un chunk vacío (ChunkSize×ChunkSize casillas).</summary>
    private void DrawFiniteMapBorderAndExpandZones(Canvas canvas, MapRenderContext ctx, double tileSize)
    {
        int cs = Math.Max(1, ctx.Project.ChunkSize);
        int ox = ctx.Project.MapBoundsOriginWorldTileX;
        int oy = ctx.Project.MapBoundsOriginWorldTileY;
        int mw = Math.Max(1, ctx.Project.MapWidth);
        int mh = Math.Max(1, ctx.Project.MapHeight);
        double innerLeft = ToCanvasX(ctx, ox);
        double innerTop = ToCanvasY(ctx, oy);
        double innerW = mw * tileSize;
        double innerH = mh * tileSize;
        var border = new Rectangle
        {
            Width = innerW,
            Height = innerH,
            Fill = Brushes.Transparent,
            Stroke = new SolidColorBrush(System.Windows.Media.Color.FromArgb(230, 0x58, 0xa6, 0xff)),
            StrokeThickness = 2,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(border, innerLeft);
        Canvas.SetTop(border, innerTop);
        canvas.Children.Add(border);

        void addZone(int wx, int wy, int wTiles, int hTiles, string arrow, string toolTip)
        {
            double zl = ToCanvasX(ctx, wx);
            double zt = ToCanvasY(ctx, wy);
            double zw = wTiles * tileSize;
            double zh = hTiles * tileSize;
            var zr = new Rectangle
            {
                Width = zw,
                Height = zh,
                Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(130, 0x38, 0x8b, 0xfd)),
                Stroke = new SolidColorBrush(System.Windows.Media.Color.FromArgb(230, 0x58, 0xa6, 0xff)),
                StrokeThickness = 2,
                RadiusX = 3,
                RadiusY = 3,
                IsHitTestVisible = false,
                ToolTip = toolTip
            };
            Canvas.SetLeft(zr, zl);
            Canvas.SetTop(zr, zt);
            canvas.Children.Add(zr);
            var sp = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Vertical, IsHitTestVisible = false };
            sp.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = "+ chunk",
                FontSize = Math.Max(10, Math.Min(14, tileSize * 0.26)),
                FontWeight = System.Windows.FontWeights.SemiBold,
                Foreground = Brushes.White,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            });
            sp.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = arrow,
                FontSize = Math.Max(14, Math.Min(18, tileSize * 0.38)),
                Foreground = Brushes.White,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(0, -2, 0, 0)
            });
            double spW = Math.Min(zw * 0.95, 120);
            Canvas.SetLeft(sp, zl + (zw - spW) * 0.5);
            Canvas.SetTop(sp, zt + (zh - 40) * 0.5);
            canvas.Children.Add(sp);
        }

        FiniteMapExpand.CollectExpandTargetChunks(ctx.Project, ctx.TileMap, _expandTargetChunksScratch);
        double gcx = ox + mw * 0.5;
        double gcy = oy + mh * 0.5;
        foreach (var (tcx, tcy) in _expandTargetChunksScratch)
        {
            int wx = tcx * cs;
            int wy = tcy * cs;
            double tcxCenter = wx + cs * 0.5;
            double tcyCenter = wy + cs * 0.5;
            string arrow;
            if (tcyCenter < gcy - 0.01) arrow = "▲";
            else if (tcyCenter > gcy + 0.01) arrow = "▼";
            else if (tcxCenter < gcx - 0.01) arrow = "◀";
            else arrow = "▶";
            string tip = $"Añade 1 chunk vacío en ({tcx},{tcy}): {cs}×{cs} casillas. Rect. juego pasa a cubrir la unión de chunks.";
            addZone(wx, wy, cs, cs, arrow, tip);
        }
    }

    /// <summary>Solo chunks existentes en la capa que intersectan el rectángulo mundo [minTx,maxTx]×[minTy,maxTy] (evita O(n²) en mapas infinitos vacíos).</summary>
    private static IEnumerable<(int cx, int cy)> EnumerateLayerChunksOverlapping(TileMap map, int layerIndex, int minTx, int maxTx, int minTy, int maxTy)
    {
        int cs = Math.Max(1, map.ChunkSize);
        foreach (var (cx, cy) in map.EnumerateChunkCoords(layerIndex))
        {
            int wx0 = cx * cs;
            int wy0 = cy * cs;
            int wx1 = wx0 + cs - 1;
            int wy1 = wy0 + cs - 1;
            if (wx1 < minTx || wx0 > maxTx || wy1 < minTy || wy0 > maxTy) continue;
            yield return (cx, cy);
        }
    }

    private double ToCanvasX(MapRenderContext ctx, int wx) => (wx - ctx.CanvasMinWx) * ctx.Project.TileSize;
    private double ToCanvasY(MapRenderContext ctx, int wy) => (wy - ctx.CanvasMinWy) * ctx.Project.TileSize;

    /// <summary>
    /// Mapa infinito: el lienzo puede ser más grande que la región dibujada (extensión sticky); las iteraciones solo deben
    /// cubrir tiles visibles más margen, no todo el ancho/alto del canvas.
    /// </summary>
    private void GetClampedTileRangeForDrawing(Canvas canvas, MapRenderContext ctx, double tileSize, out int minTx, out int minTy, out int maxTx, out int maxTy)
    {
        minTx = ctx.CanvasMinWx;
        minTy = ctx.CanvasMinWy;
        maxTx = minTx + (int)Math.Ceiling(canvas.Width / tileSize);
        maxTy = minTy + (int)Math.Ceiling(canvas.Height / tileSize);
        if (!ctx.Project.Infinite)
        {
            int ox = ctx.Project.MapBoundsOriginWorldTileX;
            int oy = ctx.Project.MapBoundsOriginWorldTileY;
            int mw = Math.Max(1, ctx.Project.MapWidth);
            int mh = Math.Max(1, ctx.Project.MapHeight);
            minTx = Math.Max(minTx, ox);
            minTy = Math.Max(minTy, oy);
            maxTx = Math.Min(maxTx, ox + mw - 1);
            maxTy = Math.Min(maxTy, oy + mh - 1);
            if (maxTx < minTx) maxTx = minTx;
            if (maxTy < minTy) maxTy = minTy;
            return;
        }
        double z = ctx.EditorZoom > 0 ? ctx.EditorZoom : 1.0;
        double leftPx = ctx.EditorScrollHorizontalOffset / z;
        double topPx = ctx.EditorScrollVerticalOffset / z;
        double visW = Math.Max(1, ctx.EditorViewportWidth / z);
        double visH = Math.Max(1, ctx.EditorViewportHeight / z);
        const int padTiles = 64;
        int vtMinTx = ctx.CanvasMinWx + (int)Math.Floor(leftPx / tileSize) - padTiles;
        int vtMinTy = ctx.CanvasMinWy + (int)Math.Floor(topPx / tileSize) - padTiles;
        int vtMaxTx = ctx.CanvasMinWx + (int)Math.Ceiling((leftPx + visW) / tileSize) + padTiles;
        int vtMaxTy = ctx.CanvasMinWy + (int)Math.Ceiling((topPx + visH) / tileSize) + padTiles;
        minTx = Math.Max(minTx, vtMinTx);
        minTy = Math.Max(minTy, vtMinTy);
        maxTx = Math.Min(maxTx, vtMaxTx);
        maxTy = Math.Min(maxTy, vtMaxTy);
        if (maxTx < minTx) maxTx = minTx;
        if (maxTy < minTy) maxTy = minTy;
    }

    private void DrawGrid(Canvas canvas, MapRenderContext ctx, double tileSize)
    {
        var gridGeom = new GeometryGroup();
        gridGeom.Children.Add(new LineGeometry(new System.Windows.Point(0, 0), new System.Windows.Point(tileSize, 0)));
        gridGeom.Children.Add(new LineGeometry(new System.Windows.Point(0, 0), new System.Windows.Point(0, tileSize)));
        var gridBrush = new DrawingBrush
        {
            TileMode = TileMode.Tile,
            Viewport = new Rect(0, 0, tileSize, tileSize),
            ViewportUnits = BrushMappingMode.Absolute,
            Opacity = 0.5,
            Drawing = new GeometryDrawing(null, new System.Windows.Media.Pen(new SolidColorBrush(ctx.GridColor), 0.5), gridGeom)
        };
        if (!ctx.Project.Infinite)
        {
            int ox = ctx.Project.MapBoundsOriginWorldTileX;
            int oy = ctx.Project.MapBoundsOriginWorldTileY;
            int mw = Math.Max(1, ctx.Project.MapWidth);
            int mh = Math.Max(1, ctx.Project.MapHeight);
            double w = mw * tileSize;
            double h = mh * tileSize;
            var gridRect = new Rectangle
            {
                Width = w,
                Height = h,
                Fill = gridBrush,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(gridRect, ToCanvasX(ctx, ox));
            Canvas.SetTop(gridRect, ToCanvasY(ctx, oy));
            canvas.Children.Add(gridRect);
            return;
        }
        var gridFull = new Rectangle
        {
            Width = canvas.Width,
            Height = canvas.Height,
            Fill = gridBrush,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(gridFull, 0);
        Canvas.SetTop(gridFull, 0);
        canvas.Children.Add(gridFull);
    }

    private void DrawTiles(Canvas canvas, MapRenderContext ctx, double tileSize)
    {
        var brushByType = new Dictionary<TileType, System.Windows.Media.Brush>
        {
            [TileType.Suelo] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(80, 80, 80)),
            [TileType.Pared] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(120, 80, 60)),
            [TileType.Objeto] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(90, 90, 120)),
            [TileType.Especial] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 60, 100))
        };

        GetClampedTileRangeForDrawing(canvas, ctx, tileSize, out int minTx, out int minTy, out int maxTx, out int maxTy);

        for (int layerIndex = 0; layerIndex < ctx.TileMap.Layers.Count; layerIndex++)
        {
            var descriptor = ctx.TileMap.Layers[layerIndex];
            if (!descriptor.IsVisible) continue;

            double layerOpacity = descriptor.Opacity / 100.0;
            if (ctx.ActiveLayerIndex.HasValue && ctx.ActiveLayerIndex.Value != layerIndex)
                layerOpacity *= 0.5;

            double offsetX = descriptor.OffsetX;
            double offsetY = descriptor.OffsetY;

            foreach (var (cx, cy) in EnumerateLayerChunksOverlapping(ctx.TileMap, layerIndex, minTx, maxTx, minTy, maxTy))
            {
                var chunk = ctx.TileMap.GetChunk(layerIndex, cx, cy);
                if (chunk == null) continue;
                foreach (var (lx, ly, data) in chunk.EnumerateTiles())
                {
                    int wx = cx * ctx.TileMap.ChunkSize + lx;
                    int wy = cy * ctx.TileMap.ChunkSize + ly;
                    if (!ctx.Project.Infinite)
                    {
                        int ox = ctx.Project.MapBoundsOriginWorldTileX;
                        int oy = ctx.Project.MapBoundsOriginWorldTileY;
                        int mw = Math.Max(1, ctx.Project.MapWidth);
                        int mh = Math.Max(1, ctx.Project.MapHeight);
                        if (wx < ox || wx >= ox + mw || wy < oy || wy >= oy + mh) continue;
                    }
                    var isInteractive = data.Interactivo || !string.IsNullOrEmpty(data.ScriptId);
                    if (ctx.MaskColision && !data.Colision) continue;
                    if (ctx.MaskScripts && !isInteractive) continue;
                    var strokeBrush = (ctx.HighlightInteractives && isInteractive)
                        ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xd2, 0x99, 0x22))
                        : Brushes.DimGray;
                    var strokeThick = (ctx.HighlightInteractives && isInteractive) ? 1.5 : 0.5;

                    double posX = ToCanvasX(ctx, wx) + offsetX;
                    double posY = ToCanvasY(ctx, wy) + offsetY;

                    if ((data.PixelOverlay != null && data.PixelOverlay.Width > 0 && data.PixelOverlay.Height > 0) || !string.IsNullOrWhiteSpace(data.SourceImagePath))
                    {
                        var bitmap = PlayTileBitmapCompositor.CreateCompositedTileBitmap(ctx.Project.ProjectDirectory, data, tileSize, ctx.TotalSeconds);
                        if (bitmap != null)
                        {
                            var img = new System.Windows.Controls.Image
                            {
                                Width = tileSize,
                                Height = tileSize,
                                Source = bitmap,
                                Stretch = Stretch.Fill,
                                Opacity = layerOpacity
                            };
                            Canvas.SetLeft(img, posX);
                            Canvas.SetTop(img, posY);
                            canvas.Children.Add(img);
                            var border = new Rectangle
                            {
                                Width = tileSize,
                                Height = tileSize,
                                Fill = Brushes.Transparent,
                                Stroke = strokeBrush,
                                StrokeThickness = strokeThick,
                                IsHitTestVisible = false,
                                Opacity = layerOpacity
                            };
                            Canvas.SetLeft(border, posX);
                            Canvas.SetTop(border, posY);
                            canvas.Children.Add(border);
                        }
                        else
                            AddTileRect(canvas, posX, posY, tileSize, brushByType.GetValueOrDefault(data.TipoTile, brushByType[TileType.Suelo]), strokeBrush, strokeThick, layerOpacity);
                    }
                    else
                        AddTileRect(canvas, posX, posY, tileSize, brushByType.GetValueOrDefault(data.TipoTile, brushByType[TileType.Suelo]), strokeBrush, strokeThick, layerOpacity);

                    if (ctx.ShowTileCoordinates)
                        AddTileCoordLabel(canvas, ctx, wx, wy, tileSize, offsetX, offsetY, layerOpacity);
                }
            }
        }
    }

    private void AddTileRect(Canvas canvas, double posX, double posY, double tileSize, System.Windows.Media.Brush fill, System.Windows.Media.Brush stroke, double strokeThickness, double opacity = 1.0)
    {
        var rect = new Rectangle
        {
            Width = tileSize,
            Height = tileSize,
            Fill = fill,
            Stroke = stroke,
            StrokeThickness = strokeThickness,
            Opacity = opacity
        };
        Canvas.SetLeft(rect, posX);
        Canvas.SetTop(rect, posY);
        canvas.Children.Add(rect);
    }

    private void AddTileCoordLabel(Canvas canvas, MapRenderContext ctx, int wx, int wy, double tileSize, double offsetX = 0, double offsetY = 0, double opacity = 1.0)
    {
        double posX = ToCanvasX(ctx, wx) + offsetX;
        double posY = ToCanvasY(ctx, wy) + offsetY;
        string label;
        if (!ctx.Project.Infinite && ctx.Project != null)
        {
            GameViewportMath.GetTileCoordsRelativeToFiniteMapCenter(ctx.Project, wx, wy, out var rx, out var ry);
            label = $"{rx},{ry}";
        }
        else
            label = $"{wx},{wy}";
        var txt = new TextBlock
        {
            Text = label,
            FontSize = Math.Max(7, Math.Min(10, tileSize * 0.4)),
            Foreground = Brushes.White,
            Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(140, 0, 0, 0)),
            Opacity = opacity
        };
        Canvas.SetLeft(txt, posX + 1);
        Canvas.SetTop(txt, posY + 1);
        canvas.Children.Add(txt);
    }

    private void DrawCollisionOverlay(Canvas canvas, MapRenderContext ctx, double tileSize)
    {
        if (ctx.Project == null) return;
        var projectDir = ctx.Project.ProjectDirectory;
        if (string.IsNullOrEmpty(projectDir)) return;
        var project = ctx.Project;

        GetClampedTileRangeForDrawing(canvas, ctx, tileSize, out int minTx, out int minTy, out int maxTx, out int maxTy);

        var solidBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(180, 0x3f, 0xb9, 0x50));
        var triggerBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(200, 0xd2, 0x99, 0x22));
        var oneWayBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(180, 0x58, 0xa6, 0xff));
        const double strokeThickness = 1.5;

        for (int layerIndex = 0; layerIndex < ctx.TileMap.Layers.Count; layerIndex++)
        {
            var descriptor = ctx.TileMap.Layers[layerIndex];
            if (!descriptor.IsVisible) continue;
            double offsetX = descriptor.OffsetX;
            double offsetY = descriptor.OffsetY;

            foreach (var (cx, cy) in EnumerateLayerChunksOverlapping(ctx.TileMap, layerIndex, minTx, maxTx, minTy, maxTy))
            {
                var chunk = ctx.TileMap.GetChunk(layerIndex, cx, cy);
                if (chunk == null) continue;
                foreach (var (lx, ly, data) in chunk.EnumerateTiles())
                {
                    int wx = cx * ctx.TileMap.ChunkSize + lx;
                    int wy = cy * ctx.TileMap.ChunkSize + ly;
                    if (!project.Infinite)
                    {
                        int ox = project.MapBoundsOriginWorldTileX;
                        int oy = project.MapBoundsOriginWorldTileY;
                        int mw = Math.Max(1, project.MapWidth);
                        int mh = Math.Max(1, project.MapHeight);
                        if (wx < ox || wx >= ox + mw || wy < oy || wy >= oy + mh) continue;
                    }
                    if (string.IsNullOrWhiteSpace(data.SourceImagePath)) continue;
                    var fullPath = System.IO.Path.Combine(projectDir, data.SourceImagePath);
                    var tiledataPath = TileDataFile.GetTileDataPath(fullPath);
                    var dto = TileDataFile.Load(tiledataPath);
                    var shapes = dto?.CollisionShapes;
                    if (shapes == null || shapes.Count == 0) continue;

                    double posX = ToCanvasX(ctx, wx) + offsetX;
                    double posY = ToCanvasY(ctx, wy) + offsetY;
                    int gridSize = dto!.GridSize > 0 ? dto.GridSize : 16;
                    double scale = tileSize / gridSize;

                    foreach (var shape in shapes)
                    {
                        var stroke = shape.Layer == "Trigger" ? triggerBrush : (shape.Layer == "OneWay" ? oneWayBrush : solidBrush);
                        switch (shape.Type)
                        {
                            case "Box":
                                var boxRect = new Rectangle
                                {
                                    Width = shape.Width * scale,
                                    Height = shape.Height * scale,
                                    Fill = Brushes.Transparent,
                                    Stroke = stroke,
                                    StrokeThickness = strokeThickness,
                                    IsHitTestVisible = false
                                };
                                Canvas.SetLeft(boxRect, posX + shape.X * scale);
                                Canvas.SetTop(boxRect, posY + shape.Y * scale);
                                canvas.Children.Add(boxRect);
                                break;
                            case "Circle":
                                var circle = new System.Windows.Shapes.Ellipse
                                {
                                    Width = shape.Radius * 2 * scale,
                                    Height = shape.Radius * 2 * scale,
                                    Fill = Brushes.Transparent,
                                    Stroke = stroke,
                                    StrokeThickness = strokeThickness,
                                    IsHitTestVisible = false
                                };
                                Canvas.SetLeft(circle, posX + (shape.CenterX - shape.Radius) * scale);
                                Canvas.SetTop(circle, posY + (shape.CenterY - shape.Radius) * scale);
                                canvas.Children.Add(circle);
                                break;
                            case "Polygon" when shape.Points != null && shape.Points.Count >= 2:
                                var poly = new System.Windows.Shapes.Polygon
                                {
                                    Fill = Brushes.Transparent,
                                    Stroke = stroke,
                                    StrokeThickness = strokeThickness,
                                    IsHitTestVisible = false,
                                    Points = new PointCollection(shape.Points.Select(p => new System.Windows.Point(posX + p.X * scale, posY + p.Y * scale)))
                                };
                                canvas.Children.Add(poly);
                                break;
                            case "Capsule":
                                var capLine = new System.Windows.Shapes.Line
                                {
                                    X1 = posX + shape.X1 * scale,
                                    Y1 = posY + shape.Y1 * scale,
                                    X2 = posX + shape.X2 * scale,
                                    Y2 = posY + shape.Y2 * scale,
                                    Stroke = stroke,
                                    StrokeThickness = strokeThickness * 2,
                                    IsHitTestVisible = false
                                };
                                canvas.Children.Add(capLine);
                                break;
                        }
                    }
                }
            }
        }
    }

    private void DrawObjects(Canvas canvas, MapRenderContext ctx, double tileSize)
    {
        GetClampedTileRangeForDrawing(canvas, ctx, tileSize, out int tMinTx, out int tMinTy, out int tMaxTx, out int tMaxTy);
        double minWx = tMinTx;
        double maxWx = tMaxTx + 2;
        double minWy = tMinTy;
        double maxWy = tMaxTy + 2;

        foreach (var inst in ctx.ObjectLayer.Instances)
        {
            var def = ctx.ObjectLayer.GetDefinition(inst.DefinitionId);
            var wTiles = def?.Width ?? 1;
            var hTiles = def?.Height ?? 1;
            if (!ctx.Project.Infinite)
            {
                int ox = ctx.Project.MapBoundsOriginWorldTileX;
                int oy = ctx.Project.MapBoundsOriginWorldTileY;
                int mw = Math.Max(1, ctx.Project.MapWidth);
                int mh = Math.Max(1, ctx.Project.MapHeight);
                if (inst.X + wTiles <= ox || inst.X >= ox + mw || inst.Y + hTiles <= oy || inst.Y >= oy + mh) continue;
            }
            if (inst.X + wTiles < minWx || inst.X > maxWx) continue;
            if (inst.Y + hTiles < minWy || inst.Y > maxWy) continue;

            var w = wTiles * tileSize;
            var h = hTiles * tileSize;
            bool selected = ctx.SelectedObjectIds.Contains(inst.InstanceId);
            var strokeBrush = selected ? Brushes.Yellow : new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x58, 0xa6, 0xff));
            var strokeThick = selected ? 2 : 1;

            if (!inst.Visible)
            {
                var markerSize = Math.Max(6, Math.Min(tileSize * 0.4, 12));
                var centerX = ToCanvasX(ctx, (int)inst.X) + w * 0.5 - markerSize * 0.5;
                var centerY = ToCanvasY(ctx, (int)inst.Y) + h * 0.5 - markerSize * 0.5;
                var ellipse = new System.Windows.Shapes.Ellipse
                {
                    Width = markerSize,
                    Height = markerSize,
                    Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(180, 0x58, 0xa6, 0xff)),
                    Stroke = strokeBrush,
                    StrokeThickness = strokeThick
                };
                Canvas.SetLeft(ellipse, centerX);
                Canvas.SetTop(ellipse, centerY);
                canvas.Children.Add(ellipse);
                var hitRect = new Rectangle
                {
                    Width = w,
                    Height = h,
                    Fill = Brushes.Transparent,
                    Stroke = Brushes.Transparent
                };
                Canvas.SetLeft(hitRect, ToCanvasX(ctx, (int)inst.X));
                Canvas.SetTop(hitRect, ToCanvasY(ctx, (int)inst.Y));
                canvas.Children.Add(hitRect);
            }
            else
            {
                var rect = new Rectangle
                {
                    Width = w,
                    Height = h,
                    Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(120, 70, 130, 180)),
                    Stroke = selected ? Brushes.Yellow : Brushes.SteelBlue,
                    StrokeThickness = strokeThick
                };
                Canvas.SetLeft(rect, ToCanvasX(ctx, (int)inst.X));
                Canvas.SetTop(rect, ToCanvasY(ctx, (int)inst.Y));
                canvas.Children.Add(rect);
            }
        }
    }

    private void DrawTriggers(Canvas canvas, MapRenderContext ctx, double tileSize)
    {
        GetClampedTileRangeForDrawing(canvas, ctx, tileSize, out int tMinTx, out int tMinTy, out int tMaxTx, out int tMaxTy);
        double minWx = tMinTx;
        double maxWx = tMaxTx + 2;
        double minWy = tMinTy;
        double maxWy = tMaxTy + 2;

        foreach (var z in ctx.TriggerZones)
        {
            if (!ctx.Project.Infinite)
            {
                int ox = ctx.Project.MapBoundsOriginWorldTileX;
                int oy = ctx.Project.MapBoundsOriginWorldTileY;
                int mw = Math.Max(1, ctx.Project.MapWidth);
                int mh = Math.Max(1, ctx.Project.MapHeight);
                if (z.X + z.Width <= ox || z.X >= ox + mw || z.Y + z.Height <= oy || z.Y >= oy + mh) continue;
            }
            if (z.X + z.Width < minWx || z.X > maxWx) continue;
            if (z.Y + z.Height < minWy || z.Y > maxWy) continue;

            bool selected = ctx.SelectedTriggerId == z.Id;
            var trRect = new Rectangle
            {
                Width = z.Width * tileSize,
                Height = z.Height * tileSize,
                Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(60, 0x58, 0xa6, 0xff)),
                Stroke = selected ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xd2, 0x99, 0x22)) : new SolidColorBrush(System.Windows.Media.Color.FromArgb(180, 0x58, 0xa6, 0xff)),
                StrokeThickness = selected ? 2 : 1,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(trRect, ToCanvasX(ctx, z.X));
            Canvas.SetTop(trRect, ToCanvasY(ctx, z.Y));
            canvas.Children.Add(trRect);
        }
    }

    private void DrawSelectionOverlay(Canvas canvas, MapRenderContext ctx, double tileSize)
    {
        int selMinTx, selMinTy, selMaxTx, selMaxTy;
        bool isDraggingSelection = ctx.TileSelectionDragging && ctx.TileSelectionStart.HasValue && ctx.TileSelectionEnd.HasValue;
        if (isDraggingSelection)
        {
            var start = ctx.TileSelectionStart!.Value;
            var end = ctx.TileSelectionEnd!.Value;
            selMinTx = Math.Min(start.x, end.x);
            selMinTy = Math.Min(start.y, end.y);
            selMaxTx = Math.Max(start.x, end.x);
            selMaxTy = Math.Max(start.y, end.y);
        }
        else if (ctx.SelectedTileMinTx.HasValue && ctx.SelectedTileMinTy.HasValue && ctx.SelectedTileMaxTx.HasValue && ctx.SelectedTileMaxTy.HasValue)
        {
            selMinTx = ctx.SelectedTileMinTx.Value;
            selMinTy = ctx.SelectedTileMinTy.Value;
            selMaxTx = ctx.SelectedTileMaxTx.Value;
            selMaxTy = ctx.SelectedTileMaxTy.Value;
        }
        else
            return;

        if (isDraggingSelection)
        {
            double x0 = ToCanvasX(ctx, selMinTx), y0 = ToCanvasY(ctx, selMinTy);
            int w = (selMaxTx - selMinTx + 1) * (int)tileSize, h = (selMaxTy - selMinTy + 1) * (int)tileSize;
            var selRect = new Rectangle
            {
                Width = w,
                Height = h,
                Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 0x58, 0xa6, 0xff)),
                Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x58, 0xa6, 0xff)),
                StrokeThickness = 2,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(selRect, x0);
            Canvas.SetTop(selRect, y0);
            canvas.Children.Add(selRect);
        }
        else
        {
            var highlightBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x58, 0xa6, 0xff));
            for (int tx = selMinTx; tx <= selMaxTx; tx++)
                for (int ty = selMinTy; ty <= selMaxTy; ty++)
                {
                    if (!ctx.TileMap.TryGetTile(tx, ty, out var cellData) || cellData == null) continue;
                    var border = new Rectangle
                    {
                        Width = tileSize,
                        Height = tileSize,
                        Fill = Brushes.Transparent,
                        Stroke = highlightBrush,
                        StrokeThickness = 2,
                        IsHitTestVisible = false
                    };
                    Canvas.SetLeft(border, ToCanvasX(ctx, tx));
                    Canvas.SetTop(border, ToCanvasY(ctx, ty));
                    canvas.Children.Add(border);
                }
        }
    }

    private void DrawToolOverlays(Canvas canvas, MapRenderContext ctx, double tileSize)
    {
        if (ctx.RectDragging)
        {
            double x0 = ToCanvasX(ctx, Math.Min(ctx.RectStartTx, ctx.RectEndTx)), y0 = ToCanvasY(ctx, Math.Min(ctx.RectStartTy, ctx.RectEndTy));
            int w = (Math.Abs(ctx.RectEndTx - ctx.RectStartTx) + 1) * (int)tileSize, h = (Math.Abs(ctx.RectEndTy - ctx.RectStartTy) + 1) * (int)tileSize;
            var rectPreview = new Rectangle
            {
                Width = w,
                Height = h,
                Stroke = new SolidColorBrush(System.Windows.Media.Color.FromArgb(200, 0x3f, 0xb9, 0x50)),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 0x3f, 0xb9, 0x50))
            };
            Canvas.SetLeft(rectPreview, x0);
            Canvas.SetTop(rectPreview, y0);
            canvas.Children.Add(rectPreview);
        }

        if (ctx.ZoneMinTx != null && ctx.ZoneMaxTx != null && ctx.ZoneMinTy != null && ctx.ZoneMaxTy != null)
        {
            double x0 = ToCanvasX(ctx, ctx.ZoneMinTx.Value), y0 = ToCanvasY(ctx, ctx.ZoneMinTy.Value);
            int w = (ctx.ZoneMaxTx.Value - ctx.ZoneMinTx.Value + 1) * (int)tileSize, h = (ctx.ZoneMaxTy.Value - ctx.ZoneMinTy.Value + 1) * (int)tileSize;
            var zoneRect = new Rectangle
            {
                Width = w,
                Height = h,
                Stroke = new SolidColorBrush(System.Windows.Media.Color.FromArgb(200, 0x58, 0xa6, 0xff)),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 0x58, 0xa6, 0xff))
            };
            Canvas.SetLeft(zoneRect, x0);
            Canvas.SetTop(zoneRect, y0);
            canvas.Children.Add(zoneRect);
        }

        if (ctx.MeasureStart.HasValue && ctx.MeasureEnd.HasValue)
        {
            var m1 = ctx.MeasureStart.Value;
            var m2 = ctx.MeasureEnd.Value;
            var line = new System.Windows.Shapes.Line
            {
                X1 = ToCanvasX(ctx, m1.x) + tileSize / 2.0,
                Y1 = ToCanvasY(ctx, m1.y) + tileSize / 2.0,
                X2 = ToCanvasX(ctx, m2.x) + tileSize / 2.0,
                Y2 = ToCanvasY(ctx, m2.y) + tileSize / 2.0,
                Stroke = Brushes.Lime,
                StrokeThickness = 2
            };
            canvas.Children.Add(line);
        }
    }

}
