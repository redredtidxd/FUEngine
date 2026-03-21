using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace FUEngine;

public partial class TileEditorTabContent : System.Windows.Controls.UserControl
{
    private string? _currentAssetPath;
    private string _projectDirectory = "";
    private readonly List<System.Windows.Media.Color> _palette = new();
    private Border? _selectedPaletteBorder;

    private bool _animationMode;
    private readonly List<(int w, int h, byte[] pixels)> _animationFrames = new();
    private int _selectedFrameIndex;
    private DispatcherTimer? _previewTimer;
    private int _previewFrameIndex;
    private bool _isPreviewPlaying;

    public TileEditorTabContent()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            RefreshPalettePanel();
            if (DrawingCanvas != null)
            {
                DrawingCanvas.IsDirtyChanged += (_, __) => DirtyChanged?.Invoke(this, DrawingCanvas.IsDirty);
                DrawingCanvas.LayersChanged += (_, __) => RefreshLayersList();
            }
        };
        Unloaded += (_, _) => StopPreviewTimer();
    }

    public event EventHandler<bool>? DirtyChanged;

    public string? CurrentAssetPath => _currentAssetPath;

    public void LoadAsset(string fullPath)
    {
        _currentAssetPath = fullPath;
        if (TxtPath != null)
            TxtPath.Text = "Tile Editor — " + Path.GetFileName(fullPath);

        if (!File.Exists(fullPath)) return;

        StopPreviewTimer();
        _animationFrames.Clear();
        _animationMode = false;
        if (AnimationPanel != null) AnimationPanel.Visibility = Visibility.Collapsed;
        if (ColAnimation != null) { ColAnimation.Width = new GridLength(0); ColAnimation.MinWidth = 0; }

        try
        {
            var tiledataPath = TileDataFile.GetTileDataPath(fullPath);
            var dto = TileDataFile.Load(tiledataPath);
            int gridSize = dto?.GridSize ?? 16;
            int frameCount = dto?.FrameCount ?? 1;
            int fps = dto?.Fps > 0 ? dto.Fps : 8;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            if (frameCount > 1 && bitmap.PixelWidth >= gridSize * frameCount && bitmap.PixelHeight >= gridSize)
            {
                _animationMode = true;
                var converted = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
                int srcW = bitmap.PixelWidth;
                int srcStride = srcW * 4;
                var fullPixels = new byte[srcW * bitmap.PixelHeight * 4];
                converted.CopyPixels(fullPixels, srcStride, 0);
                for (int f = 0; f < frameCount; f++)
                {
                    var framePixels = new byte[gridSize * gridSize * 4];
                    for (int py = 0; py < gridSize; py++)
                    for (int px = 0; px < gridSize; px++)
                    {
                        int srcX = f * gridSize + px;
                        int srcI = (py * srcW + srcX) * 4;
                        int dstI = (py * gridSize + px) * 4;
                        framePixels[dstI] = fullPixels[srcI];
                        framePixels[dstI + 1] = fullPixels[srcI + 1];
                        framePixels[dstI + 2] = fullPixels[srcI + 2];
                        framePixels[dstI + 3] = fullPixels[srcI + 3];
                    }
                    _animationFrames.Add((gridSize, gridSize, framePixels));
                }
                _selectedFrameIndex = 0;
                if (AnimationPanel != null) AnimationPanel.Visibility = Visibility.Visible;
                if (ColAnimation != null) { ColAnimation.Width = new GridLength(180); ColAnimation.MinWidth = 160; }
                if (TxtFps != null) TxtFps.Text = fps.ToString();
                RefreshFramesList();
                LoadFrameToCanvas(0);
                if (DrawingCanvas != null)
                {
                    DrawingCanvas.Mode = DrawingCanvasControl.DrawingMode.Tile;
                    DrawingCanvas.GridSize = gridSize;
                }
            }
            else
            {
                DrawingCanvas?.LoadFromBitmap(bitmap);
                if (DrawingCanvas != null)
                {
                    DrawingCanvas.Mode = DrawingCanvasControl.DrawingMode.Tile;
                    var (w, h) = DrawingCanvas.GetSize();
                    DrawingCanvas.GridSize = w == 32 && h == 32 ? 32 : 16;
                }
            }

            _palette.Clear();
            if (dto?.Palette != null)
            {
                foreach (var hex in dto.Palette)
                {
                    try
                    {
                        _palette.Add((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
                    }
                    catch { /* skip invalid */ }
                }
            }
            if (_palette.Count == 0)
                InitDefaultPalette();
            RefreshPalettePanel();
            if (_palette.Count > 0 && DrawingCanvas != null)
                DrawingCanvas.PrimaryColor = _palette[0];
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"No se pudo cargar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        RefreshLayersList();
    }

    public void SetProjectDirectory(string projectDirectory) => _projectDirectory = projectDirectory ?? "";

    private void InitDefaultPalette()
    {
        _palette.Clear();
        foreach (var hex in new[] { "#000000", "#FFFFFF", "#808080", "#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#FF00FF", "#00FFFF" })
            _palette.Add((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
    }

    private void RefreshPalettePanel()
    {
        if (PalettePanel == null) return;
        PalettePanel.Children.Clear();
        _selectedPaletteBorder = null;
        foreach (var color in _palette)
        {
            var border = new Border
            {
                Width = 24,
                Height = 24,
                Margin = new Thickness(2),
                Background = new SolidColorBrush(color),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x58, 0xa6, 0xff)),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = color
            };
            border.MouseLeftButtonDown += (s, _) =>
            {
                if (s is Border b && b.Tag is System.Windows.Media.Color c)
                {
                    _selectedPaletteBorder = b;
                    foreach (var child in PalettePanel.Children.OfType<Border>())
                        child.BorderThickness = new Thickness(0);
                    b.BorderThickness = new Thickness(2);
                    if (DrawingCanvas != null)
                        DrawingCanvas.PrimaryColor = c;
                }
            };
            PalettePanel.Children.Add(border);
        }
        if (PalettePanel.Children.Count > 0 && _selectedPaletteBorder == null)
        {
            _selectedPaletteBorder = PalettePanel.Children[0] as Border;
            if (_selectedPaletteBorder?.Tag is System.Windows.Media.Color first)
            {
                DrawingCanvas!.PrimaryColor = first;
                _selectedPaletteBorder.BorderThickness = new Thickness(2);
            }
        }
    }

    private void Tool_Checked(object sender, RoutedEventArgs e)
    {
        if (DrawingCanvas == null) return;
        if (RbEraser?.IsChecked == true)
            DrawingCanvas.Tool = DrawingCanvasControl.DrawingCanvasTool.Eraser;
        else if (RbFill?.IsChecked == true)
            DrawingCanvas.Tool = DrawingCanvasControl.DrawingCanvasTool.Fill;
        else
            DrawingCanvas.Tool = DrawingCanvasControl.DrawingCanvasTool.Pencil;
    }

    private void RefreshLayersList()
    {
        if (LayersList == null || DrawingCanvas == null) return;
        var sel = LayersList.SelectedIndex;
        LayersList.Items.Clear();
        for (int i = 0; i < DrawingCanvas.LayerCount; i++)
            LayersList.Items.Add("Capa " + (i + 1));
        if (sel >= 0 && sel < DrawingCanvas.LayerCount)
            LayersList.SelectedIndex = sel;
        else if (DrawingCanvas.LayerCount > 0)
            LayersList.SelectedIndex = DrawingCanvas.CurrentLayerIndex;
        if (ChkLayerVisible != null)
            ChkLayerVisible.IsChecked = DrawingCanvas.LayerCount > 0 && DrawingCanvas.IsLayerVisible(DrawingCanvas.CurrentLayerIndex);
    }

    private void LayersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DrawingCanvas == null || LayersList?.SelectedIndex is not int idx || idx < 0) return;
        DrawingCanvas.SetCurrentLayer(idx);
        if (ChkLayerVisible != null)
            ChkLayerVisible.IsChecked = DrawingCanvas.IsLayerVisible(idx);
    }

    private void ChkLayerVisible_Changed(object sender, RoutedEventArgs e)
    {
        if (DrawingCanvas == null || ChkLayerVisible == null) return;
        int idx = DrawingCanvas.CurrentLayerIndex;
        if (idx >= 0) DrawingCanvas.SetLayerVisible(idx, ChkLayerVisible.IsChecked == true);
    }

    private void BtnAddLayer_OnClick(object sender, RoutedEventArgs e) => DrawingCanvas?.AddLayer();

    private void BtnRemoveLayer_OnClick(object sender, RoutedEventArgs e)
    {
        if (DrawingCanvas == null || DrawingCanvas.LayerCount <= 1) return;
        int idx = LayersList?.SelectedIndex ?? DrawingCanvas.CurrentLayerIndex;
        if (idx >= 0) DrawingCanvas.RemoveLayer(idx);
        RefreshLayersList();
    }

    private void BtnAddColor_OnClick(object sender, RoutedEventArgs e)
    {
        _palette.Add(System.Windows.Media.Color.FromRgb(128, 128, 128));
        RefreshPalettePanel();
    }

    private void BtnSave_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentAssetPath) || DrawingCanvas == null) return;
        System.Windows.Media.Imaging.WriteableBitmap? bitmap;
        int frameCount = 1;
        int fps = 8;
        if (_animationMode && _animationFrames.Count > 0)
        {
            SaveCanvasToCurrentFrame();
            bitmap = BuildAnimationSpritesheet();
            frameCount = _animationFrames.Count;
            fps = int.TryParse(TxtFps?.Text, out var v) && v >= 1 ? v : 8;
        }
        else
            bitmap = DrawingCanvas.GetBitmap();
        if (bitmap == null) return;
        try
        {
            using var stream = File.Create(_currentAssetPath);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(stream);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"No se pudo guardar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        var tiledataPath = TileDataFile.GetTileDataPath(_currentAssetPath);
        var dto = new TileDataDto
        {
            GridSize = DrawingCanvas.GridSize,
            Palette = _palette.Select(c => $"#{c.R:X2}{c.G:X2}{c.B:X2}").ToList(),
            Fps = frameCount > 1 ? fps : 0,
            FrameCount = frameCount
        };
        TileDataFile.Save(tiledataPath, dto);
        DrawingCanvas.SetDirty(false);
    }

    private void SaveCanvasToCurrentFrame()
    {
        if (!_animationMode || DrawingCanvas == null || _selectedFrameIndex < 0 || _selectedFrameIndex >= _animationFrames.Count) return;
        var (w, h, _) = _animationFrames[_selectedFrameIndex];
        var pixels = DrawingCanvas.GetPixels();
        if (pixels != null && pixels.Length >= w * h * 4)
        {
            var copy = new byte[pixels.Length];
            Array.Copy(pixels, copy, pixels.Length);
            _animationFrames[_selectedFrameIndex] = (w, h, copy);
        }
    }

    private void LoadFrameToCanvas(int index)
    {
        if (DrawingCanvas == null || index < 0 || index >= _animationFrames.Count) return;
        var (w, h, pixels) = _animationFrames[index];
        var bmp = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgra32, null);
        bmp.WritePixels(new Int32Rect(0, 0, w, h), pixels, w * 4, 0);
        DrawingCanvas.LoadFromBitmap(bmp);
        if (PreviewImage != null)
            PreviewImage.Source = bmp;
    }

    private void RefreshFramesList()
    {
        if (FramesList == null) return;
        var sel = FramesList.SelectedIndex;
        FramesList.Items.Clear();
        for (int i = 0; i < _animationFrames.Count; i++)
            FramesList.Items.Add($"Frame {i + 1}");
        if (sel >= 0 && sel < _animationFrames.Count)
            FramesList.SelectedIndex = sel;
        else if (_animationFrames.Count > 0)
            FramesList.SelectedIndex = _selectedFrameIndex;
    }

    private void FramesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_animationMode || FramesList?.SelectedIndex is not int idx || idx < 0 || idx == _selectedFrameIndex) return;
        SaveCanvasToCurrentFrame();
        _selectedFrameIndex = idx;
        LoadFrameToCanvas(idx);
    }

    private void BtnPlayStop_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_animationMode || _animationFrames.Count == 0) return;
        _isPreviewPlaying = !_isPreviewPlaying;
        if (_isPreviewPlaying)
        {
            SaveCanvasToCurrentFrame();
            BtnPlayStop.Content = "Detener";
            _previewFrameIndex = _selectedFrameIndex;
            var fps = int.TryParse(TxtFps?.Text, out var v) && v >= 1 ? v : 8;
            _previewTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(1000.0 / fps) };
            _previewTimer.Tick += PreviewTimer_Tick;
            _previewTimer.Start();
        }
        else
        {
            BtnPlayStop.Content = "Reproducir";
            StopPreviewTimer();
        }
    }

    private void PreviewTimer_Tick(object? sender, EventArgs e)
    {
        if (_animationFrames.Count == 0) return;
        _previewFrameIndex = (_previewFrameIndex + 1) % _animationFrames.Count;
        var (w, h, pixels) = _animationFrames[_previewFrameIndex];
        var bmp = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgra32, null);
        bmp.WritePixels(new Int32Rect(0, 0, w, h), pixels, w * 4, 0);
        if (PreviewImage != null)
            PreviewImage.Source = bmp;
    }

    private void StopPreviewTimer()
    {
        _previewTimer?.Stop();
        _previewTimer = null;
        if (BtnPlayStop != null)
            BtnPlayStop.Content = "Reproducir";
        _isPreviewPlaying = false;
    }

    private void BtnDuplicateFrame_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_animationMode || _selectedFrameIndex < 0 || _selectedFrameIndex >= _animationFrames.Count) return;
        var (w, h, pixels) = _animationFrames[_selectedFrameIndex];
        var copy = new byte[pixels.Length];
        Array.Copy(pixels, copy, pixels.Length);
        _animationFrames.Insert(_selectedFrameIndex + 1, (w, h, copy));
        _selectedFrameIndex++;
        RefreshFramesList();
        LoadFrameToCanvas(_selectedFrameIndex);
    }

    private void BtnDeleteFrame_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_animationMode || _animationFrames.Count <= 1) return;
        _animationFrames.RemoveAt(_selectedFrameIndex);
        if (_selectedFrameIndex >= _animationFrames.Count) _selectedFrameIndex = _animationFrames.Count - 1;
        RefreshFramesList();
        LoadFrameToCanvas(_selectedFrameIndex);
    }

    private void BtnAddTileFromProject_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_animationMode || DrawingCanvas == null) return;
        var gridSize = DrawingCanvas.GridSize;
        var tilesDir = string.IsNullOrEmpty(_projectDirectory) ? "" : Path.Combine(_projectDirectory, "Assets", "Tiles");
        if (!Directory.Exists(tilesDir)) tilesDir = _projectDirectory ?? "";
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "PNG|*.png|Todos|*.*",
            InitialDirectory = tilesDir,
            Title = "Agregar tile del proyecto"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new System.Uri(dlg.FileName, System.UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            int w = bitmap.PixelWidth;
            int h = bitmap.PixelHeight;
            if (w < gridSize || h < gridSize) return;
            var converted = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
            int stride = w * 4;
            var fullPixels = new byte[w * h * 4];
            converted.CopyPixels(fullPixels, stride, 0);
            int frameW = gridSize;
            int frameH = gridSize;
            int nx = w / frameW;
            int ny = h / frameH;
            for (int fy = 0; fy < ny; fy++)
            for (int fx = 0; fx < nx; fx++)
            {
                var framePixels = new byte[frameW * frameH * 4];
                for (int py = 0; py < frameH; py++)
                for (int px = 0; px < frameW; px++)
                {
                    int srcX = fx * frameW + px;
                    int srcY = fy * frameH + py;
                    int srcI = (srcY * w + srcX) * 4;
                    int dstI = (py * frameW + px) * 4;
                    framePixels[dstI] = fullPixels[srcI];
                    framePixels[dstI + 1] = fullPixels[srcI + 1];
                    framePixels[dstI + 2] = fullPixels[srcI + 2];
                    framePixels[dstI + 3] = fullPixels[srcI + 3];
                }
                _animationFrames.Add((frameW, frameH, framePixels));
            }
            if (_animationFrames.Count > 0)
            {
                _selectedFrameIndex = _animationFrames.Count - 1;
                RefreshFramesList();
                LoadFrameToCanvas(_selectedFrameIndex);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"No se pudo cargar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private WriteableBitmap? BuildAnimationSpritesheet()
    {
        if (_animationFrames.Count == 0) return null;
        var (frameW, frameH, _) = _animationFrames[0];
        int totalW = frameW * _animationFrames.Count;
        var result = new WriteableBitmap(totalW, frameH, 96, 96, PixelFormats.Bgra32, null);
        for (int i = 0; i < _animationFrames.Count; i++)
        {
            var (_, __, pixels) = _animationFrames[i];
            result.WritePixels(new Int32Rect(i * frameW, 0, frameW, frameH), pixels, frameW * 4, 0);
        }
        return result;
    }
}
