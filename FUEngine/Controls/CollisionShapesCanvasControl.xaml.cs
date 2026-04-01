using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Canvas = System.Windows.Controls.Canvas;

namespace FUEngine;

/// <summary>
/// Canvas that displays an image with collision shapes overlay (Box/Circle/Polygon/Capsule).
/// Coordinates in image pixels. Phase 1: Box only; create by drag, select, delete, resize via handles.
/// </summary>
public partial class CollisionShapesCanvasControl : System.Windows.Controls.UserControl
{
    private readonly List<CollisionShapeDto> _shapes = new();
    private BitmapSource? _image;
    private int _imageWidth;
    private int _imageHeight;
    private string _currentLayer = "Solid";
    private bool _isDraggingNew;
    private Point _dragStart;
    private Point _dragEnd;
    private Rectangle? _previewRect;
    private int _selectedIndex = -1;
    private int _resizeHandle = -1; // 0-7: corners and edges
    private Point _resizeStartPoint;

    private static readonly Brush SolidBrush = new SolidColorBrush(Color.FromRgb(0x2e, 0xc7, 0x2e));   // Green
    private static readonly Brush TriggerBrush = new SolidColorBrush(Color.FromRgb(0xf0, 0x88, 0x3e));  // Orange
    private static readonly Brush OneWayBrush = new SolidColorBrush(Color.FromRgb(0x58, 0xa6, 0xff));  // Blue
    private static readonly Brush SelectedBrush = new SolidColorBrush(Color.FromRgb(0xff, 0xd7, 0x00)); // Gold
    private const double HandleSize = 6;
    private const double ShapeStrokeThickness = 2;
    private const double SelectedStrokeThickness = 3;

    public CollisionShapesCanvasControl()
    {
        InitializeComponent();
        Loaded += (_, _) => HostBorder?.Focus();
    }

    public string CurrentLayer
    {
        get => _currentLayer;
        set => _currentLayer = value ?? "Solid";
    }

    /// <summary>Gets a copy of the shapes list. Call SetShapes to replace.</summary>
    public IReadOnlyList<CollisionShapeDto> GetShapes() => _shapes.ToList();

    public void SetShapes(IEnumerable<CollisionShapeDto>? shapes)
    {
        _shapes.Clear();
        if (shapes != null)
            _shapes.AddRange(shapes);
        _selectedIndex = -1;
        RedrawShapes();
    }

    public int SelectedIndex => _selectedIndex;

    public void SelectIndex(int index)
    {
        if (index == _selectedIndex) return;
        _selectedIndex = index >= 0 && index < _shapes.Count ? index : -1;
        RedrawShapes();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void LoadImage(BitmapSource? source)
    {
        _image = source;
        if (source == null)
        {
            _imageWidth = 0;
            _imageHeight = 0;
            BackgroundImage.Source = null;
            BackgroundImage.Width = 0;
            BackgroundImage.Height = 0;
            ShapesOverlay.Width = 0;
            ShapesOverlay.Height = 0;
            ShapesOverlay.Children.Clear();
            return;
        }
        _imageWidth = source.PixelWidth;
        _imageHeight = source.PixelHeight;
        BackgroundImage.Source = source;
        BackgroundImage.Width = _imageWidth;
        BackgroundImage.Height = _imageHeight;
        ShapesOverlay.Width = _imageWidth;
        ShapesOverlay.Height = _imageHeight;
        ShapesOverlay.Children.Clear();
        _previewRect = null;
        RedrawShapes();
    }

    public (int w, int h) GetImageSize() => (_imageWidth, _imageHeight);

    public event EventHandler? SelectionChanged;
    public event EventHandler? ShapesChanged;

    private static Brush BrushForLayer(string layer)
    {
        return layer switch
        {
            "Trigger" => TriggerBrush,
            "OneWay" => OneWayBrush,
            _ => SolidBrush
        };
    }

    /// <summary>Gets mouse position in image pixel coordinates (relative to ShapesOverlay).</summary>
    private Point GetImagePosition(MouseEventArgs e)
    {
        if (ShapesOverlay == null) return new Point(0, 0);
        return e.GetPosition(ShapesOverlay);
    }

    private void RedrawShapes()
    {
        ShapesOverlay.Children.Clear();
        if (_imageWidth <= 0 || _imageHeight <= 0) return;

        for (int i = 0; i < _shapes.Count; i++)
        {
            var s = _shapes[i];
            if (s.Type != "Box") continue;
            var brush = (i == _selectedIndex) ? SelectedBrush : BrushForLayer(s.Layer);
            var stroke = (i == _selectedIndex) ? SelectedStrokeThickness : ShapeStrokeThickness;
            var rect = new Rectangle
            {
                Width = Math.Max(1, s.Width),
                Height = Math.Max(1, s.Height),
                Stroke = brush,
                StrokeThickness = stroke,
                Fill = Brushes.Transparent
            };
            Canvas.SetLeft(rect, s.X);
            Canvas.SetTop(rect, s.Y);
            rect.Tag = i;
            rect.MouseLeftButtonDown += Shape_MouseLeftButtonDown;
            ShapesOverlay.Children.Add(rect);
        }

        if (_selectedIndex >= 0 && _selectedIndex < _shapes.Count && _shapes[_selectedIndex].Type == "Box")
            DrawResizeHandles(_shapes[_selectedIndex]);

        if (_previewRect != null)
            ShapesOverlay.Children.Add(_previewRect);
    }

    private void DrawResizeHandles(CollisionShapeDto box)
    {
        double x = box.X, y = box.Y, w = box.Width, h = box.Height;
        if (w < 0) { x += w; w = -w; }
        if (h < 0) { y += h; h = -h; }
        var handles = new[] {
            (x, y), (x + w / 2, y), (x + w, y), (x + w, y + h / 2),
            (x + w, y + h), (x + w / 2, y + h), (x, y + h), (x, y + h / 2)
        };
        for (int i = 0; i < handles.Length; i++)
        {
            var (hx, hy) = handles[i];
            var r = new Rectangle
            {
                Width = HandleSize,
                Height = HandleSize,
                Fill = SelectedBrush,
                Stroke = Brushes.White,
                StrokeThickness = 1,
                Tag = i
            };
            Canvas.SetLeft(r, hx - HandleSize / 2);
            Canvas.SetTop(r, hy - HandleSize / 2);
            r.MouseLeftButtonDown += Handle_MouseLeftButtonDown;
            ShapesOverlay.Children.Add(r);
        }
    }

    private void Shape_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (sender is FrameworkElement fe && fe.Tag is int idx)
        {
            SelectIndex(idx);
            HostBorder?.Focus();
        }
    }

    private void Handle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (sender is FrameworkElement fe && fe.Tag is int handleIdx)
        {
            _resizeHandle = handleIdx;
            _resizeStartPoint = GetImagePosition(e);
            HostBorder?.Focus();
        }
    }

    private void Host_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_imageWidth <= 0 || _imageHeight <= 0) return;
        var pt = GetImagePosition(e);
        if (_resizeHandle >= 0 && _selectedIndex >= 0 && _selectedIndex < _shapes.Count)
        {
            _resizeStartPoint = pt;
            return;
        }
        int hit = HitTestShape(pt);
        if (hit >= 0)
        {
            SelectIndex(hit);
            return;
        }
        _selectedIndex = -1;
        RedrawShapes();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        _isDraggingNew = true;
        _dragStart = pt;
        _dragEnd = pt;
        _previewRect = new Rectangle
        {
            Stroke = BrushForLayer(_currentLayer),
            StrokeThickness = ShapeStrokeThickness,
            Fill = Brushes.Transparent
        };
        UpdatePreviewRect();
        ShapesOverlay.Children.Add(_previewRect);
        HostBorder?.Focus();
    }

    private int HitTestShape(Point pt)
    {
        for (int i = _shapes.Count - 1; i >= 0; i--)
        {
            var s = _shapes[i];
            if (s.Type != "Box") continue;
            double x = s.X, y = s.Y, w = s.Width, h = s.Height;
            if (w < 0) { x += w; w = -w; }
            if (h < 0) { y += h; h = -h; }
            if (pt.X >= x && pt.X <= x + w && pt.Y >= y && pt.Y <= y + h)
                return i;
        }
        return -1;
    }

    private void Host_MouseMove(object sender, MouseEventArgs e)
    {
        if (_imageWidth <= 0 || _imageHeight <= 0) return;
        var pt = GetImagePosition(e);

        if (_resizeHandle >= 0 && _selectedIndex >= 0 && _selectedIndex < _shapes.Count)
        {
            var box = _shapes[_selectedIndex];
            double x = box.X, y = box.Y, w = box.Width, h = box.Height;
            if (w < 0) { x += w; w = -w; }
            if (h < 0) { y += h; h = -h; }
            double dx = pt.X - _resizeStartPoint.X, dy = pt.Y - _resizeStartPoint.Y;
            _resizeStartPoint = pt;
            switch (_resizeHandle)
            {
                case 0: box.X += (float)dx; box.Y += (float)dy; box.Width -= (float)dx; box.Height -= (float)dy; break;
                case 1: box.Y += (float)dy; box.Height -= (float)dy; break;
                case 2: box.Width = (float)(pt.X - box.X); box.Y += (float)dy; box.Height -= (float)dy; break;
                case 3: box.Width = (float)(pt.X - box.X); break;
                case 4: box.Width = (float)(pt.X - box.X); box.Height = (float)(pt.Y - box.Y); break;
                case 5: box.Height = (float)(pt.Y - box.Y); break;
                case 6: box.X += (float)dx; box.Width -= (float)dx; box.Height = (float)(pt.Y - box.Y); break;
                case 7: box.X += (float)dx; box.Width -= (float)dx; break;
            }
            RedrawShapes();
            ShapesChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (_isDraggingNew)
        {
            _dragEnd = new Point(
                Math.Clamp(pt.X, 0, _imageWidth),
                Math.Clamp(pt.Y, 0, _imageHeight));
            UpdatePreviewRect();
        }
    }

    private void UpdatePreviewRect()
    {
        if (_previewRect == null) return;
        double x1 = Math.Min(_dragStart.X, _dragEnd.X), x2 = Math.Max(_dragStart.X, _dragEnd.X);
        double y1 = Math.Min(_dragStart.Y, _dragEnd.Y), y2 = Math.Max(_dragStart.Y, _dragEnd.Y);
        Canvas.SetLeft(_previewRect, x1);
        Canvas.SetTop(_previewRect, y1);
        _previewRect.Width = Math.Max(1, x2 - x1);
        _previewRect.Height = Math.Max(1, y2 - y1);
    }

    private void Host_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_resizeHandle >= 0)
        {
            _resizeHandle = -1;
            return;
        }
        if (_isDraggingNew)
        {
            _isDraggingNew = false;
            double x1 = Math.Min(_dragStart.X, _dragEnd.X), x2 = Math.Max(_dragStart.X, _dragEnd.X);
            double y1 = Math.Min(_dragStart.Y, _dragEnd.Y), y2 = Math.Max(_dragStart.Y, _dragEnd.Y);
            if (x2 - x1 >= 1 && y2 - y1 >= 1)
            {
                _shapes.Add(new CollisionShapeDto
                {
                    Type = "Box",
                    Layer = _currentLayer,
                    X = (float)x1,
                    Y = (float)y1,
                    Width = (float)(x2 - x1),
                    Height = (float)(y2 - y1)
                });
                _selectedIndex = _shapes.Count - 1;
                ShapesChanged?.Invoke(this, EventArgs.Empty);
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
            _previewRect = null;
            RedrawShapes();
        }
    }

    private void Host_MouseLeave(object sender, MouseEventArgs e)
    {
        if (_isDraggingNew)
        {
            _isDraggingNew = false;
            _previewRect = null;
            RedrawShapes();
        }
        _resizeHandle = -1;
    }

    private void Host_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Delete && e.Key != Key.Back) return;
        if (_selectedIndex < 0 || _selectedIndex >= _shapes.Count) return;
        _shapes.RemoveAt(_selectedIndex);
        _selectedIndex = Math.Min(_selectedIndex, _shapes.Count - 1);
        if (_selectedIndex >= _shapes.Count) _selectedIndex = -1;
        RedrawShapes();
        ShapesChanged?.Invoke(this, EventArgs.Empty);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    public void DeleteSelected()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _shapes.Count) return;
        _shapes.RemoveAt(_selectedIndex);
        _selectedIndex = Math.Min(_selectedIndex, _shapes.Count - 1);
        if (_selectedIndex >= _shapes.Count) _selectedIndex = -1;
        RedrawShapes();
        ShapesChanged?.Invoke(this, EventArgs.Empty);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetLayerOfSelected(string layer)
    {
        if (_selectedIndex >= 0 && _selectedIndex < _shapes.Count)
        {
            _shapes[_selectedIndex].Layer = layer ?? "Solid";
            RedrawShapes();
            ShapesChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
