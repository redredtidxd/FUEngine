using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace FUEngine;

public partial class PaintCreatorTabContent : System.Windows.Controls.UserControl
{
    private string _projectDirectory = "";

    public PaintCreatorTabContent()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (CmbSize != null)
            {
                CmbSize.Items.Add("1920 × 1080");
                CmbSize.Items.Add("1280 × 720");
                CmbSize.Items.Add("800 × 600");
                CmbSize.SelectedIndex = 0;
            }
            CreateCanvas(1920, 1080);
            if (DrawingCanvas != null)
            {
                DrawingCanvas.IsDirtyChanged += (_, __) => DirtyChanged?.Invoke(this, DrawingCanvas.IsDirty);
                DrawingCanvas.LayersChanged += (_, __) => RefreshLayersList();
            }
            RefreshLayersList();
        };
    }

    public event EventHandler<bool>? DirtyChanged;

    /// <summary>Fired when the user saves and chooses to use the saved paint as project icon. Argument is the relative path to the PNG.</summary>
    public event EventHandler<string>? RequestSetProjectIcon;

    public void SetProjectDirectory(string projectDirectory) => _projectDirectory = projectDirectory ?? "";

    private (int w, int h) GetSizeFromSelection()
    {
        var idx = CmbSize?.SelectedIndex ?? 0;
        return idx switch
        {
            1 => (1280, 720),
            2 => (800, 600),
            _ => (1920, 1080)
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

    private void CmbSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var (w, h) = GetSizeFromSelection();
        CreateCanvas(w, h);
    }

    private void Tool_Checked(object sender, RoutedEventArgs e)
    {
        if (DrawingCanvas == null) return;
        if (RbPencil?.IsChecked == true) DrawingCanvas.Tool = DrawingCanvasControl.DrawingCanvasTool.Pencil;
        else if (RbEraser?.IsChecked == true) DrawingCanvas.Tool = DrawingCanvasControl.DrawingCanvasTool.Eraser;
        else if (RbFill?.IsChecked == true) DrawingCanvas.Tool = DrawingCanvasControl.DrawingCanvasTool.Fill;
    }

    private void SliderOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DrawingCanvas != null)
            DrawingCanvas.BrushOpacity = e.NewValue;
    }

    private void BtnNew_OnClick(object sender, RoutedEventArgs e)
    {
        var (w, h) = GetSizeFromSelection();
        CreateCanvas(w, h);
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
        if (DrawingCanvas == null) return;
        var bitmap = DrawingCanvas.GetBitmap();
        if (bitmap == null) return;

        var paintDir = string.IsNullOrEmpty(_projectDirectory) ? "Assets/Paint" : Path.Combine(_projectDirectory, "Assets", "Paint");
        if (!Directory.Exists(paintDir)) Directory.CreateDirectory(paintDir);

        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "PNG|*.png",
            DefaultExt = ".png",
            InitialDirectory = paintDir,
            Title = "Guardar pintura"
        };
        if (dlg.ShowDialog() != true) return;
        var pngPath = dlg.FileName;

        try
        {
            using var stream = File.Create(pngPath);
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
            encoder.Save(stream);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"No se pudo guardar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        CreativeSuiteMetadata.Write(pngPath, CreativeSuiteMetadata.SourcePaint);
        DrawingCanvas.SetDirty(false);
        System.Windows.MessageBox.Show($"Guardado: {pngPath}", "Pintura guardada", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnSaveAndUseAsIcon_OnClick(object sender, RoutedEventArgs e)
    {
        if (DrawingCanvas == null) return;
        var bitmap = DrawingCanvas.GetBitmap();
        if (bitmap == null) return;

        var paintDir = string.IsNullOrEmpty(_projectDirectory) ? "Assets/Paint" : Path.Combine(_projectDirectory, "Assets", "Paint");
        if (!Directory.Exists(paintDir)) Directory.CreateDirectory(paintDir);

        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "PNG|*.png",
            DefaultExt = ".png",
            InitialDirectory = paintDir,
            Title = "Guardar pintura y usar como icono"
        };
        if (dlg.ShowDialog() != true) return;
        var pngPath = dlg.FileName;

        try
        {
            using var stream = File.Create(pngPath);
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
            encoder.Save(stream);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"No se pudo guardar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        CreativeSuiteMetadata.Write(pngPath, CreativeSuiteMetadata.SourcePaint);
        DrawingCanvas.SetDirty(false);

        string rel = !string.IsNullOrEmpty(_projectDirectory) && pngPath.StartsWith(_projectDirectory, StringComparison.OrdinalIgnoreCase)
            ? Path.GetRelativePath(_projectDirectory, pngPath)
            : Path.GetFileName(pngPath);
        RequestSetProjectIcon?.Invoke(this, rel);
        System.Windows.MessageBox.Show($"Guardado y establecido como icono del proyecto: {pngPath}", "Pintura guardada", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
