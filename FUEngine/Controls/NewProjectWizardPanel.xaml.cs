using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FUEngine.Editor;
using WpfColor = System.Windows.Media.Color;
using WpfLine = System.Windows.Shapes.Line;

namespace FUEngine;

/// <summary>Asistente de nuevo proyecto (overlay del Hub o del editor).</summary>
public partial class NewProjectWizardPanel : System.Windows.Controls.UserControl
{
    /// <summary>El usuario pulsó Crear y los datos están listos (leer propiedades públicas).</summary>
    public event EventHandler? CreateCommitted;

    /// <summary>Cancelar (botón o equivalente).</summary>
    public event EventHandler? CancelRequested;

    private string _resultName = "";
    private string _resultDescription = "";
    private string _resultPath = "";
    private string _resultIconPath = "";
    private int _resultTileSize = 16;
    private int _resultMapWidth = 64;
    private int _resultMapHeight = 64;
    private int _resultChunkSize = 16;
    private int _resultInitialChunksW = NewProjectStructure.DefaultMapChunksPerSide;
    private int _resultInitialChunksH = NewProjectStructure.DefaultMapChunksPerSide;
    private bool _resultAutoTiling = false;
    private bool _resultLoadTemplateScripts = true;
    private string _resultCommonModules = "";
    private bool _resultPlaceholderAnimations = false;
    private int _resultInitialObjectCount = 0;
    private int _resultFps = 60;
    private bool _resultPixelPerfect = true;
    private double _resultInitialZoom = 1.0;
    private bool _resultLightShadow = false;
    private bool _resultDebugMode = true;
    private bool _resultScriptNodes = false;
    private bool _resultSaveInitialBackup = false;
    private bool _resultCreateProjectFolder = true;
    private bool _resultGenerateStandardHierarchy = true;
    private string _resultDefaultFirstSceneBackgroundColor = "#FFFFFF";
    private string _resultAuthor = "";
    private string _resultCopyright = "";
    private string _resultVersion = "0.0.1";

    public string ProjectName => _resultName;
    public string Description => _resultDescription;
    public string ProjectPath => _resultPath;
    public string IconPath => _resultIconPath;
    /// <summary>Paleta del proyecto: toma el valor por defecto del motor (pestaña Motor), no del asistente.</summary>
    public string PaletteId => EngineSettings.Load().DefaultPaletteId ?? "default";
    public string TemplateType => "Blank";
    public string Author => _resultAuthor;
    public string Copyright => _resultCopyright;
    public string Version => _resultVersion;
    public int TileSize => _resultTileSize;
    public int MapWidth => _resultMapWidth;
    public int MapHeight => _resultMapHeight;
    public bool Infinite => false;
    public int ChunkSize => _resultChunkSize;
    public int InitialChunksW => _resultInitialChunksW;
    public int InitialChunksH => _resultInitialChunksH;
    public int TileHeight => 1;
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
    public bool GenerateStandardHierarchy => _resultGenerateStandardHierarchy;
    public string DefaultFirstSceneBackgroundColor => _resultDefaultFirstSceneBackgroundColor;

    public NewProjectWizardPanel()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private Window? HostWindow => Window.GetWindow(this);

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        CmbChunkSize.ItemsSource = new[] { "16", "32", "64" };
        CmbChunkSize.SelectedIndex = 0;
        try
        {
            var s = EngineSettings.Load();
            if (TxtDefaultGameBg != null && !string.IsNullOrWhiteSpace(s.DefaultSceneBackgroundColor))
                TxtDefaultGameBg.Text = s.DefaultSceneBackgroundColor.Trim();
            if (TxtTileSize != null && s.DefaultTileSize >= 8 && s.DefaultTileSize <= 128)
                TxtTileSize.Text = s.DefaultTileSize.ToString();
            if (CmbChunkSize != null && (s.DefaultChunkSize == 16 || s.DefaultChunkSize == 32 || s.DefaultChunkSize == 64))
                CmbChunkSize.SelectedItem = s.DefaultChunkSize.ToString();
            if (ChkDebugMode != null)
                ChkDebugMode.IsChecked = s.DefaultNewProjectDebugMode;
        }
        catch { /* use XAML default */ }
        UpdateDefaultPath();
        SyncDerivedMapSizeFromChunkDefaults();
        DrawPreview();
        UpdateStructurePreview();
        UpdateCreateEnabled();
    }

    private void SyncDerivedMapSizeFromChunkDefaults()
    {
        var cs = int.TryParse(CmbChunkSize?.SelectedItem?.ToString(), out var c) ? Math.Clamp(c, 16, 64) : 16;
        var n = NewProjectStructure.DefaultMapChunksPerSide;
        _resultInitialChunksW = n;
        _resultInitialChunksH = n;
        _resultMapWidth = n * cs;
        _resultMapHeight = n * cs;
        _resultChunkSize = cs;
    }

    private void MapTileSettings_OnChanged(object sender, TextChangedEventArgs e)
    {
        DrawPreview();
    }

    private void CmbChunkSize_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SyncDerivedMapSizeFromChunkDefaults();
        DrawPreview();
    }

    private void BtnCancel_OnClick(object sender, RoutedEventArgs e) => CancelRequested?.Invoke(this, EventArgs.Empty);

    private void TxtName_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateDefaultPath();
        UpdateCreateEnabled();
    }

    private void TxtPath_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateStructurePreview();
        UpdateCreateEnabled();
    }

    private void ChkCreateProjectFolder_Changed(object sender, RoutedEventArgs e) => UpdateCreateEnabled();

    private void ChkGenerateStandardHierarchy_Changed(object sender, RoutedEventArgs e) => UpdateStructurePreview();

    private void TxtDefaultGameBg_OnTextChanged(object sender, TextChangedEventArgs e) => UpdateCreateEnabled();

    private void UpdateCreateEnabled()
    {
        if (BtnCreate == null || TxtName == null || TxtPath == null) return;
        var name = TxtName.Text?.Trim() ?? "";
        var path = TxtPath.Text?.Trim() ?? "";
        var createFolder = ChkCreateProjectFolder?.IsChecked == true;
        var nameOk = NewProjectStructure.IsValidProjectFolderName(name);
        var pathOk = NewProjectStructure.TryValidateProjectOutputPath(path, createFolder, out _);
        var bg = TxtDefaultGameBg?.Text?.Trim() ?? "";
        var bgOk = string.IsNullOrEmpty(bg) || IsValidHexColor(bg);
        BtnCreate.IsEnabled = nameOk && pathOk && bgOk;
    }

    private static bool IsValidHexColor(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        return Regex.IsMatch(s.Trim(), @"^#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$");
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
        UpdateCreateEnabled();
    }

    private void UpdateStructurePreview()
    {
        if (TxtStructurePreview == null) return;
        var path = TxtPath?.Text?.Trim() ?? "";
        var name = string.IsNullOrEmpty(path) ? "NombreProyecto" : System.IO.Path.GetFileName(path.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar));
        var std = "";
        if (ChkGenerateStandardHierarchy?.IsChecked == true)
        {
            try
            {
                var st = EngineSettings.Load();
                var order = st.GetResolvedNewProjectStandardRootFolders();
                var extra = st.GetResolvedExtraNewProjectRootFolders();
                std = "\n  (orden en raíz + extras)\n" + string.Join("\n", order.Select(f => $"  {f}/"));
                if (extra.Count > 0)
                    std += "\n  + " + string.Join(", ", extra);
            }
            catch
            {
                std = "\n  Sprites/ … (orden en Configuración del motor → Explorador)";
            }
        }
        TxtStructurePreview.Text = $"{name}/\n  Mapa/\n  Objetos/\n  Escenas/\n  Autoguardados/\n    Mapa/\n    Objetos/\n    Escenas/\n  Assets/\n    Sprites/\n    Sonidos/\n    Animations/\n    Logos/\n    Tilesets/\n  Maps/\n  Scripts/\n  Seeds/{std}";
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

    private void BtnBrowse_OnClick(object sender, RoutedEventArgs e)
    {
        var defaultPath = "";
        try { defaultPath = EngineSettings.Load().DefaultProjectsPath ?? ""; } catch { }
        if (string.IsNullOrWhiteSpace(defaultPath) || !Directory.Exists(defaultPath))
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

    private void TxtIconPath_TextChanged(object sender, TextChangedEventArgs e) => UpdateLogoPreview();

    private void UpdateLogoPreview()
    {
        if (ImgLogoPreview == null) return;
        var path = TxtIconPath?.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(path) || !File.Exists(path) || !IsImageFile(path))
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
        PreviewMinimap.Background = new SolidColorBrush(WpfColor.FromRgb(0x21, 0x26, 0x2d));
        var tile = int.TryParse(TxtTileSize?.Text, out var ts) ? Math.Clamp(ts, 8, 128) : 16;
        var chunk = int.TryParse(CmbChunkSize?.SelectedItem?.ToString(), out var cs) ? Math.Clamp(cs, 16, 64) : 16;
        var chunksSide = NewProjectStructure.DefaultMapChunksPerSide;
        var cells = chunk * chunksSide;
        // Rejilla: líneas cada "chunk" celdas lógicas en miniatura
        double stepX = w / (double)Math.Max(1, Math.Min(cells, 64));
        double stepY = h / (double)Math.Max(1, Math.Min(cells, 64));
        var gridMajor = new SolidColorBrush(WpfColor.FromRgb(0x48, 0x54, 0x66));
        var gridMinor = new SolidColorBrush(WpfColor.FromRgb(0x38, 0x40, 0x4d));
        for (int i = 0; i <= cells && i <= 64; i++)
        {
            var lineBrush = (i % chunk == 0) ? gridMajor : gridMinor;
            var x = i * stepX;
            if (x <= w)
            {
                PreviewMinimap.Children.Add(new WpfLine { X1 = x, Y1 = 0, X2 = x, Y2 = h, Stroke = lineBrush, StrokeThickness = (i % chunk == 0) ? 1 : 0.5 });
            }
            var y = i * stepY;
            if (y <= h)
            {
                PreviewMinimap.Children.Add(new WpfLine { X1 = 0, Y1 = y, X2 = w, Y2 = y, Stroke = lineBrush, StrokeThickness = (i % chunk == 0) ? 1 : 0.5 });
            }
        }
        var hint = new TextBlock
        {
            Text = $"{tile}px tile · chunk {chunk} · {chunksSide}×{chunksSide} chunks",
            Foreground = new SolidColorBrush(WpfColor.FromRgb(0x8b, 0x94, 0x9e)),
            FontSize = 9,
            Margin = new Thickness(4, 2, 0, 0)
        };
        Canvas.SetLeft(hint, 0);
        Canvas.SetTop(hint, 0);
        PreviewMinimap.Children.Add(hint);
    }

    private void BtnCreate_OnClick(object sender, RoutedEventArgs e)
    {
        var owner = HostWindow;
        var name = TxtName?.Text?.Trim() ?? "";
        var path = TxtPath?.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(name))
        {
            System.Windows.MessageBox.Show(owner, "Indique el nombre del juego.", "Nuevo proyecto",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(path))
        {
            System.Windows.MessageBox.Show(owner, "Seleccione o escriba la carpeta del proyecto.", "Nuevo proyecto",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!Directory.Exists(path) && (ChkCreateProjectFolder?.IsChecked != true))
        {
            System.Windows.MessageBox.Show(owner,
                "La carpeta no existe. Active \"Crear la carpeta del proyecto si no existe\" o seleccione una carpeta existente.",
                "Nuevo proyecto", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _resultName = name;
        _resultDescription = TxtDescription?.Text?.Trim() ?? "";
        _resultPath = path;
        _resultIconPath = TxtIconPath?.Text?.Trim() ?? "";
        _resultAuthor = TxtAuthor?.Text?.Trim() ?? "";
        _resultCopyright = TxtCopyright?.Text?.Trim() ?? "";
        _resultVersion = TxtVersion?.Text?.Trim() ?? "0.0.1";
        _resultTileSize = int.TryParse(TxtTileSize?.Text, out var ts2) ? Math.Clamp(ts2, 8, 128) : 16;
        _resultChunkSize = int.TryParse(CmbChunkSize?.SelectedItem?.ToString(), out var cs2) ? Math.Clamp(cs2, 16, 64) : 16;
        var nChunks = NewProjectStructure.DefaultMapChunksPerSide;
        _resultInitialChunksW = nChunks;
        _resultInitialChunksH = nChunks;
        _resultMapWidth = nChunks * _resultChunkSize;
        _resultMapHeight = nChunks * _resultChunkSize;
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
        _resultGenerateStandardHierarchy = ChkGenerateStandardHierarchy?.IsChecked != false;
        var bgRaw = TxtDefaultGameBg?.Text?.Trim() ?? "";
        _resultDefaultFirstSceneBackgroundColor = IsValidHexColor(bgRaw) ? bgRaw : "#FFFFFF";

        CreateCommitted?.Invoke(this, EventArgs.Empty);
    }
}
