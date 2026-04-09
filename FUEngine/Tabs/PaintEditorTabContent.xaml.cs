using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace FUEngine;

public partial class PaintEditorTabContent : System.Windows.Controls.UserControl
{
    private string? _currentAssetPath;
    private string _projectDirectory = "";

    public PaintEditorTabContent()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            try
            {
                if (DrawingCanvas != null)
                {
                    DrawingCanvas.IsDirtyChanged += (_, __) => DirtyChanged?.Invoke(this, DrawingCanvas.IsDirty);
                    DrawingCanvas.LayersChanged += (_, __) => RefreshLayersList();
                    if (DrawingCanvas.LayerCount == 0)
                        CreateCanvas(1920, 1080);
                }
                RefreshLayersList();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"No se pudo inicializar el editor de pintura: {ex.Message}", "Editor de Pintura", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        };
    }

    private void CreateCanvas(int width, int height)
    {
        DrawingCanvas?.CreateCanvas(width, height);
        if (DrawingCanvas != null)
        {
            DrawingCanvas.Mode = DrawingCanvasControl.DrawingMode.Paint;
            DrawingCanvas.Tool = DrawingCanvasControl.DrawingCanvasTool.Pencil;
            DrawingCanvas.BrushSize = int.TryParse(TxtBrushSize?.Text, out var bs) ? Math.Clamp(bs, 1, 64) : 4;
            DrawingCanvas.BrushOpacity = SliderOpacity?.Value ?? 1.0;
        }
    }

    public event EventHandler<bool>? DirtyChanged;

    public string? CurrentAssetPath => _currentAssetPath;

    /// <summary>Fired when the paint is saved; EditorWindow can refresh MapRenderer if this path is used as background layer.</summary>
    public event EventHandler<string>? PaintSaved;

    /// <summary>Fired when the user chooses to use the current paint as project icon. Argument is the relative path to the PNG.</summary>
    public event EventHandler<string>? RequestSetProjectIcon;
    /// <summary>Convertir lienzo actual en asset de tile y abrir Editor de Tiles.</summary>
    public event EventHandler? RequestConvertToTile;

    public void SetProjectDirectory(string projectDirectory) => _projectDirectory = projectDirectory ?? "";

    public void LoadAsset(string fullPath)
    {
        _currentAssetPath = fullPath;
        if (TxtPath != null)
            TxtPath.Text = "Paint Editor — " + Path.GetFileName(fullPath);

        if (!File.Exists(fullPath)) return;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            DrawingCanvas?.LoadFromBitmap(bitmap);
            if (DrawingCanvas != null)
            {
                DrawingCanvas.Mode = DrawingCanvasControl.DrawingMode.Paint;
                DrawingCanvas.Tool = DrawingCanvasControl.DrawingCanvasTool.Pencil;
                DrawingCanvas.BrushSize = int.TryParse(TxtBrushSize?.Text, out var bs) ? Math.Clamp(bs, 1, 64) : 4;
                DrawingCanvas.BrushOpacity = SliderOpacity?.Value ?? 1.0;
            }
            RefreshLayersList();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"No se pudo cargar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Tool_Checked(object sender, RoutedEventArgs e)
    {
        if (DrawingCanvas == null) return;
        if (RbPencil?.IsChecked == true) DrawingCanvas.Tool = DrawingCanvasControl.DrawingCanvasTool.Pencil;
        else if (RbEraser?.IsChecked == true) DrawingCanvas.Tool = DrawingCanvasControl.DrawingCanvasTool.Eraser;
        else if (RbFill?.IsChecked == true) DrawingCanvas.Tool = DrawingCanvasControl.DrawingCanvasTool.Fill;
    }

    private void TxtBrushSize_LostFocus(object sender, RoutedEventArgs e)
    {
        if (DrawingCanvas != null && int.TryParse(TxtBrushSize?.Text, out var bs))
            DrawingCanvas.BrushSize = Math.Clamp(bs, 1, 64);
    }

    private void SliderOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DrawingCanvas != null)
            DrawingCanvas.BrushOpacity = e.NewValue;
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

    private void BtnSave_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentAssetPath) || DrawingCanvas == null) return;
        var bitmap = DrawingCanvas.GetBitmap();
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
        DrawingCanvas.SetDirty(false);
        PaintSaved?.Invoke(this, _currentAssetPath);
    }

    private void BtnUseAsIcon_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentAssetPath)) return;
        string rel;
        if (!string.IsNullOrEmpty(_projectDirectory) && _currentAssetPath.StartsWith(_projectDirectory, StringComparison.OrdinalIgnoreCase))
            rel = Path.GetRelativePath(_projectDirectory, _currentAssetPath);
        else
            rel = Path.GetFileName(_currentAssetPath);
        RequestSetProjectIcon?.Invoke(this, rel);
    }

    private void BtnTransformToTile_OnClick(object sender, RoutedEventArgs e)
    {
        if (System.Windows.MessageBox.Show(
                "Esto transformará la imagen en un asset de Tile y abrirá el Editor de Tiles. ¿Continuar?",
                "Transformar en Tile",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        RequestConvertToTile?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Exporta el lienzo a PNG bajo Assets/Sprites para abrirlo como tile.</summary>
    public string? ExportBitmapToProjectSpritesForTile()
    {
        if (DrawingCanvas == null || string.IsNullOrEmpty(_projectDirectory)) return null;
        var bmp = DrawingCanvas.GetBitmap();
        if (bmp == null) return null;
        try
        {
            var dir = Path.Combine(_projectDirectory, "Assets", "Sprites");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"from_paint_tile_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png");
            using var stream = File.Create(path);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            encoder.Save(stream);
            return path;
        }
        catch
        {
            return null;
        }
    }
}
