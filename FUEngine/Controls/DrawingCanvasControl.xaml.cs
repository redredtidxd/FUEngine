using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FUEngine;

/// <summary>
/// WPF drawing control using WriteableBitmap. Supports Tile mode (pixel grid, pencil/eraser/fill)
/// and Paint mode (free resolution, interpolated brush with opacity).
/// </summary>
public partial class DrawingCanvasControl : System.Windows.Controls.UserControl
{
    public static readonly int DefaultTileSize = 16;

    private WriteableBitmap? _bitmap;
    private byte[]? _bgra;
    private readonly List<byte[]> _layers = new();
    private readonly List<bool> _layerVisible = new();
    private int _currentLayerIndex;
    private int _width;
    private int _height;
    private bool _isDrawing;
    private System.Windows.Point _lastPoint;
    private readonly List<byte[]> _drawingUndoStack = new();
    private const int MaxDrawingUndo = 40;
    private DrawingCanvasTool _tool = DrawingCanvasTool.Pencil;
    private System.Windows.Media.Color _primaryColor = System.Windows.Media.Color.FromRgb(255, 255, 255);
    private System.Windows.Media.Color _eraseColor = System.Windows.Media.Color.FromArgb(0, 0, 0, 0);

    public DrawingCanvasControl()
    {
        InitializeComponent();
    }

    public enum DrawingMode { Tile, Paint }
    public enum DrawingCanvasTool { Pencil, Eraser, Fill }

    public DrawingMode Mode { get; set; } = DrawingMode.Tile;
    public int GridSize { get; set; } = DefaultTileSize;
    public DrawingCanvasTool Tool { get => _tool; set => _tool = value; }
    public System.Windows.Media.Color PrimaryColor { get => _primaryColor; set => _primaryColor = value; }
    public int BrushSize { get; set; } = 4;
    public double BrushOpacity { get; set; } = 1.0;

    public int CanvasWidth => _width;
    public int CanvasHeight => _height;

    public int LayerCount => _layers.Count;
    public int CurrentLayerIndex => _currentLayerIndex;
    public event EventHandler? LayersChanged;

    public bool IsDirty { get; private set; }
    public event EventHandler? IsDirtyChanged;

    public void CreateCanvas(int width, int height)
    {
        if (width <= 0 || height <= 0) return;
        _width = width;
        _height = height;
        int len = width * height * 4;
        _layers.Clear();
        _layerVisible.Clear();
        var layer0 = new byte[len];
        _layers.Add(layer0);
        _layerVisible.Add(true);
        _currentLayerIndex = 0;
        _bgra = new byte[len];
        _bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        _drawingUndoStack.Clear();
        CompositeToDisplay();
        if (CanvasImage != null) CanvasImage.Source = _bitmap;
        SetDirty(false);
        LayersChanged?.Invoke(this, EventArgs.Empty);
    }

    public void LoadFromBitmap(BitmapSource source)
    {
        if (source == null) return;
        int w = source.PixelWidth;
        int h = source.PixelHeight;
        if (w <= 0 || h <= 0) return;
        var converted = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        _width = w;
        _height = h;
        int stride = w * 4;
        int len = w * h * 4;
        _layers.Clear();
        _layerVisible.Clear();
        var layer0 = new byte[len];
        converted.CopyPixels(layer0, stride, 0);
        _layers.Add(layer0);
        _layerVisible.Add(true);
        _currentLayerIndex = 0;
        _bgra = new byte[len];
        _bitmap = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgra32, null);
        _drawingUndoStack.Clear();
        CompositeToDisplay();
        if (CanvasImage != null) CanvasImage.Source = _bitmap;
        SetDirty(false);
        LayersChanged?.Invoke(this, EventArgs.Empty);
    }

    public WriteableBitmap? GetBitmap()
    {
        CompositeToDisplay();
        return _bitmap;
    }

    public void AddLayer()
    {
        if (_width <= 0 || _height <= 0) return;
        int len = _width * _height * 4;
        var layer = new byte[len];
        _layers.Add(layer);
        _layerVisible.Add(true);
        _currentLayerIndex = _layers.Count - 1;
        CompositeToDisplay();
        LayersChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveLayer(int index)
    {
        if (index < 0 || index >= _layers.Count) return;
        _layers.RemoveAt(index);
        _layerVisible.RemoveAt(index);
        if (_currentLayerIndex >= _layers.Count) _currentLayerIndex = Math.Max(0, _layers.Count - 1);
        if (_layers.Count == 0)
        {
            _bgra = null;
            return;
        }
        CompositeToDisplay();
        LayersChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetCurrentLayer(int index)
    {
        if (index >= 0 && index < _layers.Count)
        {
            _currentLayerIndex = index;
            LayersChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void SetLayerVisible(int index, bool visible)
    {
        if (index >= 0 && index < _layerVisible.Count)
        {
            _layerVisible[index] = visible;
            CompositeToDisplay();
            LayersChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool IsLayerVisible(int index) => index >= 0 && index < _layerVisible.Count && _layerVisible[index];

    public byte[]? GetPixels()
    {
        CompositeToDisplay();
        return _bgra;
    }

    public (int width, int height) GetSize() => (_width, _height);

    public void SetDirty(bool dirty)
    {
        if (IsDirty == dirty) return;
        IsDirty = dirty;
        IsDirtyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void FlushToBitmap()
    {
        if (_bitmap == null || _bgra == null) return;
        _bitmap.WritePixels(new Int32Rect(0, 0, _width, _height), _bgra, _width * 4, 0);
    }

    private void CompositeToDisplay()
    {
        if (_bgra == null || _width <= 0 || _height <= 0) return;
        int len = _width * _height * 4;
        Array.Clear(_bgra, 0, len);
        for (int L = 0; L < _layers.Count; L++)
        {
            if (!_layerVisible[L]) continue;
            var layer = _layers[L];
            for (int i = 0; i < len; i += 4)
            {
                byte sa = layer[i + 3];
                if (sa == 0) continue;
                byte sb = layer[i], sg = layer[i + 1], sr = layer[i + 2];
                byte da = _bgra[i + 3];
                byte inv = (byte)(255 - sa);
                _bgra[i] = (byte)((sa * sb + inv * _bgra[i]) / 255);
                _bgra[i + 1] = (byte)((sa * sg + inv * _bgra[i + 1]) / 255);
                _bgra[i + 2] = (byte)((sa * sr + inv * _bgra[i + 2]) / 255);
                _bgra[i + 3] = (byte)Math.Min(255, da + (sa * (255 - da) / 255));
            }
        }
        FlushToBitmap();
    }

    private byte[]? GetCurrentLayerBuffer() =>
        _layers.Count > 0 && _currentLayerIndex >= 0 && _currentLayerIndex < _layers.Count ? _layers[_currentLayerIndex] : null;

    private void PushDrawingUndoSnapshot()
    {
        var buf = GetCurrentLayerBuffer();
        if (buf == null) return;
        var copy = new byte[buf.Length];
        Buffer.BlockCopy(buf, 0, copy, 0, buf.Length);
        _drawingUndoStack.Add(copy);
        while (_drawingUndoStack.Count > MaxDrawingUndo)
            _drawingUndoStack.RemoveAt(0);
    }

    /// <summary>Deshace el último trazo o relleno en la capa activa (Ctrl+Z en el lienzo).</summary>
    public bool TryUndoDrawing()
    {
        if (_drawingUndoStack.Count == 0) return false;
        var prev = _drawingUndoStack[_drawingUndoStack.Count - 1];
        _drawingUndoStack.RemoveAt(_drawingUndoStack.Count - 1);
        var buf = GetCurrentLayerBuffer();
        if (buf == null || buf.Length != prev.Length) return false;
        Buffer.BlockCopy(prev, 0, buf, 0, buf.Length);
        CompositeToDisplay();
        SetDirty(true);
        return true;
    }

    private System.Windows.Point ToImageCoords(System.Windows.Point pt)
    {
        if (CanvasImage == null) return pt;
        var rel = pt;
        try
        {
            var transform = CanvasImage.TransformToAncestor(CanvasHost);
            if (transform != null)
            {
                var origin = transform.Transform(new System.Windows.Point(0, 0));
                rel = new System.Windows.Point(pt.X - origin.X, pt.Y - origin.Y);
            }
        }
        catch { /* ignore */ }
        double scale = 1.0;
        if (CanvasImage.Source is BitmapSource bs)
        {
            scale = bs.Width > 0 ? CanvasImage.ActualWidth / bs.Width : 1;
            if (scale <= 0) scale = 1;
        }
        double x = scale > 0 ? rel.X / scale : rel.X;
        double y = scale > 0 ? rel.Y / scale : rel.Y;
        return new System.Windows.Point(x, y);
    }

    private (int px, int py) SnapToPixel(System.Windows.Point pt)
    {
        if (Mode == DrawingMode.Tile)
        {
            int g = Math.Max(1, GridSize);
            int px = (int)Math.Floor(pt.X / g) * g;
            int py = (int)Math.Floor(pt.Y / g) * g;
            if (px < 0) px = 0;
            if (py < 0) py = 0;
            if (px >= _width) px = _width - 1;
            if (py >= _height) py = _height - 1;
            return (px, py);
        }
        int ix = (int)Math.Round(pt.X);
        int iy = (int)Math.Round(pt.Y);
        ix = Math.Clamp(ix, 0, _width - 1);
        iy = Math.Clamp(iy, 0, _height - 1);
        return (ix, iy);
    }

    private void SetPixel(int x, int y, byte b, byte g, byte r, byte a)
    {
        var buf = GetCurrentLayerBuffer();
        if (buf == null || x < 0 || x >= _width || y < 0 || y >= _height) return;
        int i = (y * _width + x) * 4;
        if (a >= 255)
        {
            buf[i] = b;
            buf[i + 1] = g;
            buf[i + 2] = r;
            buf[i + 3] = a;
        }
        else
        {
            byte inv = (byte)(255 - a);
            buf[i] = (byte)((a * b + inv * buf[i]) / 255);
            buf[i + 1] = (byte)((a * g + inv * buf[i + 1]) / 255);
            buf[i + 2] = (byte)((a * r + inv * buf[i + 2]) / 255);
            buf[i + 3] = (byte)Math.Min(255, buf[i + 3] + a);
        }
    }

    private void DrawPixelOrBrush(int x, int y, bool isEraser)
    {
        if (Mode == DrawingMode.Tile || Tool == DrawingCanvasTool.Pencil || Tool == DrawingCanvasTool.Eraser)
        {
            var c = isEraser ? _eraseColor : _primaryColor;
            byte alphaPx = c.A;
            if (!isEraser && Mode == DrawingMode.Paint && Tool == DrawingCanvasTool.Pencil)
                alphaPx = 255;
            SetPixel(x, y, c.B, c.G, c.R, alphaPx);
            return;
        }
        int radius = Math.Max(1, BrushSize);
        byte a = (byte)(Math.Clamp(BrushOpacity, 0, 1) * 255);
        var color = isEraser ? _eraseColor : _primaryColor;
        double r = radius + 0.5;
        for (int dy = -radius; dy <= radius; dy++)
        for (int dx = -radius; dx <= radius; dx++)
        {
            int nx = x + dx;
            int ny = y + dy;
            if (nx < 0 || nx >= _width || ny < 0 || ny >= _height) continue;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist > r) continue;
            double t = dist / r;
            double falloff = 1.0 - t * t * 0.6;
            byte aa = (byte)(a * Math.Clamp(falloff, 0, 1));
            SetPixel(nx, ny, color.B, color.G, color.R, aa);
        }
    }

    private void FloodFill(int startX, int startY, byte newB, byte newG, byte newR, byte newA)
    {
        var buf = GetCurrentLayerBuffer();
        if (buf == null) return;
        int i0 = (startY * _width + startX) * 4;
        byte oldB = buf[i0];
        byte oldG = buf[i0 + 1];
        byte oldR = buf[i0 + 2];
        byte oldA = buf[i0 + 3];
        if (oldB == newB && oldG == newG && oldR == newR && oldA == newA) return;

        var stack = new Stack<(int x, int y)>();
        var visited = new HashSet<(int, int)>();
        stack.Push((startX, startY));
        int filled = 0;
        const int maxFill = 4 * 1024 * 1024;
        while (stack.Count > 0 && filled < maxFill)
        {
            var (cx, cy) = stack.Pop();
            if (cx < 0 || cx >= _width || cy < 0 || cy >= _height) continue;
            if (visited.Contains((cx, cy))) continue;
            int idx = (cy * _width + cx) * 4;
            if (buf[idx] != oldB || buf[idx + 1] != oldG || buf[idx + 2] != oldR || buf[idx + 3] != oldA)
                continue;
            visited.Add((cx, cy));
            buf[idx] = newB;
            buf[idx + 1] = newG;
            buf[idx + 2] = newR;
            buf[idx + 3] = newA;
            filled++;
            stack.Push((cx + 1, cy));
            stack.Push((cx - 1, cy));
            stack.Push((cx, cy + 1));
            stack.Push((cx, cy - 1));
        }
    }

    private void CanvasHost_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (GetCurrentLayerBuffer() == null) return;
        CanvasHost.Focus();
        var pt = ToImageCoords(e.GetPosition(CanvasHost));
        var (px, py) = SnapToPixel(pt);
        PushDrawingUndoSnapshot();
        _isDrawing = true;
        _lastPoint = new System.Windows.Point(px, py);
        CanvasHost.CaptureMouse();

        if (Tool == DrawingCanvasTool.Fill)
        {
            var c = _primaryColor;
            FloodFill(px, py, c.B, c.G, c.R, c.A);
            CompositeToDisplay();
            SetDirty(true);
        }
        else
        {
            bool eraser = Tool == DrawingCanvasTool.Eraser;
            DrawPixelOrBrush(px, py, eraser);
            CompositeToDisplay();
            SetDirty(true);
        }
    }

    private void CanvasHost_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || !_isDrawing || GetCurrentLayerBuffer() == null) return;
        var pt = ToImageCoords(e.GetPosition(CanvasHost));
        var (px, py) = SnapToPixel(pt);

        if (Mode == DrawingMode.Paint && (_lastPoint.X != px || _lastPoint.Y != py))
        {
            int x0 = (int)_lastPoint.X;
            int y0 = (int)_lastPoint.Y;
            double dist = Math.Sqrt((px - x0) * (px - x0) + (py - y0) * (py - y0));
            int steps = Math.Max(2, (int)Math.Ceiling(dist * 2.5));
            bool eraser = Tool == DrawingCanvasTool.Eraser;
            for (int s = 0; s <= steps; s++)
            {
                double t = steps > 0 ? (double)s / steps : 1;
                int ix = (int)Math.Round(x0 + t * (px - x0));
                int iy = (int)Math.Round(y0 + t * (py - y0));
                DrawPixelOrBrush(ix, iy, eraser);
            }
        }
        else if (Mode == DrawingMode.Tile && (px != (int)_lastPoint.X || py != (int)_lastPoint.Y))
        {
            bool eraser = Tool == DrawingCanvasTool.Eraser;
            DrawPixelOrBrush(px, py, eraser);
        }

        _lastPoint = new System.Windows.Point(px, py);
        CompositeToDisplay();
        SetDirty(true);
    }

    private void CanvasHost_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDrawing = false;
        if (CanvasHost.IsMouseCaptured)
            CanvasHost.ReleaseMouseCapture();
    }

    private void CanvasHost_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!CanvasHost.IsMouseCaptured)
            _isDrawing = false;
    }

    private void CanvasHost_LostMouseCapture(object sender, RoutedEventArgs e)
    {
        _isDrawing = false;
    }

    private void CanvasHost_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Z && (Keyboard.Modifiers & ModifierKeys.Control) != 0 && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
        {
            if (TryUndoDrawing())
                e.Handled = true;
        }
    }
}
