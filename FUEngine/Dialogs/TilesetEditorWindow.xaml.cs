using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using FUEngine.Core;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;
using WpfSaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace FUEngine;

public partial class TilesetEditorWindow : Window
{
    private Tileset? _tileset;
    private int _selectedTileId = -1;
    private string? _projectDirectory;
    private string? _jsonAbsolutePath;

    public TilesetEditorWindow()
    {
        InitializeComponent();
        CmbMaterial.ItemsSource = TileMaterial.All;
        if (CmbMaterial.Items.Count > 0) CmbMaterial.SelectedIndex = 0;
    }

    /// <summary>Modo simple: tileset en memoria (sin rutas de guardado).</summary>
    public void SetTileset(Tileset tileset)
    {
        _tileset = tileset;
        _projectDirectory = null;
        _jsonAbsolutePath = null;
        _selectedTileId = -1;
        SyncCellInputs();
        RefreshGrid();
        TxtTilesetInfo.Text = $"{tileset.Name} · {tileset.TileWidth}×{tileset.TileHeight} px";
        UpdatePanelVisibility();
    }

    /// <summary>Abre o prepara edición con proyecto en disco (rutas relativas y guardado .tileset.json).</summary>
    public void SetTilesetForProject(Tileset tileset, string projectDirectory, string? jsonAbsolutePath)
    {
        _tileset = tileset;
        _projectDirectory = string.IsNullOrWhiteSpace(projectDirectory) ? null : projectDirectory.TrimEnd('\\', '/');
        _jsonAbsolutePath = string.IsNullOrWhiteSpace(jsonAbsolutePath) ? null : jsonAbsolutePath;
        _selectedTileId = -1;
        SyncCellInputs();
        RefreshGrid();
        var rel = _jsonAbsolutePath != null && _projectDirectory != null
            ? Path.GetRelativePath(_projectDirectory, _jsonAbsolutePath).Replace('\\', '/')
            : tileset.TexturePath;
        TxtTilesetInfo.Text = $"{tileset.Name} · {tileset.TileWidth}×{tileset.TileHeight} px · {rel}";
        UpdatePanelVisibility();
    }

    private void SyncCellInputs()
    {
        if (_tileset == null) return;
        TxtCellW.Text = _tileset.TileWidth.ToString();
        TxtCellH.Text = _tileset.TileHeight.ToString();
    }

    private void BtnApplyCellSize_OnClick(object sender, RoutedEventArgs e)
    {
        if (_tileset == null) return;
        if (int.TryParse(TxtCellW.Text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var w) && w > 0)
            _tileset.TileWidth = w;
        if (int.TryParse(TxtCellH.Text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var h) && h > 0)
            _tileset.TileHeight = h;
        RefreshGrid();
        TxtTilesetInfo.Text = $"{_tileset.Name} · {_tileset.TileWidth}×{_tileset.TileHeight} px";
    }

    private void RefreshGrid()
    {
        if (_tileset == null) return;
        int count = ComputeVisibleTileSlotCount();
        var list = Enumerable.Range(0, count).Select(i => _tileset.GetOrCreateTile(i)).ToList();
        TilesGrid.ItemsSource = list;
    }

    private int ComputeVisibleTileSlotCount()
    {
        if (_tileset == null) return 32;
        int tw = Math.Max(1, _tileset.TileWidth);
        int th = Math.Max(1, _tileset.TileHeight);
        int maxDefId = _tileset.EnumerateTiles().Select(t => t.id).DefaultIfEmpty(-1).Max();
        int fromDefs = maxDefId < 0 ? 0 : maxDefId + 1;
        if (string.IsNullOrWhiteSpace(_tileset.TexturePath) || string.IsNullOrWhiteSpace(_projectDirectory))
            return Math.Max(32, fromDefs);
        var full = Path.Combine(_projectDirectory, _tileset.TexturePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(full)) return Math.Max(32, fromDefs);
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new System.Uri(System.IO.Path.GetFullPath(full));
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            int cols = Math.Max(1, bmp.PixelWidth / tw);
            int rows = Math.Max(1, bmp.PixelHeight / th);
            int fromImage = cols * rows;
            return Math.Max(fromImage, fromDefs);
        }
        catch
        {
            return Math.Max(32, fromDefs);
        }
    }

    private void TileSlot_OnClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement el && el.DataContext is Tile tile)
        {
            _selectedTileId = tile.Id;
            TxtSelectedId.Text = $"#{tile.Id}";
            ChkCollision.IsChecked = tile.Collision;
            ChkLightBlock.IsChecked = tile.LightBlock;
            TxtFriction.Text = tile.Friction.ToString("F2");
            TxtTags.Text = string.Join(", ", tile.Tags ?? new List<string>());
            TxtAnimationId.Text = tile.AnimationId ?? "";
            TxtAnimationSpeed.Text = tile.AnimationSpeed.ToString("F1");
            if (!string.IsNullOrEmpty(tile.Material))
            {
                var idx = Array.IndexOf(TileMaterial.All, tile.Material);
                CmbMaterial.SelectedIndex = idx >= 0 ? idx : 0;
            }
            else
                CmbMaterial.SelectedIndex = 0;
            UpdatePanelVisibility();
        }
    }

    private void UpdatePanelVisibility()
    {
        BtnApply.IsEnabled = _selectedTileId >= 0 && _tileset != null;
        BtnSaveJson.IsEnabled = _tileset != null;
    }

    private void BtnApply_OnClick(object sender, RoutedEventArgs e)
    {
        if (_tileset == null || _selectedTileId < 0) return;
        var tile = _tileset.GetOrCreateTile(_selectedTileId);
        tile.Collision = ChkCollision.IsChecked == true;
        tile.LightBlock = ChkLightBlock.IsChecked == true;
        if (float.TryParse(TxtFriction.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var f))
            tile.Friction = Math.Clamp(f, 0f, 1f);
        tile.Material = CmbMaterial.SelectedItem as string ?? "";
        var tagsStr = TxtTags.Text?.Trim() ?? "";
        tile.Tags = string.IsNullOrEmpty(tagsStr) ? new List<string>() : tagsStr.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        tile.AnimationId = string.IsNullOrWhiteSpace(TxtAnimationId.Text) ? null : TxtAnimationId.Text.Trim();
        if (float.TryParse(TxtAnimationSpeed.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var sp))
            tile.AnimationSpeed = Math.Max(0, sp);
        RefreshGrid();
    }

    private void BtnOpenJson_OnClick(object sender, RoutedEventArgs e)
    {
        var dlg = new WpfOpenFileDialog
        {
            Title = "Abrir tileset",
            Filter = "Tileset JSON (*.tileset.json)|*.tileset.json|JSON (*.json)|*.json|Todos (*.*)|*.*"
        };
        if (!string.IsNullOrWhiteSpace(_projectDirectory))
            dlg.InitialDirectory = _projectDirectory;
        if (dlg.ShowDialog() != true) return;
        var path = dlg.FileName;
        var loaded = TilesetPersistence.Load(path);
        if (loaded == null)
        {
            System.Windows.MessageBox.Show("No se pudo cargar el archivo.", "Tileset", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var dir = !string.IsNullOrWhiteSpace(_projectDirectory)
            ? _projectDirectory!
            : (Path.GetDirectoryName(path) ?? "");
        SetTilesetForProject(loaded, dir, path);
    }

    private void BtnSaveJson_OnClick(object sender, RoutedEventArgs e)
    {
        if (_tileset == null) return;
        string path = _jsonAbsolutePath ?? "";
        if (string.IsNullOrEmpty(path))
        {
            var dlg = new WpfSaveFileDialog
            {
                Title = "Guardar tileset",
                Filter = "Tileset JSON (*.tileset.json)|*.tileset.json",
                DefaultExt = ".tileset.json"
            };
            if (!string.IsNullOrWhiteSpace(_projectDirectory))
                dlg.InitialDirectory = Path.Combine(_projectDirectory, "Assets", "Tilesets");
            if (dlg.ShowDialog() != true) return;
            path = dlg.FileName;
            _jsonAbsolutePath = path;
        }
        try
        {
            TilesetPersistence.Save(path, _tileset);
            System.Windows.MessageBox.Show("Guardado.", "Tileset", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Error al guardar", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnClose_OnClick(object sender, RoutedEventArgs e) => Close();

    public Tileset? GetTileset() => _tileset;
}
