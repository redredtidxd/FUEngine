using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FUEngine.Editor;

namespace FUEngine;

public partial class NewProjectDialog : Window
{
    private string _resultName = "";
    private string _resultDescription = "";
    private string _resultPath = "";
    private string _resultIconPath = "";
    private string _resultPaletteId = "default";
    private string _resultTemplateType = "General";
    private string _resultAuthor = "";
    private string _resultCopyright = "";
    private string _resultVersion = "0.0.1";
    private int _resultTileSize = 16;
    private int _resultMapWidth = 64;
    private int _resultMapHeight = 64;
    private bool _resultInfinite = true;
    private int _resultChunkSize = 16;
    private int _resultInitialChunksW = 4;
    private int _resultInitialChunksH = 4;
    private int _resultTileHeight = 1;
    private bool _resultAutoTiling = false;
    private bool _resultLoadTemplateScripts = true;
    private string _resultCommonModules = "";
    private bool _resultPlaceholderAnimations = false;
    private int _resultInitialObjectCount = 0;
    private int _resultFps = 60;
    private bool _resultPixelPerfect = true;
    private double _resultInitialZoom = 1.0;
    private bool _resultLightShadow = false;
    private bool _resultDebugMode = false;
    private bool _resultScriptNodes = false;
    private bool _resultSaveInitialBackup = false;
    private bool _resultCreateProjectFolder = true;

    public string ProjectName => _resultName;
    public string Description => _resultDescription;
    public string ProjectPath => _resultPath;
    public string IconPath => _resultIconPath;
    public string PaletteId => _resultPaletteId;
    public string TemplateType => _resultTemplateType;
    public string Author => _resultAuthor;
    public string Copyright => _resultCopyright;
    public string Version => _resultVersion;
    public int TileSize => _resultTileSize;
    public int MapWidth => _resultMapWidth;
    public int MapHeight => _resultMapHeight;
    public bool Infinite => _resultInfinite;
    public int ChunkSize => _resultChunkSize;
    public int InitialChunksW => _resultInitialChunksW;
    public int InitialChunksH => _resultInitialChunksH;
    public int TileHeight => _resultTileHeight;
    public bool AutoTiling => _resultAutoTiling;
    public bool LoadTemplateScripts => _resultLoadTemplateScripts;
    public string CommonModules => _resultCommonModules;
    public bool PlaceholderAnimations => _resultPlaceholderAnimations;
    public int InitialObjectCount => _resultInitialObjectCount;
    public int Fps => _resultFps;
    public bool PixelPerfect => _resultPixelPerfect;
    public double InitialZoom => _resultInitialZoom;
    public bool LightShadowDefault => _resultLightShadow;
    public bool DebugMode => _resultDebugMode;
    public bool ScriptNodes => _resultScriptNodes;
    public bool SaveInitialBackup => _resultSaveInitialBackup;
    public bool CreateProjectFolderIfMissing => _resultCreateProjectFolder;

    public NewProjectDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        CmbPalette.ItemsSource = new[] { "default", "grayscale", "gameboy", "warm", "cold" };
        CmbPalette.SelectedIndex = 0;
        CmbTemplateType.ItemsSource = new[] { "Blank", "General", "Plataforma", "Top-down", "Puzzle", "TileBased", "Metroidvania", "Horror", "RPG", "Shooter", "Runner", "Sandbox", "Casual" };
        CmbTemplateType.SelectedIndex = 0;
        CmbChunkSize.ItemsSource = new[] { "16", "32", "64" };
        CmbChunkSize.SelectedIndex = 0;
        UpdateDefaultPath();
        DrawPreview();
    }

    private void TxtName_OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        UpdateDefaultPath();
    }

    private void UpdateDefaultPath()
    {
        if (TxtPath == null) return;
        var name = TxtName?.Text?.Trim() ?? "";
        var root = EngineSettings.EnsureDefaultProjectsRoot();
        var folder = NewProjectStructure.SanitizeFolderName(name);
        var uniquePath = NewProjectStructure.GetUniqueProjectPath(root, folder);
        TxtPath.Text = uniquePath;
        UpdateStructurePreview();
    }

    private void UpdateStructurePreview()
    {
        if (TxtStructurePreview == null) return;
        var path = TxtPath?.Text?.Trim() ?? "";
        var name = string.IsNullOrEmpty(path) ? "NombreProyecto" : System.IO.Path.GetFileName(path.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar));
        TxtStructurePreview.Text = $"{name}/\n  Mapa/\n  Objetos/\n  Escenas/\n  Autoguardados/\n    Mapa/\n    Objetos/\n    Escenas/\n  Assets/\n    Sprites/\n    Sonidos/\n    Animations/\n    Logos/\n    Tilesets/\n  Maps/\n  Scripts/\n  Seeds/";
    }

    private void LogoDropZone_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            var files = (string[]?)e.Data.GetData(System.Windows.DataFormats.FileDrop);
            var hasImage = files != null && files.Length > 0 && IsImageFile(files[0]);
            e.Effects = hasImage ? System.Windows.DragDropEffects.Copy : System.Windows.DragDropEffects.None;
        }
        else
            e.Effects = System.Windows.DragDropEffects.None;
        e.Handled = true;
    }

    private void LogoDropZone_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop) || TxtIconPath == null) return;
        var files = (string[]?)e.Data.GetData(System.Windows.DataFormats.FileDrop);
        if (files == null || files.Length == 0) return;
        var path = files[0];
        if (IsImageFile(path))
        {
            TxtIconPath.Text = path;
            UpdateLogoPreview();
        }
        e.Handled = true;
    }

    private static bool IsImageFile(string path)
    {
        var ext = System.IO.Path.GetExtension(path);
        return string.Equals(ext, ".png", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ext, ".jpg", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ext, ".jpeg", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ext, ".bmp", StringComparison.OrdinalIgnoreCase);
    }

    private void ChkInfinite_Changed(object sender, RoutedEventArgs e)
    {
        if (PanelSize != null && ChkInfinite != null)
            PanelSize.Visibility = ChkInfinite.IsChecked == true ? Visibility.Collapsed : Visibility.Visible;
    }

    private void BtnBrowse_OnClick(object sender, RoutedEventArgs e)
    {
        var defaultPath = "";
        try { defaultPath = EngineSettings.Load().DefaultProjectsPath ?? ""; } catch { }
        if (string.IsNullOrWhiteSpace(defaultPath) || !System.IO.Directory.Exists(defaultPath))
            defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        using var dlg = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Seleccionar carpeta del proyecto",
            UseDescriptionForTitle = true,
            SelectedPath = defaultPath
        };
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK && !string.IsNullOrEmpty(dlg.SelectedPath) && TxtPath != null)
            TxtPath.Text = dlg.SelectedPath;
    }

    private void BtnBrowseIcon_OnClick(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Imágenes (PNG, JPG)|*.png;*.jpg;*.jpeg;*.bmp|PNG|*.png|Todos|*.*",
            Title = "Seleccionar icono del proyecto"
        };
        if (dlg.ShowDialog() == true && TxtIconPath != null)
        {
            TxtIconPath.Text = dlg.FileName;
            UpdateLogoPreview();
        }
    }

    private void TxtIconPath_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        UpdateLogoPreview();
    }

    private void UpdateLogoPreview()
    {
        if (ImgLogoPreview == null) return;
        var path = TxtIconPath?.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path) || !IsImageFile(path))
        {
            ImgLogoPreview.Source = null;
            return;
        }
        try
        {
            var bmp = new System.Windows.Media.Imaging.BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
            bmp.DecodePixelWidth = 128;
            bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            ImgLogoPreview.Source = bmp;
        }
        catch
        {
            ImgLogoPreview.Source = null;
        }
    }

    private void DrawPreview()
    {
        if (PreviewMinimap == null) return;
        PreviewMinimap.Children.Clear();
        int w = (int)PreviewMinimap.Width;
        int h = (int)PreviewMinimap.Height;
        if (w <= 0 || h <= 0) return;
        var brush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 42, 50));
        var rect = new System.Windows.Shapes.Rectangle { Width = w, Height = h, Fill = brush };
        PreviewMinimap.Children.Add(rect);
    }

    private void BtnCreate_OnClick(object sender, RoutedEventArgs e)
    {
        var name = TxtName?.Text?.Trim() ?? "";
        var path = TxtPath?.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(name))
        {
            System.Windows.MessageBox.Show(this, "Indique el nombre del juego.", "Nuevo proyecto",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(path))
        {
            System.Windows.MessageBox.Show(this, "Seleccione o escriba la carpeta del proyecto.", "Nuevo proyecto",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!System.IO.Directory.Exists(path) && (ChkCreateProjectFolder?.IsChecked != true))
        {
            System.Windows.MessageBox.Show(this,
                "La carpeta no existe. Active \"Crear la carpeta del proyecto si no existe\" o seleccione una carpeta existente.",
                "Nuevo proyecto", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _resultName = name;
        _resultDescription = TxtDescription?.Text?.Trim() ?? "";
        _resultPath = path;
        _resultIconPath = TxtIconPath?.Text?.Trim() ?? "";
        _resultPaletteId = CmbPalette?.SelectedItem?.ToString() ?? "default";
        _resultTemplateType = CmbTemplateType?.SelectedItem?.ToString() ?? "General";
        _resultAuthor = TxtAuthor?.Text?.Trim() ?? "";
        _resultCopyright = TxtCopyright?.Text?.Trim() ?? "";
        _resultVersion = TxtVersion?.Text?.Trim() ?? "0.0.1";
        _resultTileSize = int.TryParse(TxtTileSize?.Text, out var ts) ? Math.Clamp(ts, 8, 128) : 16;
        _resultMapWidth = int.TryParse(TxtMapWidth?.Text, out var mw) ? Math.Max(1, mw) : 64;
        _resultMapHeight = int.TryParse(TxtMapHeight?.Text, out var mh) ? Math.Max(1, mh) : 64;
        _resultInfinite = ChkInfinite?.IsChecked == true;
        _resultChunkSize = int.TryParse(CmbChunkSize?.SelectedItem?.ToString(), out var cs) ? Math.Clamp(cs, 16, 64) : 16;
        _resultInitialChunksW = int.TryParse(TxtInitialChunksW?.Text, out var cw) ? Math.Max(1, Math.Min(32, cw)) : 4;
        _resultInitialChunksH = int.TryParse(TxtInitialChunksH?.Text, out var ch) ? Math.Max(1, Math.Min(32, ch)) : 4;
        _resultTileHeight = int.TryParse(TxtTileHeight?.Text, out var th) ? Math.Max(1, th) : 1;
        _resultAutoTiling = ChkAutoTiling?.IsChecked == true;
        _resultLoadTemplateScripts = ChkLoadTemplateScripts?.IsChecked == true;
        _resultCommonModules = TxtCommonModules?.Text?.Trim() ?? "";
        _resultPlaceholderAnimations = ChkPlaceholderAnimations?.IsChecked == true;
        _resultInitialObjectCount = int.TryParse(TxtInitialObjectCount?.Text, out var noc) ? Math.Max(0, noc) : 0;
        _resultFps = int.TryParse(TxtFps?.Text, out var fps) ? Math.Clamp(fps, 15, 240) : 60;
        _resultPixelPerfect = ChkPixelPerfect?.IsChecked == true;
        _resultInitialZoom = double.TryParse(TxtInitialZoom?.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var zoom) && zoom > 0 ? zoom : 1.0;
        _resultLightShadow = ChkLightShadow?.IsChecked == true;
        _resultDebugMode = ChkDebugMode?.IsChecked == true;
        _resultScriptNodes = ChkScriptNodes?.IsChecked == true;
        _resultSaveInitialBackup = ChkSaveInitialBackup?.IsChecked == true;
        _resultCreateProjectFolder = ChkCreateProjectFolder?.IsChecked == true;

        DialogResult = true;
        Close();
    }
}
