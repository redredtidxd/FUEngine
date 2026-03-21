using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using FUEngine.Core;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace FUEngine;

public partial class TilesTabContent : System.Windows.Controls.UserControl
{
    public event EventHandler<int?>? TileSelected;
    private ProjectInfo? _project;

    public TilesTabContent()
    {
        InitializeComponent();
        Loaded += (_, _) => PopulatePlaceholderTiles();
    }

    public void SetProject(ProjectInfo project) => _project = project;

    private void PopulatePlaceholderTiles()
    {
        if (TilesList == null) return;
        TilesList.ItemsSource = Enumerable.Range(0, 16).ToList();
    }

    private void TilesList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var id = TilesList?.SelectedItem is int i ? (int?)i : null;
        TileSelected?.Invoke(this, id);
    }

    private void BtnOpenTilesetEditor_OnClick(object sender, RoutedEventArgs e)
    {
        var owner = Window.GetWindow(this);
        var projDir = _project?.ProjectDirectory;
        if (!string.IsNullOrWhiteSpace(projDir))
        {
            var open = new WpfOpenFileDialog
            {
                Title = "Abrir tileset o atlas PNG",
                Filter = "Tileset JSON (*.tileset.json)|*.tileset.json|JSON (*.json)|*.json|Imagen PNG|*.png|Todos|*.*"
            };
            if (Directory.Exists(Path.Combine(projDir, "Assets", "Tilesets")))
                open.InitialDirectory = Path.Combine(projDir, "Assets", "Tilesets");
            else
                open.InitialDirectory = projDir;
            if (open.ShowDialog() == true)
            {
                var path = open.FileName;
                if (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".tileset.json", StringComparison.OrdinalIgnoreCase))
                {
                    var loaded = TilesetPersistence.Load(path);
                    if (loaded != null)
                    {
                        var w = new TilesetEditorWindow { Owner = owner };
                        w.SetTilesetForProject(loaded, projDir, path);
                        w.ShowDialog();
                    }
                    else
                        System.Windows.MessageBox.Show(owner, "No se pudo cargar el JSON.", "Tileset", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    var rel = Path.GetRelativePath(projDir, path).Replace('\\', '/');
                    var stem = Path.GetFileNameWithoutExtension(path);
                    var jsonPath = Path.Combine(Path.GetDirectoryName(path)!, stem + ".tileset.json");
                    var tileset = new Tileset
                    {
                        Id = stem,
                        Name = stem,
                        TexturePath = rel,
                        TileWidth = Math.Max(8, _project?.TileSize ?? 16),
                        TileHeight = Math.Max(8, _project?.TileSize ?? 16)
                    };
                    try
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.UriSource = new System.Uri(System.IO.Path.GetFullPath(path));
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.EndInit();
                        bmp.Freeze();
                    }
                    catch { /* ignorar */ }
                    var w = new TilesetEditorWindow { Owner = owner };
                    w.SetTilesetForProject(tileset, projDir, File.Exists(jsonPath) ? jsonPath : null);
                    w.ShowDialog();
                }
                return;
            }
        }
        var fallback = new Tileset { Id = "default", Name = "Tileset principal", TileWidth = 16, TileHeight = 16 };
        var win = new TilesetEditorWindow { Owner = owner };
        win.SetTileset(fallback);
        win.ShowDialog();
    }
}
