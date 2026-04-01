using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FUEngine.Core;
using Microsoft.Win32;
using System.IO;
using System.Windows.Shapes;
using Rectangle = System.Windows.Shapes.Rectangle;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace FUEngine;

public partial class PixelEditWindow : Window
{
    private readonly int _worldTx, _worldTy;
    private readonly int _tileSize;
    private TileData _tileData;
    private TilePixelOverlay _overlay;
    private readonly Action<TileData> _onSave;
    private readonly string? _projectDirectory;
    private readonly System.Windows.Media.Color _baseColor;
    private byte[]? _baseImageRgba;
    private int _baseImageWidth;
    private int _baseImageHeight;
    private (byte r, byte g, byte b, byte a) _paintColor = (255, 255, 255, 255);
    private bool _eraseMode;
    private bool _fillMode;
    private bool _eraseFromRightClick;
    private readonly List<byte[]> _undoStack = new();
    private const int MaxUndo = 50;
    private System.Windows.Shapes.Rectangle?[,]? _cells;
    private bool _mouseDown;

    public PixelEditWindow(int worldTx, int worldTy, TileData tileDataClone, int tileSizePx,
        System.Windows.Media.Color baseColor, Action<TileData> onSave, string? projectDirectory = null)
    {
        InitializeComponent();
        _worldTx = worldTx;
        _worldTy = worldTy;
        _tileSize = tileSizePx;
        _tileData = tileDataClone;
        _baseColor = baseColor;
        _onSave = onSave;
        _projectDirectory = projectDirectory;
        _overlay = _tileData.PixelOverlay ?? new TilePixelOverlay(tileSizePx, tileSizePx);
        if (_tileData.PixelOverlay == null)
            _tileData.PixelOverlay = _overlay;
        else
            _overlay = _tileData.PixelOverlay.Clone();
        _tileData.PixelOverlay = _overlay;
        _overlay.EnsureSize(tileSizePx, tileSizePx);
        LoadBaseImage();
        TxtTitle.Text = $"Tile ({worldTx}, {worldTy}) — {tileSizePx}×{tileSizePx} píxeles";
        BorderColor.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(_paintColor.r, _paintColor.g, _paintColor.b));
        BuildPixelGrid();
        PushUndo();
    }

    private void LoadBaseImage()
    {
        _baseImageRgba = null;
        _baseImageWidth = _baseImageHeight = 0;
        if (string.IsNullOrWhiteSpace(_tileData.SourceImagePath) || string.IsNullOrWhiteSpace(_projectDirectory)) return;
        var fullPath = System.IO.Path.Combine(_projectDirectory, _tileData.SourceImagePath);
        var rgba = TileImageLoader.LoadToRgba(fullPath, _tileSize, _tileSize);
        if (rgba != null && rgba.Length >= _tileSize * _tileSize * 4)
        {
            _baseImageRgba = rgba;
            _baseImageWidth = _tileSize;
            _baseImageHeight = _tileSize;
        }
    }

    private System.Windows.Media.Color GetBaseColorAt(int px, int py)
    {
        if (_baseImageRgba != null && _baseImageWidth > 0 && _baseImageHeight > 0 && px >= 0 && px < _baseImageWidth && py >= 0 && py < _baseImageHeight)
            return TileImageLoader.GetPixelColor(_baseImageRgba, _baseImageWidth, _baseImageHeight, px, py);
        return _baseColor;
    }

    private void BuildPixelGrid()
    {
        PixelGrid.Children.Clear();
        PixelGrid.RowDefinitions.Clear();
        PixelGrid.ColumnDefinitions.Clear();
        for (int i = 0; i < _tileSize; i++)
        {
            PixelGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            PixelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }
        const int cellSize = 16;
        PixelGrid.Width = _tileSize * cellSize;
        PixelGrid.Height = _tileSize * cellSize;
        _cells = new Rectangle[_tileSize, _tileSize];
        for (int py = 0; py < _tileSize; py++)
            for (int px = 0; px < _tileSize; px++)
            {
                var (r, g, b, a) = _overlay.GetPixel(px, py);
                var fill = a > 0
                    ? new SolidColorBrush(System.Windows.Media.Color.FromArgb(a, r, g, b))
                    : new SolidColorBrush(GetBaseColorAt(px, py));
                var rect = new Rectangle
                {
                    Width = cellSize,
                    Height = cellSize,
                    Fill = fill,
                    Stroke = System.Windows.Media.Brushes.DimGray,
                    StrokeThickness = 0.5
                };
                Grid.SetColumn(rect, px);
                Grid.SetRow(rect, py);
                PixelGrid.Children.Add(rect);
                _cells[px, py] = rect;
            }
    }

    private void RefreshCell(int px, int py)
    {
        if (_cells == null || px < 0 || px >= _tileSize || py < 0 || py >= _tileSize) return;
        var (r, g, b, a) = _overlay.GetPixel(px, py);
        _cells[px, py]!.Fill = a > 0
            ? new SolidColorBrush(System.Windows.Media.Color.FromArgb(a, r, g, b))
            : new SolidColorBrush(GetBaseColorAt(px, py));
    }

    private void ApplyAt(int px, int py)
    {
        if (_fillMode)
        {
            PushUndo();
            FloodFill(px, py);
            _fillMode = false;
            BtnFill.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x21, 0x26, 0x2d));
            return;
        }
        if (_eraseMode)
            _overlay.SetPixel(px, py, 0, 0, 0, 0);
        else
            _overlay.SetPixel(px, py, _paintColor.r, _paintColor.g, _paintColor.b, _paintColor.a);
        RefreshCell(px, py);
    }

    private void FloodFill(int startPx, int startPy)
    {
        var (sr, sg, sb, sa) = _overlay.GetPixel(startPx, startPy);
        (byte br, byte bg, byte bb, byte ba) replaceWith = _eraseMode ? ((byte)0, (byte)0, (byte)0, (byte)0) : _paintColor;
        if (_eraseMode && sa == 0) return;
        if (!_eraseMode && sr == replaceWith.br && sg == replaceWith.bg && sb == replaceWith.bb && sa == replaceWith.ba) return;
        var stack = new Stack<(int x, int y)>();
        var visited = new HashSet<(int, int)>();
        stack.Push((startPx, startPy));
        int count = 0;
        const int maxFill = 10000;
        while (stack.Count > 0 && count < maxFill)
        {
            var (x, y) = stack.Pop();
            if (x < 0 || x >= _tileSize || y < 0 || y >= _tileSize || visited.Contains((x, y))) continue;
            var (rp, gp, bp, ap) = _overlay.GetPixel(x, y);
            if (rp != sr || gp != sg || bp != sb || ap != sa) continue;
            visited.Add((x, y));
            _overlay.SetPixel(x, y, replaceWith.br, replaceWith.bg, replaceWith.bb, replaceWith.ba);
            RefreshCell(x, y);
            count++;
            stack.Push((x + 1, y));
            stack.Push((x - 1, y));
            stack.Push((x, y + 1));
            stack.Push((x, y - 1));
        }
    }

    private (int px, int py)? GetCellFromPosition(System.Windows.Point pos)
    {
        if (_cells == null) return null;
        var el = PixelGrid.InputHitTest(pos) as FrameworkElement;
        if (el == null) return null;
        int col = Grid.GetColumn(el);
        int row = Grid.GetRow(el);
        if (col >= 0 && col < _tileSize && row >= 0 && row < _tileSize)
            return (col, row);
        return null;
    }

    private void PixelGrid_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _mouseDown = true;
        _eraseFromRightClick = (e.RightButton == MouseButtonState.Pressed);
        if (_eraseFromRightClick) _eraseMode = true;
        PixelGrid.CaptureMouse();
        var cell = GetCellFromPosition(e.GetPosition(PixelGrid));
        if (!cell.HasValue) return;
        PushUndo();
        ApplyAt(cell.Value.px, cell.Value.py);
    }

    private void PixelGrid_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_mouseDown) return;
        var cell = GetCellFromPosition(e.GetPosition(PixelGrid));
        if (cell.HasValue)
            ApplyAt(cell.Value.px, cell.Value.py);
    }

    private void PixelGrid_OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        _mouseDown = false;
        PixelGrid.ReleaseMouseCapture();
        if (_eraseFromRightClick)
        {
            _eraseMode = false;
            BtnErase.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x21, 0x26, 0x2d));
        }
    }

    private void PushUndo()
    {
        if (_overlay.RgbaData == null) return;
        var copy = new byte[_overlay.RgbaData.Length];
        Array.Copy(_overlay.RgbaData, copy, copy.Length);
        _undoStack.Add(copy);
        while (_undoStack.Count > MaxUndo)
            _undoStack.RemoveAt(0);
    }

    private void BtnUndo_OnClick(object sender, RoutedEventArgs e)
    {
        if (_undoStack.Count < 2) return;
        _undoStack.RemoveAt(_undoStack.Count - 1);
        var prev = _undoStack[_undoStack.Count - 1];
        Array.Copy(prev, _overlay.RgbaData, Math.Min(prev.Length, _overlay.RgbaData.Length));
        BuildPixelGrid();
    }

    private void BorderColor_OnClick(object sender, MouseButtonEventArgs e)
    {
        var colors = new[]
        {
            (255, 255, 255), (0, 0, 0), (255, 0, 0), (0, 255, 0), (0, 0, 255),
            (255, 255, 0), (255, 0, 255), (128, 128, 128), (255, 128, 0)
        };
        var idx = Array.IndexOf(colors, (_paintColor.r, _paintColor.g, _paintColor.b));
        idx = (idx + 1) % colors.Length;
        var c = colors[idx];
        _paintColor = ((byte)c.Item1, (byte)c.Item2, (byte)c.Item3, 255);
        BorderColor.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(_paintColor.r, _paintColor.g, _paintColor.b));
        _eraseMode = false;
        BtnErase.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x21, 0x26, 0x2d));
    }

    private void BtnErase_OnClick(object sender, RoutedEventArgs e)
    {
        _eraseMode = !_eraseMode;
        BtnErase.Background = _eraseMode
            ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x58, 0x35, 0x35))
            : new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x21, 0x26, 0x2d));
    }

    private void BtnFill_OnClick(object sender, RoutedEventArgs e)
    {
        _fillMode = true;
        BtnFill.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x38, 0x4a, 0x35));
    }

    private void BtnSave_OnClick(object sender, RoutedEventArgs e)
    {
        _tileData.PixelOverlay = _overlay.Clone();
        _onSave(_tileData);
        DialogResult = true;
        Close();
    }

    private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void BtnImageBase_OnClick(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Imágenes|*.png;*.jpg;*.jpeg;*.bmp|PNG|*.png|Todos|*.*",
            Title = "Seleccionar imagen base del tile"
        };
        if (dlg.ShowDialog() != true) return;
        var fullPath = dlg.FileName;
        if (string.IsNullOrWhiteSpace(_projectDirectory))
        {
            _tileData.SourceImagePath = fullPath;
        }
        else
        {
            try
            {
                var projectDir = System.IO.Path.GetFullPath(_projectDirectory);
                var fileDir = System.IO.Path.GetFullPath(System.IO.Path.GetDirectoryName(fullPath) ?? "");
                if (fileDir.StartsWith(projectDir, StringComparison.OrdinalIgnoreCase))
                    _tileData.SourceImagePath = System.IO.Path.GetRelativePath(projectDir, fullPath);
                else
                    _tileData.SourceImagePath = System.IO.Path.GetFileName(fullPath);
            }
            catch
            {
                _tileData.SourceImagePath = System.IO.Path.GetFileName(fullPath);
            }
        }
        LoadBaseImage();
        BuildPixelGrid();
    }
}
