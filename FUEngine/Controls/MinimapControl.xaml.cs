using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using FUEngine.Core;

namespace FUEngine;

public partial class MinimapControl : System.Windows.Controls.UserControl
{
    private TileMap? _map;
    private int _chunkSize = 16;
    private const int MinimapSize = 128;

    public MinimapControl()
    {
        InitializeComponent();
    }

    public void SetMap(TileMap? map, int chunkSize)
    {
        _map = map;
        _chunkSize = Math.Max(1, chunkSize);
        DrawMinimap();
    }

    public void DrawMinimap()
    {
        MinimapCanvas.Children.Clear();
        if (_map == null) return;
        var coords = _map.EnumerateChunkCoords().ToList();
        if (coords.Count == 0) return;
        int minCx = coords.Min(c => c.cx), maxCx = coords.Max(c => c.cx);
        int minCy = coords.Min(c => c.cy), maxCy = coords.Max(c => c.cy);
        int rangeX = maxCx - minCx + 1;
        int rangeY = maxCy - minCy + 1;
        if (rangeX <= 0) rangeX = 1;
        if (rangeY <= 0) rangeY = 1;
        double scaleX = (double)MinimapSize / (rangeX * _chunkSize);
        double scaleY = (double)MinimapSize / (rangeY * _chunkSize);
        double scale = Math.Min(scaleX, scaleY);
        foreach (var (cx, cy) in coords)
        {
            var chunk = _map.GetChunk(cx, cy);
            if (chunk == null) continue;
            bool hasAny = false;
            foreach (var (lx, ly, data) in chunk.EnumerateTiles())
            {
                hasAny = true;
                break;
            }
            double x = (cx - minCx) * _chunkSize * scale;
            double y = (cy - minCy) * _chunkSize * scale;
            double w = _chunkSize * scale;
            double h = _chunkSize * scale;
            if (w < 1) w = 1;
            if (h < 1) h = 1;
            var rect = new System.Windows.Shapes.Rectangle
            {
                Width = w,
                Height = h,
                Fill = hasAny ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x58, 0xa6, 0xff)) : new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x21, 0x26, 0x2d)),
                Stroke = System.Windows.Media.Brushes.Transparent
            };
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            MinimapCanvas.Children.Add(rect);
        }
    }
}
