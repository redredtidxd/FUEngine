using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using FUEngine.Editor;

namespace FUEngine;

public partial class ImageResizeDialog : Window
{
    /// <summary>Ruta del archivo escrito (original al sobrescribir o destino al guardar copia).</summary>
    public string? LastWrittenPath { get; private set; }

    private readonly string _sourcePath;
    private readonly int _tileSize;
    private double _aspect = 1;
    private bool _syncingSize;
    private int _baseW, _baseH;

    /// <param name="projectDirectory">Para leer TileSize del manifiesto; puede ser vacío.</param>
    public ImageResizeDialog(string imagePath, string? projectDirectory)
    {
        InitializeComponent();
        _sourcePath = imagePath ?? "";

        _tileSize = 16;
        try
        {
            var manifest = ProjectManifestPaths.GetCanonicalManifestPath(projectDirectory);
            if (!string.IsNullOrEmpty(manifest) && File.Exists(manifest))
            {
                var p = ProjectSerialization.Load(manifest);
                if (p.TileSize > 0) _tileSize = p.TileSize;
            }
        }
        catch { /* ignore */ }

        TxtHint.Text = $"Reescalado «vecino más cercano» (sin blur). Tile del proyecto: {_tileSize} px. Máx. {ImageNearestNeighborResize.MaxDimension} px por lado.";

        if (!LoadImageSize())
        {
            TxtInfo.Text = "No se pudo leer la imagen.";
            return;
        }

        _aspect = _baseH > 0 ? (double)_baseW / _baseH : 1;
        TxtInfo.Text = $"{Path.GetFileName(_sourcePath)} — original {_baseW}×{_baseH} px";
    }

    private bool LoadImageSize()
    {
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(Path.GetFullPath(_sourcePath), UriKind.Absolute);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            _baseW = bmp.PixelWidth;
            _baseH = bmp.PixelHeight;
            TxtWidth.Text = _baseW.ToString();
            TxtHeight.Text = _baseH.ToString();
            return _baseW > 0 && _baseH > 0;
        }
        catch
        {
            return false;
        }
    }

    private void TxtSize_OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (!_syncingSize && ChkAspect.IsChecked == true)
        {
            if (sender == TxtWidth && int.TryParse(TxtWidth.Text, out var w) && w > 0 && _aspect > 0)
            {
                _syncingSize = true;
                TxtHeight.Text = Math.Max(1, (int)Math.Round(w / _aspect)).ToString();
                _syncingSize = false;
            }
            else if (sender == TxtHeight && int.TryParse(TxtHeight.Text, out var h) && h > 0)
            {
                _syncingSize = true;
                TxtWidth.Text = Math.Max(1, (int)Math.Round(h * _aspect)).ToString();
                _syncingSize = false;
            }
        }
    }

    private void ChkAspect_OnChanged(object sender, RoutedEventArgs e)
    {
        if (ChkAspect.IsChecked == true && int.TryParse(TxtWidth.Text, out var w) && w > 0 && _aspect > 0)
        {
            _syncingSize = true;
            TxtHeight.Text = Math.Max(1, (int)Math.Round(w / _aspect)).ToString();
            _syncingSize = false;
        }
    }

    private void BtnScale_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button b || b.Tag is not string tag) return;
        if (!double.TryParse(tag, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var factor)) return;
        if (!int.TryParse(TxtWidth.Text, out var cw) || cw <= 0) cw = _baseW;
        if (!int.TryParse(TxtHeight.Text, out var ch) || ch <= 0) ch = _baseH;
        var nw = Math.Max(1, (int)Math.Round(cw * factor));
        var nh = Math.Max(1, (int)Math.Round(ch * factor));
        nw = Math.Min(nw, ImageNearestNeighborResize.MaxDimension);
        nh = Math.Min(nh, ImageNearestNeighborResize.MaxDimension);
        _syncingSize = true;
        TxtWidth.Text = nw.ToString();
        TxtHeight.Text = nh.ToString();
        _syncingSize = false;
    }

    private void BtnSnapTile_OnClick(object sender, RoutedEventArgs e)
    {
        var ts = Math.Max(1, _tileSize);
        if (!int.TryParse(TxtWidth.Text, out var w) || w <= 0) w = _baseW;
        if (!int.TryParse(TxtHeight.Text, out var h) || h <= 0) h = _baseH;
        w = Math.Max(ts, ((w + ts / 2) / ts) * ts);
        h = Math.Max(ts, ((h + ts / 2) / ts) * ts);
        w = Math.Min(w, ImageNearestNeighborResize.MaxDimension);
        h = Math.Min(h, ImageNearestNeighborResize.MaxDimension);
        _syncingSize = true;
        TxtWidth.Text = w.ToString();
        TxtHeight.Text = h.ToString();
        _syncingSize = false;
    }

    private bool TryGetDimensions(out int w, out int h)
    {
        w = h = 0;
        return int.TryParse(TxtWidth.Text?.Trim(), out w) && int.TryParse(TxtHeight.Text?.Trim(), out h)
               && w > 0 && h > 0;
    }

    private void ApplySnapTile(ref int w, ref int h)
    {
        if (ChkSnapTile.IsChecked != true) return;
        var ts = Math.Max(1, _tileSize);
        w = Math.Max(ts, ((w + ts / 2) / ts) * ts);
        h = Math.Max(ts, ((h + ts / 2) / ts) * ts);
        w = Math.Min(w, ImageNearestNeighborResize.MaxDimension);
        h = Math.Min(h, ImageNearestNeighborResize.MaxDimension);
    }

    private void BtnOverwrite_OnClick(object sender, RoutedEventArgs e)
    {
        if (!TryGetDimensions(out var w, out var h)) { System.Windows.MessageBox.Show(this, "Introduce ancho y alto válidos.", "Reescalar", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        ApplySnapTile(ref w, ref h);
        if (w == _baseW && h == _baseH) { System.Windows.MessageBox.Show(this, "El tamaño coincide con el original.", "Reescalar", MessageBoxButton.OK, MessageBoxImage.Information); return; }

        if (System.Windows.MessageBox.Show(this,
                $"Se sobrescribirá el archivo con una imagen {w}×{h} px.\n¿Continuar?",
                "Confirmar",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        if (!ImageNearestNeighborResize.TryResizeToFile(_sourcePath, _sourcePath, w, h, out var err))
        {
            System.Windows.MessageBox.Show(this, err ?? "Error", "Reescalar", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        LastWrittenPath = _sourcePath;
        DialogResult = true;
        Close();
    }

    private void BtnSaveCopy_OnClick(object sender, RoutedEventArgs e)
    {
        if (!TryGetDimensions(out var w, out var h)) { System.Windows.MessageBox.Show(this, "Introduce ancho y alto válidos.", "Reescalar", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        ApplySnapTile(ref w, ref h);

        var dir = Path.GetDirectoryName(_sourcePath) ?? "";
        var name = Path.GetFileNameWithoutExtension(_sourcePath);
        var ext = Path.GetExtension(_sourcePath);
        if (string.IsNullOrEmpty(ext)) ext = ".png";
        var suggested = Path.Combine(dir, $"{name}_scaled{ext}");

        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Guardar copia reescalada",
            Filter = "PNG|*.png|JPEG|*.jpg;*.jpeg|Todos|*.*",
            FileName = Path.GetFileName(suggested),
            InitialDirectory = Directory.Exists(dir) ? dir : null
        };
        if (dlg.ShowDialog(this) != true || string.IsNullOrEmpty(dlg.FileName)) return;

        if (!ImageNearestNeighborResize.TryResizeToFile(_sourcePath, dlg.FileName, w, h, out var err))
        {
            System.Windows.MessageBox.Show(this, err ?? "Error", "Reescalar", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        LastWrittenPath = dlg.FileName;
        DialogResult = true;
        Close();
    }

    private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
