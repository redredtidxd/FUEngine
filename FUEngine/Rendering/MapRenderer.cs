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
        if (ctx.ShowVisibleArea)
            DrawVisibleAreaFrame(canvas, ctx, tileSize);
        if (ctx.ShowStreamingGizmos && ctx.Project.ChunkSize > 0)
            DrawStreamingGizmos(canvas, ctx, tileSize);
        if (ctx.Project.ShowChunkBounds && ctx.Project.ChunkSize > 0)
            DrawChunkBounds(canvas, ctx, tileSize);
    }

    private void DrawBackgroundLayers(Canvas canvas, MapRenderContext ctx)
    {
        var projectDir = ctx.Project?.ProjectDirectory;
        if (string.IsNullOrEmpty(projectDir)) return;
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
                var img = new System.Windows.Controls.Image
                {
                    Source = bitmap,
                    Stretch = Stretch.UniformToFill,
                    Width = canvas.Width,
                    Height = canvas.Height,
                    Opacity = descriptor.Opacity / 100.0,
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(img, 0);
                Canvas.SetTop(img, 0);
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
        int minTx = ctx.CanvasMinWx;
        int minTy = ctx.CanvasMinWy;
        int maxTx = minTx + (int)Math.Ceiling(canvas.Width / tileSize);
        int maxTy = minTy + (int)Math.Ceiling(canvas.Height / tileSize);
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
        GameViewportMath.GetEffectiveResolutionPixels(ctx.Project, out int gwPx, out int ghPx);
        int tsProj = Math.Max(1, ctx.Project.TileSize);
        double widthCanvas = gwPx / (double)tsProj * tileSize;
        double heightCanvas = ghPx / (double)tsProj * tileSize;
        double left = (0 - ctx.CanvasMinWx) * tileSize;
        double top = (0 - ctx.CanvasMinWy) * tileSize;
        var rect = new Rectangle
        {
            Width = widthCanvas,
            Height = heightCanvas,
            Fill = Brushes.Transparent,
            Stroke = new SolidColorBrush(System.Windows.Media.Color.FromArgb(200, 0x58, 0xa6, 0xff)),
            StrokeThickness = 2,
            StrokeDashArray = new System.Windows.Media.DoubleCollection(new[] { 4.0, 4.0 }),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(rect, left);
        Canvas.SetTop(rect, top);
        canvas.Children.Add(rect);
    }

    private int _lastMinWx;
    private int _lastMinWy;

    private void ComputeCanvasBounds(MapRenderContext ctx, out int canvasW, out int canvasH)
    {
        var tileSize = ctx.Project.TileSize;
        var tileMap = ctx.TileMap;
        if (ctx.Project.Infinite)
        {
            int minWx = 0, minWy = 0, maxWx = 0, maxWy = 0;
            var coords = tileMap.EnumerateChunkCoords().ToList();
            if (coords.Count > 0)
            {
                int minCx = coords.Min(c => c.cx), maxCx = coords.Max(c => c.cx);
                int minCy = coords.Min(c => c.cy), maxCy = coords.Max(c => c.cy);
                int cs = tileMap.ChunkSize;
                minWx = minCx * cs;
                minWy = minCy * cs;
                maxWx = (maxCx + 1) * cs - 1;
                maxWy = (maxCy + 1) * cs - 1;
            }
            int margin = Math.Max(2, tileMap.ChunkSize * 2);
            if (coords.Count == 0)
            {
                // Mapa infinito vacío: centrar el lienzo en el origen del mundo (0,0) como en el visor de juego.
                int tilesW = Math.Max((int)Math.Ceiling(800.0 / tileSize), Math.Max(16, ctx.Project.MapWidth));
                int tilesH = Math.Max((int)Math.Ceiling(600.0 / tileSize), Math.Max(12, ctx.Project.MapHeight));
                _lastMinWx = -(tilesW / 2);
                _lastMinWy = -(tilesH / 2);
                canvasW = Math.Max(tilesW * tileSize, 800);
                canvasH = Math.Max(tilesH * tileSize, 600);
            }
            else
            {
                _lastMinWx = minWx - margin;
                _lastMinWy = minWy - margin;
                int rangeX = (maxWx - minWx + 1) + margin * 2;
                int rangeY = (maxWy - minWy + 1) + margin * 2;
                canvasW = Math.Max(rangeX * tileSize, 800);
                canvasH = Math.Max(rangeY * tileSize, 600);
            }
        }
        else
        {
            _lastMinWx = 0;
            _lastMinWy = 0;
            canvasW = Math.Max(ctx.Project.MapWidth * tileSize, 800);
            canvasH = Math.Max(ctx.Project.MapHeight * tileSize, 600);
        }
    }

    private double ToCanvasX(MapRenderContext ctx, int wx) => (wx - ctx.CanvasMinWx) * ctx.Project.TileSize;
    private double ToCanvasY(MapRenderContext ctx, int wy) => (wy - ctx.CanvasMinWy) * ctx.Project.TileSize;

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
        var gridRect = new Rectangle
        {
            Width = canvas.Width,
            Height = canvas.Height,
            Fill = gridBrush,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(gridRect, 0);
        Canvas.SetTop(gridRect, 0);
        canvas.Children.Add(gridRect);
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

        int cs = ctx.TileMap.ChunkSize;
        int minTx = ctx.CanvasMinWx;
        int minTy = ctx.CanvasMinWy;
        int maxTx = minTx + (int)Math.Ceiling(canvas.Width / tileSize);
        int maxTy = minTy + (int)Math.Ceiling(canvas.Height / tileSize);
        int startCx = (int)Math.Floor((double)minTx / cs) - 1;
        int endCx = (int)Math.Ceiling((double)maxTx / cs) + 1;
        int startCy = (int)Math.Floor((double)minTy / cs) - 1;
        int endCy = (int)Math.Ceiling((double)maxTy / cs) + 1;

        for (int layerIndex = 0; layerIndex < ctx.TileMap.Layers.Count; layerIndex++)
        {
            var descriptor = ctx.TileMap.Layers[layerIndex];
            if (!descriptor.IsVisible) continue;

            double layerOpacity = descriptor.Opacity / 100.0;
            if (ctx.ActiveLayerIndex.HasValue && ctx.ActiveLayerIndex.Value != layerIndex)
                layerOpacity *= 0.5;

            double offsetX = descriptor.OffsetX;
            double offsetY = descriptor.OffsetY;

            for (int cx = startCx; cx <= endCx; cx++)
            for (int cy = startCy; cy <= endCy; cy++)
            {
                var chunk = ctx.TileMap.GetChunk(layerIndex, cx, cy);
                if (chunk == null) continue;
                foreach (var (lx, ly, data) in chunk.EnumerateTiles())
                {
                    int wx = cx * ctx.TileMap.ChunkSize + lx;
                    int wy = cy * ctx.TileMap.ChunkSize + ly;
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
                        var bitmap = CreateCompositedTileBitmap(ctx.Project.ProjectDirectory, data, tileSize, ctx.TotalSeconds);
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
        var txt = new TextBlock
        {
            Text = $"{wx},{wy}",
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
        var projectDir = ctx.Project?.ProjectDirectory;
        if (string.IsNullOrEmpty(projectDir)) return;

        int cs = ctx.TileMap.ChunkSize;
        int minTx = ctx.CanvasMinWx;
        int minTy = ctx.CanvasMinWy;
        int maxTx = minTx + (int)Math.Ceiling(canvas.Width / tileSize);
        int maxTy = minTy + (int)Math.Ceiling(canvas.Height / tileSize);
        int startCx = (int)Math.Floor((double)minTx / cs) - 1;
        int endCx = (int)Math.Ceiling((double)maxTx / cs) + 1;
        int startCy = (int)Math.Floor((double)minTy / cs) - 1;
        int endCy = (int)Math.Ceiling((double)maxTy / cs) + 1;

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

            for (int cx = startCx; cx <= endCx; cx++)
            for (int cy = startCy; cy <= endCy; cy++)
            {
                var chunk = ctx.TileMap.GetChunk(layerIndex, cx, cy);
                if (chunk == null) continue;
                foreach (var (lx, ly, data) in chunk.EnumerateTiles())
                {
                    if (string.IsNullOrWhiteSpace(data.SourceImagePath)) continue;
                    var fullPath = System.IO.Path.Combine(projectDir, data.SourceImagePath);
                    var tiledataPath = TileDataFile.GetTileDataPath(fullPath);
                    var dto = TileDataFile.Load(tiledataPath);
                    var shapes = dto?.CollisionShapes;
                    if (shapes == null || shapes.Count == 0) continue;

                    int wx = cx * ctx.TileMap.ChunkSize + lx;
                    int wy = cy * ctx.TileMap.ChunkSize + ly;
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
        foreach (var inst in ctx.ObjectLayer.Instances)
        {
            var def = ctx.ObjectLayer.GetDefinition(inst.DefinitionId);
            var w = (def?.Width ?? 1) * tileSize;
            var h = (def?.Height ?? 1) * tileSize;
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
        foreach (var z in ctx.TriggerZones)
        {
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

    private static BitmapSource? CreateCompositedTileBitmap(string? projectDir, TileData data, double tileSizePx, double totalSeconds = 0)
    {
        int w = (int)tileSizePx;
        int h = (int)tileSizePx;
        if (w <= 0 || h <= 0) return null;
        var baseColor = GetBaseColorForTileType(data.TipoTile);
        byte[]? baseRgba = null;
        if (!string.IsNullOrWhiteSpace(data.SourceImagePath) && !string.IsNullOrWhiteSpace(projectDir))
        {
            var fullPath = System.IO.Path.Combine(projectDir, data.SourceImagePath);
            var tiledataPath = TileDataFile.GetTileDataPath(fullPath);
            var dto = TileDataFile.Load(tiledataPath);
            int frameCount = dto?.FrameCount ?? 1;
            int fps = dto?.Fps ?? 8;
            if (frameCount > 1 && fps > 0 && System.IO.File.Exists(fullPath))
            {
                int frameIndex = (int)(totalSeconds * fps) % frameCount;
                if (frameIndex < 0) frameIndex = 0;
                baseRgba = TileImageLoader.LoadFrameToRgba(fullPath, w, h, frameIndex, frameCount);
            }
            if (baseRgba == null)
                baseRgba = TileImageLoader.LoadToRgba(fullPath, w, h);
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

    private static System.Windows.Media.Color GetBaseColorForTileType(TileType tipo)
    {
        return tipo switch
        {
            TileType.Suelo => System.Windows.Media.Color.FromRgb(80, 80, 80),
            TileType.Pared => System.Windows.Media.Color.FromRgb(120, 80, 60),
            TileType.Objeto => System.Windows.Media.Color.FromRgb(90, 90, 120),
            TileType.Especial => System.Windows.Media.Color.FromRgb(100, 60, 100),
            _ => System.Windows.Media.Color.FromRgb(80, 80, 80)
        };
    }
}
