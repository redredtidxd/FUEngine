using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace FUEngine;

public partial class CollisionsEditorTabContent : System.Windows.Controls.UserControl
{
    private string? _currentAssetPath;
    private string _projectDirectory = "";
    private bool _updatingList;

    public CollisionsEditorTabContent()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (ShapesCanvas != null)
            {
                ShapesCanvas.SelectionChanged += (_, __) => SyncListSelectionFromCanvas();
                ShapesCanvas.ShapesChanged += (_, __) => { RefreshShapesList(); SetDirty(true); };
            }
            if (CmbLayer != null && CmbLayer.Items.Count > 0)
                CmbLayer.SelectedIndex = 0;
        };
    }

    public event EventHandler<bool>? DirtyChanged;

    public string? CurrentAssetPath => _currentAssetPath;

    public void SetProjectDirectory(string projectDirectory) => _projectDirectory = projectDirectory ?? "";

    public void LoadAsset(string fullPath)
    {
        _currentAssetPath = fullPath;
        if (TxtPath != null)
            TxtPath.Text = "Editor de colisiones — " + (string.IsNullOrEmpty(fullPath) ? "Sin archivo" : Path.GetFileName(fullPath));

        if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
        {
            ShapesCanvas?.LoadImage(null);
            ShapesCanvas?.SetShapes(null);
            RefreshShapesList();
            return;
        }

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            ShapesCanvas?.LoadImage(bitmap);

            var tiledataPath = TileDataFile.GetTileDataPath(fullPath);
            var dto = TileDataFile.Load(tiledataPath);
            var shapes = dto?.CollisionShapes;
            ShapesCanvas?.SetShapes(shapes ?? new List<CollisionShapeDto>());

            if (CmbLayer != null && shapes != null && shapes.Count > 0 && !string.IsNullOrEmpty(shapes[0].Layer))
            {
                for (int i = 0; i < CmbLayer.Items.Count; i++)
                {
                    if (CmbLayer.Items[i] is ComboBoxItem item && (item.Tag as string) == shapes[0].Layer)
                    {
                        CmbLayer.SelectedIndex = i;
                        break;
                    }
                }
            }
            else if (CmbLayer != null && CmbLayer.Items.Count > 0)
                CmbLayer.SelectedIndex = 0;

            if (ShapesCanvas != null)
                ShapesCanvas.CurrentLayer = (CmbLayer?.SelectedItem as ComboBoxItem)?.Tag as string ?? "Solid";

            RefreshShapesList();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"No se pudo cargar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOpen_OnClick(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Imágenes|*.png;*.jpg;*.jpeg;*.bmp;*.gif|Todos|*.*",
            Title = "Abrir imagen para editar colisiones"
        };
        if (!string.IsNullOrEmpty(_projectDirectory) && Directory.Exists(_projectDirectory))
            dlg.InitialDirectory = _projectDirectory;
        if (dlg.ShowDialog() != true) return;
        LoadAsset(dlg.FileName);
        SetDirty(false);
    }

    private void CmbLayer_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ShapesCanvas == null || CmbLayer?.SelectedItem is not ComboBoxItem item) return;
        var layer = item.Tag as string ?? "Solid";
        ShapesCanvas.CurrentLayer = layer;
        ShapesCanvas.SetLayerOfSelected(layer);
    }

    private void RefreshShapesList()
    {
        if (ShapesList == null || ShapesCanvas == null) return;
        _updatingList = true;
        var sel = ShapesList.SelectedIndex;
        ShapesList.Items.Clear();
        foreach (var s in ShapesCanvas.GetShapes())
        {
            var text = s.Type switch
            {
                "Box" => $"Box {s.Layer} ({s.X:F0},{s.Y:F0}) {s.Width:F0}×{s.Height:F0}",
                "Circle" => $"Circle {s.Layer} ({s.CenterX:F0},{s.CenterY:F0}) r={s.Radius:F0}",
                "Polygon" => $"Polygon {s.Layer} ({(s.Points?.Count ?? 0) / 2} pts)",
                "Capsule" => $"Capsule {s.Layer}",
                _ => $"{s.Type} {s.Layer}"
            };
            ShapesList.Items.Add(text);
        }
        if (sel >= 0 && sel < ShapesList.Items.Count)
            ShapesList.SelectedIndex = sel;
        _updatingList = false;
    }

    private void SyncListSelectionFromCanvas()
    {
        if (_updatingList || ShapesList == null || ShapesCanvas == null) return;
        var idx = ShapesCanvas.SelectedIndex;
        if (idx >= 0 && idx < ShapesList.Items.Count)
            ShapesList.SelectedIndex = idx;
        else
            ShapesList.SelectedIndex = -1;
    }

    private void ShapesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingList || ShapesCanvas == null || ShapesList == null) return;
        var idx = ShapesList.SelectedIndex;
        ShapesCanvas.SelectIndex(idx);
        if (idx >= 0 && ShapesCanvas.GetShapes().Count > idx)
        {
            var shape = ShapesCanvas.GetShapes()[idx];
            var layer = shape.Layer ?? "Solid";
            for (int i = 0; i < CmbLayer?.Items.Count; i++)
            {
                if (CmbLayer.Items[i] is ComboBoxItem item && (item.Tag as string) == layer)
                {
                    _updatingList = true;
                    CmbLayer.SelectedIndex = i;
                    _updatingList = false;
                    break;
                }
            }
        }
    }

    private void BtnDeleteShape_OnClick(object sender, RoutedEventArgs e) => ShapesCanvas?.DeleteSelected();

    private void BtnSave_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentAssetPath) || ShapesCanvas == null) return;
        var tiledataPath = TileDataFile.GetTileDataPath(_currentAssetPath);
        var dto = TileDataFile.Load(tiledataPath) ?? new TileDataDto();
        dto.CollisionShapes = ShapesCanvas.GetShapes().ToList();
        TileDataFile.Save(tiledataPath, dto);
        SetDirty(false);
    }

    private void SetDirty(bool dirty) => DirtyChanged?.Invoke(this, dirty);
}
