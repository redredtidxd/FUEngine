using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FUEngine.Core;
using Wpf = System.Windows.Controls;

namespace FUEngine;

/// <summary>Ventana mínima para marcar colisión de un tile en el JSON del tileset (molde).</summary>
public sealed class TileCollisionMiniDialog : System.Windows.Window
{
    private readonly string _absoluteTilesetPath;
    private readonly int _tileId;
    private readonly string _projectDir;
    private readonly Tileset _tileset;
    private readonly Wpf.Border _preview = new()
    {
        Width = 160,
        Height = 160,
        Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x21, 0x26, 0x2d))
    };

    public TileCollisionMiniDialog(string absoluteTilesetPath, int tileId, string projectDir)
    {
        _absoluteTilesetPath = absoluteTilesetPath;
        _tileId = tileId;
        _projectDir = projectDir;
        Title = $"Colisión · tile #{tileId}";
        Width = 420;
        Height = 340;
        Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x16, 0x1b, 0x22));
        var loaded = TilesetPersistence.Load(absoluteTilesetPath);
        if (loaded == null)
        {
            System.Windows.MessageBox.Show(this, "No se pudo cargar el tileset.", "Tile", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            _tileset = new Tileset();
            Content = new Wpf.TextBlock { Text = "Error al cargar.", Margin = new System.Windows.Thickness(12), Foreground = System.Windows.Media.Brushes.White };
            return;
        }
        _tileset = loaded;
        BuildUi();
    }

    private void BuildUi()
    {
        var root = new Wpf.StackPanel { Margin = new System.Windows.Thickness(16) };
        var def = _tileset.GetOrCreateTile(_tileId);
        var info = new Wpf.TextBlock
        {
            Text = $"Archivo: {Path.GetFileName(_absoluteTilesetPath)}  ·  Colisión actual: {(def.Collision ? "sí" : "no")}",
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xe6, 0xed, 0xf3)),
            TextWrapping = System.Windows.TextWrapping.Wrap,
            Margin = new System.Windows.Thickness(0, 0, 0, 12)
        };
        root.Children.Add(info);
        root.Children.Add(_preview);
        TryLoadPreview();
        var row = new Wpf.StackPanel { Orientation = Wpf.Orientation.Horizontal, Margin = new System.Windows.Thickness(0, 12, 0, 0) };
        var btnFull = new Wpf.Button { Content = "AABB (todo el tile)", Margin = new System.Windows.Thickness(0, 0, 8, 0), Padding = new System.Windows.Thickness(12, 6, 12, 6) };
        btnFull.Click += (_, _) =>
        {
            var t = _tileset.GetOrCreateTile(_tileId);
            t.Collision = true;
            info.Text = $"Archivo: {Path.GetFileName(_absoluteTilesetPath)}  ·  Colisión: sí (AABB completo)";
        };
        var btnNone = new Wpf.Button { Content = "Sin colisión", Margin = new System.Windows.Thickness(0, 0, 8, 0), Padding = new System.Windows.Thickness(12, 6, 12, 6) };
        btnNone.Click += (_, _) =>
        {
            var t = _tileset.GetOrCreateTile(_tileId);
            t.Collision = false;
            info.Text = $"Archivo: {Path.GetFileName(_absoluteTilesetPath)}  ·  Colisión: no";
        };
        row.Children.Add(btnFull);
        row.Children.Add(btnNone);
        root.Children.Add(row);
        var btnSave = new Wpf.Button
        {
            Content = "Guardar y cerrar",
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
            Margin = new System.Windows.Thickness(0, 16, 0, 0),
            Padding = new System.Windows.Thickness(16, 8, 16, 8),
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x23, 0x86, 0x36)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new System.Windows.Thickness(0)
        };
        btnSave.Click += (_, _) =>
        {
            try
            {
                TilesetPersistence.Save(_absoluteTilesetPath, _tileset);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(this, ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        };
        root.Children.Add(btnSave);
        Content = root;
    }

    private void TryLoadPreview()
    {
        var tex = (_tileset.TexturePath ?? "").Replace('\\', '/').Trim();
        if (string.IsNullOrEmpty(tex)) return;
        var full = Path.Combine(_projectDir, tex.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(full)) return;
        try
        {
            var rgba = TileImageLoader.LoadAtlasTileToRgba(full, Math.Max(1, _tileset.TileWidth), Math.Max(1, _tileset.TileHeight), _tileId, 160, 160);
            if (rgba == null || rgba.Length < 160 * 160 * 4) return;
            var wb = new WriteableBitmap(160, 160, 96, 96, PixelFormats.Bgra32, null);
            var bgra = new byte[rgba.Length];
            for (int i = 0; i < rgba.Length; i += 4)
            {
                bgra[i] = rgba[i + 2];
                bgra[i + 1] = rgba[i + 1];
                bgra[i + 2] = rgba[i];
                bgra[i + 3] = rgba[i + 3];
            }
            wb.WritePixels(new System.Windows.Int32Rect(0, 0, 160, 160), bgra, 160 * 4, 0);
            wb.Freeze();
            _preview.Child = new Wpf.Image { Source = wb, Stretch = System.Windows.Media.Stretch.Uniform };
        }
        catch { /* ignore */ }
    }
}
