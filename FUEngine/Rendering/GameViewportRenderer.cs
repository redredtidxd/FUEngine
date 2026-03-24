using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Ellipse = System.Windows.Shapes.Ellipse;
using Line = System.Windows.Shapes.Line;
using Rectangle = System.Windows.Shapes.Rectangle;
using FUEngine.Core;
using FUEngine.Runtime;

namespace FUEngine.Rendering;

/// <summary>Dibuja escena 2D: tilemap (capas), sprites, debug. Capas con SortOrder ≥ 100 o <see cref="MapLayerDescriptor.RenderAbovePlayer"/> van delante de los objetos.</summary>
public static class GameViewportRenderer
{
    public const int ForegroundTileSortThreshold = 100;

    public static void DrawWorldAndDebug(
        Canvas canvas,
        IReadOnlyList<GameObject> sceneObjects,
        IReadOnlyList<DebugDrawItem> debugItems,
        ProjectInfo project,
        TextureAssetCache textureCache,
        double vw,
        double vh,
        TileMap? tileMap = null,
        double gameTimeSeconds = 0,
        double? cameraCenterWorldX = null,
        double? cameraCenterWorldY = null)
    {
        int tileSize = Math.Max(1, project.TileSize > 0 ? project.TileSize : 32);
        var objects = sceneObjects;
        double camX = 0, camY = 0;
        if (cameraCenterWorldX.HasValue && cameraCenterWorldY.HasValue)
        {
            camX = cameraCenterWorldX.Value;
            camY = cameraCenterWorldY.Value;
        }
        else
        {
            var player = objects.FirstOrDefault(go =>
                string.Equals(go.Name, "Player", StringComparison.OrdinalIgnoreCase));
            if (player != null)
            {
                camX = player.Transform?.X ?? 0;
                camY = player.Transform?.Y ?? 0;
            }
        }

        double scale, offsetX, offsetY;
        double letterX = 0, letterY = 0, scaledW = 0, scaledH = 0;
        // Sin padding extra: misma región útil que la UI (ComputeCanvasTransform usa todo el viewport centrado).
        const double pad = 0;
        if (tileMap != null)
        {
            GameViewportMath.GetEffectiveResolutionPixels(project, out int effW, out int effH, vw, vh, 1.0);
            double availW = Math.Max(1, vw - pad * 2);
            double availH = Math.Max(1, vh - pad * 2);
            // Misma idea que UI (UIRuntimeBackend.ComputeCanvasTransform): escala uniforme sin saltos enteros,
            // para que el marco del mundo coincida con el overlay UI cuando la resolución lógica es la misma.
            double rawScale = Math.Min(availW / Math.Max(effW, 1), availH / Math.Max(effH, 1));
            // Pixel-perfect: escala entera cuando cabe, para que los tiles alineen al grid de píxeles del monitor.
            if (project.PixelPerfect)
            {
                var intScale = Math.Floor(rawScale);
                scale = intScale >= 1 ? intScale : rawScale;
            }
            else
                scale = rawScale;
            scale = Math.Clamp(scale, 0.2, 64.0);
            scaledW = effW * scale;
            scaledH = effH * scale;
            letterX = (vw - scaledW) / 2.0;
            letterY = (vh - scaledH) / 2.0;
            // Centro del buffer lógico alineado con el centro del área útil (cámara en casillas → centro del viewport).
            offsetX = letterX + scaledW / 2.0 - camX * tileSize * scale;
            offsetY = letterY + scaledH / 2.0 - camY * tileSize * scale;

            // Letterbox: mismo color que el fondo de escena del proyecto (proyecto.json → DefaultFirstSceneBackgroundColor).
            var sceneBg = ParseProjectSceneBackgroundColor(project);
            if (scaledW > 1 && scaledH > 1)
            {
                var gutterBrush = new SolidColorBrush(sceneBg);
                void AddGutter(double gx, double gy, double gW, double gH)
                {
                    if (gW < 0.5 || gH < 0.5) return;
                    var g = new Rectangle
                    {
                        Width = gW,
                        Height = gH,
                        Fill = gutterBrush,
                        IsHitTestVisible = false
                    };
                    Canvas.SetLeft(g, gx);
                    Canvas.SetTop(g, gy);
                    System.Windows.Controls.Panel.SetZIndex(g, -50_000);
                    canvas.Children.Add(g);
                }
                AddGutter(0, 0, Math.Max(0, letterX), vh);
                AddGutter(letterX + scaledW, 0, Math.Max(0, vw - letterX - scaledW), vh);
                AddGutter(letterX, 0, scaledW, Math.Max(0, letterY));
                AddGutter(letterX, letterY + scaledH, scaledW, Math.Max(0, vh - letterY - scaledH));
            }

            // Relleno del rectángulo lógico del juego (coincide con DefaultFirstSceneBackgroundColor del proyecto).
            if (scaledW > 1 && scaledH > 1)
            {
                var playBg = new Rectangle
                {
                    Width = scaledW,
                    Height = scaledH,
                    Fill = new SolidColorBrush(sceneBg),
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(playBg, letterX);
                Canvas.SetTop(playBg, letterY);
                System.Windows.Controls.Panel.SetZIndex(playBg, -49_999);
                canvas.Children.Add(playBg);
            }
        }
        else if (objects.Count > 0)
        {
            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;
            foreach (var go in objects)
            {
                double gx = go.Transform?.X ?? 0;
                double gy = go.Transform?.Y ?? 0;
                if (gx < minX) minX = gx;
                if (gx > maxX) maxX = gx;
                if (gy < minY) minY = gy;
                if (gy > maxY) maxY = gy;
            }
            double worldW = (maxX - minX + 1) * tileSize;
            double worldH = (maxY - minY + 1) * tileSize;
            scale = Math.Min(Math.Min((vw - pad * 2) / Math.Max(worldW, 1), (vh - pad * 2) / Math.Max(worldH, 1)), 4.0);
            offsetX = vw / 2.0 - (minX + maxX) / 2.0 * tileSize * scale;
            offsetY = vh / 2.0 - (minY + maxY) / 2.0 * tileSize * scale;
        }
        else
        {
            scale = 1.0;
            offsetX = vw / 2.0;
            offsetY = vh / 2.0;
        }

        double objSizeDefault = Math.Max(8, tileSize * scale);
        var objFill = new SolidColorBrush(Color.FromArgb(200, 0x58, 0xa6, 0xff));
        var objBorder = new SolidColorBrush(Colors.White);
        var textBrush = new SolidColorBrush(Colors.White);

        int z = 0;
        var projectDir = project.ProjectDirectory;
        bool sceneHasLights = objects.Any(go => go.GetComponent<LightComponent>() != null);
        var lightingScene = sceneHasLights ? objects : null;
        if (tileMap != null)
            DrawTilemapLayers(canvas, tileMap, projectDir, vw, vh, tileSize, scale, offsetX, offsetY, gameTimeSeconds, foreground: false, ref z, lightingScene);

        var sortedObjects = objects
            .OrderBy(go => go.RenderOrder)
            .ThenBy(go => go.GetComponent<SpriteComponent>()?.SortOffset ?? 0)
            .ThenBy(go => go.Transform?.Y ?? 0)
            .ToList();

        foreach (var go in sortedObjects)
        {
            double px = (go.Transform?.X ?? 0) * tileSize * scale + offsetX;
            double py = (go.Transform?.Y ?? 0) * tileSize * scale + offsetY;

            var sprite = go.GetComponent<SpriteComponent>();
            if (sprite != null && !string.IsNullOrEmpty(sprite.TexturePath))
            {
                var bmp = textureCache.GetOrLoad(sprite.TexturePath);
                if (bmp != null)
                {
                    ImageSource src = bmp;
                    if (sprite.FrameRegions.Count > 0 &&
                        sprite.CurrentFrameIndex >= 0 &&
                        sprite.CurrentFrameIndex < sprite.FrameRegions.Count)
                    {
                        var fr = sprite.FrameRegions[sprite.CurrentFrameIndex];
                        if (fr.Width > 0 && fr.Height > 0)
                        {
                            try
                            {
                                var cropped = new CroppedBitmap(bmp, new Int32Rect(fr.X, fr.Y, fr.Width, fr.Height));
                                cropped.Freeze();
                                src = cropped;
                            }
                            catch { /* usar textura completa */ }
                        }
                    }

                    float sx = go.Transform?.ScaleX ?? 1f;
                    float sy = go.Transform?.ScaleY ?? 1f;
                    double drawW = Math.Max(4, sprite.DisplayWidthTiles * Math.Abs(sx) * tileSize * scale);
                    double drawH = Math.Max(4, sprite.DisplayHeightTiles * Math.Abs(sy) * tileSize * scale);

                    if (px < -drawW || px > vw + drawW || py < -drawH || py > vh + drawH) continue;

                    double imgOpacity = 1.0;
                    double tr = 1, tg = 1, tb = 1;
                    if (lightingScene != null)
                    {
                        var (lr, lg, lb) = SceneLighting.SampleRgbTint(lightingScene, go.Transform?.X ?? 0f, go.Transform?.Y ?? 0f);
                        tr = lr; tg = lg; tb = lb;
                    }
                    tr *= sprite.ColorTintR; tg *= sprite.ColorTintG; tb *= sprite.ColorTintB;
                    if (src is BitmapSource bsTint)
                    {
                        var tinted = SpriteBitmapTint.MultiplyRgb(bsTint, tr, tg, tb);
                        if (tinted != null) src = tinted;
                    }

                    var img = new System.Windows.Controls.Image
                    {
                        Source = src,
                        Stretch = Stretch.Fill,
                        Width = drawW,
                        Height = drawH,
                        ToolTip = go.Name ?? "",
                        Opacity = imgOpacity,
                    };
                    img.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                    var tf = new TransformGroup();
                    double scaleX = (sx < 0 ? -1 : 1) * (sprite.FlipX ? -1 : 1);
                    double scaleY = (sy < 0 ? -1 : 1) * (sprite.FlipY ? -1 : 1);
                    tf.Children.Add(new ScaleTransform(scaleX, scaleY));
                    tf.Children.Add(new RotateTransform(go.Transform?.RotationDegrees ?? 0));
                    img.RenderTransform = tf;
                    Canvas.SetLeft(img, px - drawW / 2);
                    Canvas.SetTop(img, py - drawH / 2);
                    System.Windows.Controls.Panel.SetZIndex(img, 100_000 + go.RenderOrder);
                    canvas.Children.Add(img);

                    if (drawW >= 14 && !string.IsNullOrEmpty(go.Name))
                    {
                        var lbl = new TextBlock
                        {
                            Text = go.Name,
                            FontSize = Math.Clamp(drawW / 3.5, 8.0, 10.0),
                            Foreground = textBrush,
                            IsHitTestVisible = false
                        };
                        Canvas.SetLeft(lbl, px - drawW / 2 + 2);
                        Canvas.SetTop(lbl, py - drawH / 2 + 2);
                        System.Windows.Controls.Panel.SetZIndex(lbl, 100_001 + go.RenderOrder);
                        canvas.Children.Add(lbl);
                    }

                    continue;
                }
            }

            double fw = objSizeDefault;
            double fh = objSizeDefault;
            var col = go.GetComponent<ColliderComponent>();
            if (col != null)
            {
                float csx = Math.Abs(go.Transform?.ScaleX ?? 1f);
                float csy = Math.Abs(go.Transform?.ScaleY ?? 1f);
                fw = Math.Max(8, col.TileHalfWidth * 2f * csx * tileSize * scale);
                fh = Math.Max(8, col.TileHalfHeight * 2f * csy * tileSize * scale);
            }

            if (px < -fw || px > vw + fw || py < -fh || py > vh + fh) continue;

            double lightMul2 = lightingScene != null
                ? SceneLighting.SampleBrightness(lightingScene, go.Transform?.X ?? 0f, go.Transform?.Y ?? 0f)
                : 1.0;
            // Sin textura: el relleno sigue el tinte del sprite (p. ej. cuadrado negro con setSpriteTint).
            SolidColorBrush fillBrush;
            if (sprite != null)
            {
                double tr = sprite.ColorTintR * lightMul2;
                double tg = sprite.ColorTintG * lightMul2;
                double tb = sprite.ColorTintB * lightMul2;
                fillBrush = new SolidColorBrush(Color.FromRgb(
                    (byte)Math.Clamp(255 * tr, 0, 255),
                    (byte)Math.Clamp(255 * tg, 0, 255),
                    (byte)Math.Clamp(255 * tb, 0, 255)));
            }
            else
                fillBrush = objFill;

            var rect = new Rectangle
            {
                Width = fw,
                Height = fh,
                Fill = fillBrush,
                Stroke = objBorder,
                StrokeThickness = 1,
                ToolTip = go.Name ?? "",
                Opacity = sprite != null ? 1.0 : lightMul2,
            };
            rect.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
            rect.RenderTransform = new RotateTransform(go.Transform?.RotationDegrees ?? 0);
            Canvas.SetLeft(rect, px - fw / 2);
            Canvas.SetTop(rect, py - fh / 2);
            System.Windows.Controls.Panel.SetZIndex(rect, 100_000 + go.RenderOrder);
            canvas.Children.Add(rect);

            if (fw >= 14 && !string.IsNullOrEmpty(go.Name))
            {
                var lbl = new TextBlock
                {
                    Text = go.Name,
                    FontSize = Math.Clamp(fw / 3.5, 8.0, 10.0),
                    Foreground = textBrush,
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(lbl, px - fw / 2 + 2);
                Canvas.SetTop(lbl, py - fh / 2 + 2);
                System.Windows.Controls.Panel.SetZIndex(lbl, 100_001 + go.RenderOrder);
                canvas.Children.Add(lbl);
            }
        }

        if (tileMap != null)
            DrawTilemapLayers(canvas, tileMap, projectDir, vw, vh, tileSize, scale, offsetX, offsetY, gameTimeSeconds, foreground: true, ref z, lightingScene);

        foreach (var d in debugItems)
        {
            var stroke = new SolidColorBrush(Color.FromArgb(d.A, d.R, d.G, d.B));
            if (d.Kind == DebugDrawKind.Line)
            {
                var px1 = d.X1 * tileSize * scale + offsetX;
                var py1 = d.Y1 * tileSize * scale + offsetY;
                var px2 = d.X2 * tileSize * scale + offsetX;
                var py2 = d.Y2 * tileSize * scale + offsetY;
                var line = new Line
                {
                    X1 = px1,
                    Y1 = py1,
                    X2 = px2,
                    Y2 = py2,
                    Stroke = stroke,
                    StrokeThickness = 2
                };
                System.Windows.Controls.Panel.SetZIndex(line, 500_000);
                canvas.Children.Add(line);
            }
            else
            {
                var px = d.X1 * tileSize * scale + offsetX;
                var py = d.Y1 * tileSize * scale + offsetY;
                var dia = Math.Max(4, d.X2 * 2 * tileSize * scale);
                var ell = new Ellipse
                {
                    Width = dia,
                    Height = dia,
                    Stroke = stroke,
                    StrokeThickness = 1.5,
                    Fill = new SolidColorBrush(Color.FromArgb((byte)Math.Min(255, d.A / 2), d.R, d.G, d.B))
                };
                Canvas.SetLeft(ell, px - dia / 2);
                Canvas.SetTop(ell, py - dia / 2);
                System.Windows.Controls.Panel.SetZIndex(ell, 500_000);
                canvas.Children.Add(ell);
            }
        }
    }

    /// <summary>
    /// Dibuja solo chunks presentes en el <see cref="TileMap"/> (sin cola async). En Play, el streaming debe
    /// precargar desde caché al inicio del tick para reducir huecos al mover la cámara rápido.
    /// </summary>
    private static void DrawTilemapLayers(
        Canvas canvas,
        TileMap map,
        string? projectDir,
        double vw,
        double vh,
        int tileSize,
        double scale,
        double offsetX,
        double offsetY,
        double gameTimeSeconds,
        bool foreground,
        ref int z,
        IReadOnlyList<GameObject>? lightingScene)
    {
        int cs = map.ChunkSize > 0 ? map.ChunkSize : Chunk.DefaultSize;
        double pxPerTile = tileSize * scale;
        double marginPx = pxPerTile * 2;
        int minTx = (int)Math.Floor((-marginPx - offsetX) / pxPerTile);
        int maxTx = (int)Math.Floor((vw + marginPx - offsetX) / pxPerTile);
        int minTy = (int)Math.Floor((-marginPx - offsetY) / pxPerTile);
        int maxTy = (int)Math.Floor((vh + marginPx - offsetY) / pxPerTile);

        int startCx = WorldToChunkCoord(minTx, cs);
        int endCx = WorldToChunkCoord(maxTx, cs);
        int startCy = WorldToChunkCoord(minTy, cs);
        int endCy = WorldToChunkCoord(maxTy, cs);

        var brushByType = new Dictionary<TileType, System.Windows.Media.Brush>
        {
            [TileType.Suelo] = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
            [TileType.Pared] = new SolidColorBrush(Color.FromRgb(120, 80, 60)),
            [TileType.Objeto] = new SolidColorBrush(Color.FromRgb(90, 90, 120)),
            [TileType.Especial] = new SolidColorBrush(Color.FromRgb(100, 60, 100))
        };

        var layerIndices = Enumerable.Range(0, map.Layers.Count)
            .OrderBy(i => map.Layers[i].SortOrder)
            .ToList();

        foreach (var layerIndex in layerIndices)
        {
            var descriptor = map.Layers[layerIndex];
            bool isForeground = descriptor.SortOrder >= ForegroundTileSortThreshold || descriptor.RenderAbovePlayer;
            if (isForeground != foreground) continue;
            if (!descriptor.IsVisible) continue;

            double layerOpacity = descriptor.Opacity / 100.0;
            double layerOffX = descriptor.OffsetX;
            double layerOffY = descriptor.OffsetY;

            for (int cx = startCx; cx <= endCx; cx++)
            for (int cy = startCy; cy <= endCy; cy++)
            {
                var chunk = map.GetChunk(layerIndex, cx, cy);
                if (chunk == null) continue;
                foreach (var (lx, ly, data) in chunk.EnumerateTiles())
                {
                    int wx = cx * cs + lx;
                    int wy = cy * cs + ly;
                    if (wx < minTx || wx > maxTx || wy < minTy || wy > maxTy) continue;

                    double posX = wx * pxPerTile + offsetX + layerOffX;
                    double posY = wy * pxPerTile + offsetY + layerOffY;

                    double tileLight = lightingScene != null
                        ? SceneLighting.SampleBrightness(lightingScene, wx + 0.5f, wy + 0.5f)
                        : 1.0;
                    double tileOpacity = layerOpacity * tileLight;
                    if ((data.PixelOverlay != null && data.PixelOverlay.Width > 0 && data.PixelOverlay.Height > 0) ||
                        !string.IsNullOrWhiteSpace(data.SourceImagePath))
                    {
                        var bitmap = PlayTileBitmapCompositor.CreateCompositedTileBitmap(projectDir, data, tileSize * scale, gameTimeSeconds);
                        if (bitmap != null)
                        {
                            var img = new System.Windows.Controls.Image
                            {
                                Width = pxPerTile,
                                Height = pxPerTile,
                                Source = bitmap,
                                Stretch = Stretch.Fill,
                                Opacity = tileOpacity
                            };
                            Canvas.SetLeft(img, posX);
                            Canvas.SetTop(img, posY);
                            System.Windows.Controls.Panel.SetZIndex(img, z++);
                            canvas.Children.Add(img);
                        }
                        else
                            AddFallbackTileRect(canvas, posX, posY, pxPerTile, brushByType, data.TipoTile, tileOpacity, z++);
                    }
                    else
                        AddFallbackTileRect(canvas, posX, posY, pxPerTile, brushByType, data.TipoTile, tileOpacity, z++);
                }
            }
        }
    }

    private static void AddFallbackTileRect(Canvas canvas, double posX, double posY, double pxPerTile,
        Dictionary<TileType, System.Windows.Media.Brush> brushByType, TileType tipo, double layerOpacity, int zIdx)
    {
        var rect = new Rectangle
        {
            Width = pxPerTile,
            Height = pxPerTile,
            Fill = brushByType.GetValueOrDefault(tipo, brushByType[TileType.Suelo]),
            Stroke = Brushes.DimGray,
            StrokeThickness = 0.5,
            Opacity = layerOpacity
        };
        Canvas.SetLeft(rect, posX);
        Canvas.SetTop(rect, posY);
        System.Windows.Controls.Panel.SetZIndex(rect, zIdx);
        canvas.Children.Add(rect);
    }

    private static int WorldToChunkCoord(int world, int chunkSize)
    {
        if (chunkSize <= 0) chunkSize = Chunk.DefaultSize;
        return world < 0 ? (world + 1) / chunkSize - 1 : world / chunkSize;
    }

    /// <summary>Color de fondo del área útil / letterbox en el tab Juego (hex #RRGGBB en proyecto).</summary>
    private static Color ParseProjectSceneBackgroundColor(ProjectInfo project)
    {
        var s = project.DefaultFirstSceneBackgroundColor?.Trim();
        if (string.IsNullOrEmpty(s)) return Colors.White;
        if (s.Length >= 7 && s[0] == '#' &&
            int.TryParse(s.Substring(1, 6), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb))
        {
            return Color.FromRgb((byte)((rgb >> 16) & 0xff), (byte)((rgb >> 8) & 0xff), (byte)(rgb & 0xff));
        }
        return Colors.White;
    }
}
