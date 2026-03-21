using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using FUEngine.Core;
using FUEngine.Editor;
using FUEngine.Help;
using Color = System.Windows.Media.Color;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace FUEngine;

public partial class StartupWindow : Window
{
    private readonly DispatcherTimer _demoTimer;
    private readonly DispatcherTimer _tilePatternTimer;
    private const int DemoCols = 10, DemoRows = 8;
    private static readonly string[] Tips = new[]
    {
        "Haz tu primer mapa infinito con la herramienta Pintar tile.",
        "Prueba animación por capas para efectos 2.5D.",
        "Asigna scripts a puertas para onInteract y animación.",
        "Usa el modo Colocar objeto y la tecla R para rotar.",
        "Guarda todo (Archivo) para exportar mapa.json y objetos.json.",
        "Zoom con + y - en la barra del editor para trabajar cómodo.",
        "Cada tile puede ser Suelo, Pared, Objeto o Especial (script)."
    };
    private readonly Random _rnd = new();
    private readonly ObservableCollection<GlobalLibraryEntryDto> _libraryRows = new();

    public StartupWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            if (EngineSettings.Load().HardwareAccelerationEnabled) return;
            if (System.Windows.PresentationSource.FromVisual(this) is HwndSource hs && hs.CompositionTarget != null)
                hs.CompositionTarget.RenderMode = RenderMode.SoftwareOnly;
        };
        _demoTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _tilePatternTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _tilePatternTimer.Tick += TilePattern_Tick;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        EngineTypography.ApplyToRoot(this);
        var bootSettings = EngineSettings.Load();
        if (MainTabControl != null)
            MainTabControl.SelectedIndex = 0; // Hub por defecto
        LoadRecentProjects();
        ApplyTimeOfDayPalette();
        DrawTilePattern();
        _tilePatternTimer.Start();
        EditorLog.ToastRequested += OnToastRequested;
        Dispatcher.BeginInvoke(new Action(() => ApplyStartupBehaviorIfNeeded(bootSettings)), DispatcherPriority.Background);
    }

    private void ApplyStartupBehaviorIfNeeded(EngineSettings st)
    {
        try
        {
            if (st.StartupBehavior == "LastProject")
            {
                var list = StartupService.LoadRecentProjects().Where(x => File.Exists(x.Path))
                    .OrderByDescending(x => x.IsPinned).ThenByDescending(x => x.LastOpened).ToList();
                if (list.Count > 0)
                    OpenRecentProject(list[0]);
            }
            else if (st.StartupBehavior == "NewProject")
                BtnCreateProject_OnClick(this, new RoutedEventArgs());
        }
        catch { /* ignore */ }
    }

    private System.Windows.Threading.DispatcherTimer? _toastHideTimer;
    private void OnToastRequested(object? sender, (string Message, LogLevel Level) e)
    {
        if (ToastPanel == null || ToastText == null) return;
        Dispatcher.Invoke(() =>
        {
            ToastText.Text = e.Message;
            ToastText.Foreground = e.Level switch
            {
                LogLevel.Warning => new SolidColorBrush(Color.FromRgb(0xd2, 0x99, 0x22)),
                LogLevel.Error => new SolidColorBrush(Color.FromRgb(0xf8, 0x51, 0x49)),
                _ => new SolidColorBrush(Color.FromRgb(0xe6, 0xed, 0xf3))
            };
            ToastPanel.Visibility = Visibility.Visible;
            _toastHideTimer?.Stop();
            _toastHideTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(e.Level == LogLevel.Error ? 0 : 3) };
            _toastHideTimer.Tick += (_, _) => { ToastPanel.Visibility = Visibility.Collapsed; _toastHideTimer?.Stop(); };
            _toastHideTimer.Start();
        });
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        EditorLog.ToastRequested -= OnToastRequested;
        _demoTimer.Stop();
        _tilePatternTimer.Stop();
    }

    private string _currentFilterType = "Todos";
    private string? _currentFilterTag;
    private string _currentSortOrder = "Último abierto"; // "Último abierto", "Nombre", "Fecha modificación"
    private string _searchText = "";

    private void LoadRecentProjects()
    {
        var list = StartupService.LoadRecentProjects();
        var settings = EngineSettings.Load();
        if (!string.IsNullOrWhiteSpace(settings.DefaultProjectsPath) && Directory.Exists(settings.DefaultProjectsPath))
        {
            var discovered = StartupService.DiscoverProjects(settings.DefaultProjectsPath);
            list = StartupService.MergeWithDiscovered(list, discovered);
            foreach (var p in list)
                if (p.Tags == null) p.Tags = new List<string>();
            StartupService.SaveRecentList(list);
        }
        list = list.Where(x => File.Exists(x.Path)).ToList();
        foreach (var item in list)
            StartupService.RefreshProjectStats(item);
        list = list.OrderByDescending(x => x.IsPinned).ThenByDescending(x => x.LastOpened).ToList();
        var pinned = list.Where(x => x.IsPinned).ToList();
        var recent = list.Where(x => !x.IsPinned).ToList();
        if (HubPinnedList != null)
            HubPinnedList.ItemsSource = pinned;
        if (HubRecentList != null)
            HubRecentList.ItemsSource = recent;
        ApplyFilterAndSetSource(list);
        foreach (var item in list)
            GeneratePreviewAsync(item);
        UpdateStatsBar(list);
        UpdateFilterButtonStyles();
        if (CmbFilterTag != null && CmbFilterTag.ItemsSource == null)
        {
            var tagList = new List<string> { "Todas" };
            tagList.AddRange(StartupService.SuggestedTags);
            CmbFilterTag.ItemsSource = tagList;
            CmbFilterTag.SelectedIndex = 0;
        }
        if (CmbFilterTag != null && _currentFilterTag != null && CmbFilterTag.ItemsSource is IList<string> tagOptions)
        {
            for (int i = 0; i < tagOptions.Count; i++)
                if (string.Equals(tagOptions[i], _currentFilterTag, StringComparison.OrdinalIgnoreCase))
                { CmbFilterTag.SelectedIndex = i; break; }
        }
        if (CmbSortProjects != null && CmbSortProjects.ItemsSource == null)
        {
            CmbSortProjects.ItemsSource = new[] { "Último abierto", "Nombre", "Fecha modificación" };
            CmbSortProjects.SelectedIndex = 0;
        }
    }

    private void UpdateFilterButtonStyles()
    {
        foreach (var child in FilterButtonsPanel.Children.OfType<System.Windows.Controls.Button>())
        {
            child.Background = string.Equals(child.Tag as string, _currentFilterType, StringComparison.OrdinalIgnoreCase)
                ? (System.Windows.Media.Brush)FindResource("AccentBrush")
                : (System.Windows.Media.Brush)FindResource("BgLightBrush");
        }
    }

    private void ApplyFilterAndSetSource(List<RecentProjectInfo> list)
    {
        var filtered = _currentFilterType == "Todos"
            ? list
            : _currentFilterType == "Fijados"
                ? list.Where(x => x.IsPinned).ToList()
                : list.Where(x => string.Equals(x.ProjectType, _currentFilterType, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrEmpty(_currentFilterTag) && _currentFilterTag != "Todas")
            filtered = filtered.Where(x => x.Tags != null && x.Tags.Contains(_currentFilterTag, StringComparer.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var term = _searchText.Trim();
            filtered = filtered.Where(x =>
                (x.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) == true) ||
                (x.Path?.Contains(term, StringComparison.OrdinalIgnoreCase) == true) ||
                (x.ShortPath?.Contains(term, StringComparison.OrdinalIgnoreCase) == true)).ToList();
        }
        if (_currentSortOrder == "Nombre")
            filtered = filtered.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
        else if (_currentSortOrder == "Fecha modificación")
            filtered = filtered.OrderByDescending(x => x.LastModified).ToList();
        else
            filtered = filtered.OrderByDescending(x => x.IsPinned).ThenByDescending(x => x.LastOpened).ToList();
        RecentProjectsList.ItemsSource = filtered;
    }

    private void FilterByTag_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbFilterTag?.SelectedItem is not string tag) return;
        _currentFilterTag = tag == "Todas" ? null : tag;
        var list = StartupService.LoadRecentProjects().Where(x => File.Exists(x.Path)).ToList();
        list = list.OrderByDescending(x => x.IsPinned).ThenByDescending(x => x.LastOpened).ToList();
        ApplyFilterAndSetSource(list);
    }

    private void UpdateStatsBar(List<RecentProjectInfo>? list = null)
    {
        // Estadísticas integradas en cada tarjeta de proyecto
    }

    private void FilterByType_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn || btn.Tag is not string tag) return;
        _currentFilterType = tag;
        UpdateFilterButtonStyles();
        var list = StartupService.LoadRecentProjects().Where(x => File.Exists(x.Path)).ToList();
        list = list.OrderByDescending(x => x.IsPinned).ThenByDescending(x => x.LastOpened).ToList();
        ApplyFilterAndSetSource(list);
    }

    private void BtnPin_OnClick(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (sender is not System.Windows.Controls.Button btn || btn.DataContext is not RecentProjectInfo info) return;
        StartupService.TogglePin(info.Path);
        LoadRecentProjects();
    }

    private void RecentProjectsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Estado integrado en cada tarjeta de proyecto
    }

    private void RecentProjectsList_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // Permitir que el ListBox maneje la selección al hacer clic en la tarjeta
    }

    private void ProjectCard_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is Border border && border.RenderTransform is ScaleTransform scale)
        {
            scale.ScaleX = 1.02;
            scale.ScaleY = 1.02;
        }
    }

    private void ProjectCard_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is Border border && border.RenderTransform is ScaleTransform scale)
        {
            scale.ScaleX = 1;
            scale.ScaleY = 1;
        }
    }

    private void LeftPanel_DragEnter(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            e.Effects = System.Windows.DragDropEffects.Copy;
            LeftPanelDropZone.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x58, 0xa6, 0xff));
            LeftPanelDropZone.BorderThickness = new Thickness(2);
        }
    }

    private void LeftPanel_DragLeave(object sender, System.Windows.DragEventArgs e)
    {
        LeftPanelDropZone.BorderBrush = (System.Windows.Media.Brush)FindResource("BorderBrush");
        LeftPanelDropZone.BorderThickness = new Thickness(0, 0, 1, 0);
    }

    private void LeftPanel_Drop(object sender, System.Windows.DragEventArgs e)
    {
        LeftPanel_DragLeave(sender, e);
        if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop) || e.Data.GetData(System.Windows.DataFormats.FileDrop) is not string[] files)
            return;
        foreach (var f in files)
        {
            var path = f.Trim();
            if (string.IsNullOrEmpty(path)) continue;
            if (Directory.Exists(path))
            {
                var fuePath = System.IO.Path.Combine(path, FUEngine.Editor.NewProjectStructure.ProjectFileName);
                var projPath = System.IO.Path.Combine(path, "proyecto.json");
                var projectJson = System.IO.Path.Combine(path, "Project.json");
                if (File.Exists(fuePath)) path = fuePath;
                else if (File.Exists(projPath)) path = projPath;
                else if (File.Exists(projectJson)) path = projectJson;
            }
            if (!File.Exists(path) || (!path.EndsWith(FUEngine.Editor.NewProjectStructure.ProjectFileExtension, StringComparison.OrdinalIgnoreCase) && !path.EndsWith("proyecto.json", StringComparison.OrdinalIgnoreCase) && !path.EndsWith("Project.json", StringComparison.OrdinalIgnoreCase)))
                continue;
            try
            {
                var project = ProjectSerialization.Load(path);
                StartupService.AddRecentProject(path, project.Nombre ?? "", project.Descripcion, FUEngine.Core.EngineVersion.Current);
                LoadRecentProjects();
                break;
            }
            catch { /* skip invalid */ }
        }
    }

    private void QuickTemplate_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn || btn.Tag is not string tagStr || !int.TryParse(tagStr, out var templateId))
            return;
        var picker = new TemplatePickerWindow { Owner = this };
        picker.SelectTemplateById(templateId);
        if (picker.ShowDialog() != true || picker.SelectedTemplate == null) return;
        var templateData = TemplateProvider.GetTemplateData(picker.SelectedTemplate.Id);
        var optionsDlg = new CreateFromTemplateDialog(picker.SelectedTemplate, templateData);
        if (optionsDlg.ShowDialog() != true) return;
        var projectDir = optionsDlg.ProjectPath;
        var project = new ProjectInfo
        {
            Nombre = optionsDlg.ProjectName,
            Descripcion = templateData.Project.Descripcion,
            TileSize = optionsDlg.TileSize,
            MapWidth = optionsDlg.MapWidth,
            MapHeight = optionsDlg.MapHeight,
            Infinite = optionsDlg.Infinite,
            ProjectDirectory = projectDir
        };
        if (!Directory.Exists(projectDir))
            Directory.CreateDirectory(projectDir);
        var projectPath = System.IO.Path.Combine(projectDir, FUEngine.Editor.NewProjectStructure.ProjectFileName);
        ProjectSerialization.Save(project, projectPath);
        File.WriteAllText(System.IO.Path.Combine(projectDir, "mapa.json"), TemplateProvider.ToJson(templateData.Map));
        File.WriteAllText(System.IO.Path.Combine(projectDir, "objetos.json"), TemplateProvider.ToJson(templateData.Objects));
        var scriptsMerged = TemplateProvider.MergeWithCommonModules(templateData.Scripts);
        File.WriteAllText(System.IO.Path.Combine(projectDir, "scripts.json"), TemplateProvider.ToJson(scriptsMerged));
        File.WriteAllText(System.IO.Path.Combine(projectDir, "animaciones.json"), TemplateProvider.ToJson(templateData.Animations));
        StartupService.AddRecentProject(projectPath, project.Nombre, project.Descripcion, FUEngine.Core.EngineVersion.Current);
        OpenEditor(project);
        Close();
    }

    private void BtnResource_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn || btn.Tag is not string kind) return;
        System.Windows.MessageBox.Show(this,
            $"Recursos de tipo \"{kind}\": en una próxima versión se abrirá el marketplace interno con assets, sprites, tilesets, scripts y fuentes descargables.",
            "Recursos rápidos",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private static RecentProjectInfo? GetProjectFromContextMenu(object sender)
    {
        if (sender is not MenuItem mi) return null;
        var ctx = mi.Parent as ContextMenu;
        var target = ctx?.PlacementTarget as FrameworkElement;
        return target?.DataContext as RecentProjectInfo;
    }

    /// <summary>Obtiene el proyecto desde un botón (DataContext) o desde un ítem de menú contextual.</summary>
    private static RecentProjectInfo? GetProjectFromSender(object sender)
    {
        if (sender is FrameworkElement fe && fe.DataContext is RecentProjectInfo fromFe)
            return fromFe;
        return GetProjectFromContextMenu(sender);
    }

    private void CtxPin_OnClick(object sender, RoutedEventArgs e)
    {
        var info = GetProjectFromContextMenu(sender);
        if (info == null) return;
        StartupService.TogglePin(info.Path);
        LoadRecentProjects();
    }

    private void CtxProjectType_OnClick(object sender, RoutedEventArgs e)
    {
        var info = GetProjectFromContextMenu(sender);
        if (info == null || sender is not MenuItem mi || mi.Tag is not string tag) return;
        StartupService.SetProjectType(info.Path, tag);
        info.ProjectType = tag;
        LoadRecentProjects();
    }

    private void CtxAddTag_OnClick(object sender, RoutedEventArgs e)
    {
        var info = GetProjectFromContextMenu(sender);
        if (info == null || sender is not MenuItem mi || mi.Tag is not string tag) return;
        var list = new List<string>(info.Tags ?? new List<string>());
        if (!list.Contains(tag, StringComparer.OrdinalIgnoreCase))
            list.Add(tag);
        StartupService.SetTags(info.Path, list);
        LoadRecentProjects();
    }

    private void GeneratePreviewAsync(RecentProjectInfo item)
    {
        var path = item.Path;
        var projectDir = System.IO.Path.GetDirectoryName(path) ?? path;
        var mapPath = System.IO.Path.Combine(projectDir, "mapa.json");
        if (!File.Exists(mapPath)) return;
        System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                var map = MapSerialization.Load(mapPath);
                var coords = map.EnumerateChunkCoords().Take(4).ToList();
                if (coords.Count == 0) return;
                int minCx = coords.Min(c => c.cx), maxCx = coords.Max(c => c.cx);
                int minCy = coords.Min(c => c.cy), maxCy = coords.Max(c => c.cy);
                const int size = 48;
                var bmp = new System.Windows.Media.Imaging.WriteableBitmap(size, size, 96, 96, System.Windows.Media.PixelFormats.Bgr24, null);
                byte[] pixels = new byte[size * size * 3];
                int stride = size * 3;
                for (int py = 0; py < size; py++)
                    for (int px = 0; px < size; px++)
                    {
                        int tx = minCx * map.ChunkSize + (px * (maxCx - minCx + 1) * map.ChunkSize / size);
                        int ty = minCy * map.ChunkSize + (py * (maxCy - minCy + 1) * map.ChunkSize / size);
                        byte r = 0x21, g = 0x26, b = 0x2d;
                        if (map.TryGetTile(tx, ty, out var data) && data != null)
                        {
                            r = data.TipoTile switch { Core.TileType.Pared => 0x78, Core.TileType.Objeto => 0x5a, Core.TileType.Especial => 0x64, _ => 0x50 };
                            g = data.TipoTile switch { Core.TileType.Pared => 0x50, Core.TileType.Objeto => 0x5a, Core.TileType.Especial => 0x3c, _ => 0x50 };
                            b = data.TipoTile switch { Core.TileType.Pared => 0x3c, Core.TileType.Objeto => 0x78, Core.TileType.Especial => 0x64, _ => 0x50 };
                        }
                        int i = (py * size + px) * 3;
                        pixels[i] = b; pixels[i + 1] = g; pixels[i + 2] = r;
                    }
                bmp.WritePixels(new System.Windows.Int32Rect(0, 0, size, size), pixels, stride, 0);
                Dispatcher.Invoke(() => item.Preview = bmp);
            }
            catch { /* ignore */ }
        });
    }

    private void ApplyTimeOfDayPalette()
    {
        var hour = DateTime.Now.Hour;
        byte r = 13, g = 17, b = 23;
        if (hour >= 6 && hour < 12) { r = 18; g = 20; b = 28; }
        else if (hour >= 12 && hour < 20) { r = 20; g = 24; b = 32; }
        else if (hour >= 20 || hour < 6) { r = 10; g = 14; b = 20; }
        GradStop1.Color = Color.FromRgb(r, g, b);
        GradStop2.Color = Color.FromRgb((byte)(r + 3), (byte)(g + 3), (byte)(b + 5));
        GradStop3.Color = Color.FromRgb((byte)(r - 2), (byte)(g - 2), (byte)(b - 2));
    }

    private void DrawTilePattern()
    {
        if (TilePatternCanvas.Children.Count > 0) return;
        const int step = 24;
        int w = Math.Max(1200, (int)TilePatternCanvas.ActualWidth + step);
        int h = Math.Max(800, (int)TilePatternCanvas.ActualHeight + step);
        for (int x = 0; x < w; x += step)
            for (int y = 0; y < h; y += step)
            {
                var rect = new Rectangle { Width = 1, Height = 1, Fill = new SolidColorBrush(Color.FromRgb(88, 166, 255)) };
                System.Windows.Controls.Canvas.SetLeft(rect, x);
                System.Windows.Controls.Canvas.SetTop(rect, y);
                TilePatternCanvas.Children.Add(rect);
            }
    }

    private void TilePattern_Tick(object? sender, EventArgs e)
    {
        if (TilePatternCanvas.Children.Count == 0 && (TilePatternCanvas.ActualWidth > 0 || TilePatternCanvas.ActualHeight > 0))
            DrawTilePattern();
    }

    private void Logo_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        LogoGlow.BlurRadius = 20;
        LogoGlow.Opacity = 0.8;
    }

    private void Logo_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        LogoGlow.BlurRadius = 0;
        LogoGlow.Opacity = 0;
    }

    private void Theme_Changed(object sender, RoutedEventArgs e)
    {
        // Por ahora todo es oscuro; el switch podría aplicar paleta clara en futuras versiones
    }

    private void RecentProject_Open(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not RecentProjectInfo info) return;
        OpenRecentProject(info);
    }

    private void HubRecent_Open(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not RecentProjectInfo info) return;
        OpenRecentProject(info);
    }

    private void HubRecent_OpenBtn(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not RecentProjectInfo info) return;
        OpenRecentProject(info);
    }

    private void HubRecent_OpenFolder(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not RecentProjectInfo info) return;
        e.Handled = true;
        try
        {
            var dir = System.IO.Path.GetDirectoryName(info.Path);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                System.Diagnostics.Process.Start("explorer.exe", dir);
        }
        catch { /* ignore */ }
    }

    private void HubRecent_Pin(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        var info = GetProjectFromHubItem(sender);
        if (info == null) return;
        StartupService.TogglePin(info.Path);
        LoadRecentProjects();
    }

    /// <summary>Obtiene el RecentProjectInfo del ítem del Hub (botón dentro de DataTemplate).</summary>
    private static RecentProjectInfo? GetProjectFromHubItem(object sender)
    {
        if (sender is not DependencyObject dep) return null;
        var item = VisualTreeHelper.GetParent(dep);
        while (item != null && item is not ListBoxItem)
            item = VisualTreeHelper.GetParent(item);
        return (item as ListBoxItem)?.DataContext as RecentProjectInfo;
    }

    private void HubRecent_Remove(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not RecentProjectInfo info) return;
        e.Handled = true;
        StartupService.RemoveFromRecent(info.Path);
        LoadRecentProjects();
    }

    private void RecentProject_OpenBtn(object sender, RoutedEventArgs e)
    {
        var info = GetProjectFromSender(sender);
        if (info == null) return;
        OpenRecentProject(info);
    }

    private void RecentProject_OpenFolder(object sender, RoutedEventArgs e)
    {
        var info = GetProjectFromSender(sender);
        if (info == null) return;
        e.Handled = true;
        try
        {
            var dir = System.IO.Path.GetDirectoryName(info.Path);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                System.Diagnostics.Process.Start("explorer.exe", dir);
        }
        catch { /* ignore */ }
    }

    private void RecentProject_Config(object sender, RoutedEventArgs e)
    {
        var info = GetProjectFromSender(sender);
        if (info == null || !File.Exists(info.Path)) return;
        e.Handled = true;
        try
        {
            var project = ProjectSerialization.Load(info.Path);
            var configWindow = new ProjectConfigWindow(project, info.Path) { Owner = this };
            configWindow.ShowDialog();
            StartupService.RefreshProjectStats(info);
            LoadRecentProjects();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, "No se pudo abrir la configuración: " + ex.Message, "Configuración", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void RecentProject_Remove(object sender, RoutedEventArgs e)
    {
        var info = GetProjectFromSender(sender);
        if (info == null) return;
        e.Handled = true;
        StartupService.RemoveFromRecent(info.Path);
        LoadRecentProjects();
    }

    private void RecentProject_DeletePermanently(object sender, RoutedEventArgs e)
    {
        var info = GetProjectFromSender(sender);
        if (info == null) return;
        e.Handled = true;
        var displayName = !string.IsNullOrWhiteSpace(info.Name) ? info.Name.Trim() : info.ShortPath;
        if (string.IsNullOrEmpty(displayName)) displayName = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(info.Path)) ?? "Proyecto";
        var dlg = new ConfirmDeleteProjectDialog(displayName) { Owner = this };
        dlg.ShowDialog();
        if (!dlg.Confirmed) return;
        var projectDir = System.IO.Path.GetDirectoryName(info.Path);
        if (string.IsNullOrEmpty(projectDir) || !Directory.Exists(projectDir))
        {
            System.Windows.MessageBox.Show(this, "La carpeta del proyecto no existe.", "Eliminar", MessageBoxButton.OK, MessageBoxImage.Information);
            StartupService.RemoveFromRecent(info.Path);
            LoadRecentProjects();
            return;
        }
        try
        {
            Directory.Delete(projectDir, recursive: true);
            StartupService.RemoveFromRecent(info.Path);
            LoadRecentProjects();
            System.Windows.MessageBox.Show(this, "Proyecto eliminado correctamente.", "Eliminar", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, "No se pudo eliminar: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RecentProject_Properties(object sender, RoutedEventArgs e)
    {
        var info = GetProjectFromSender(sender);
        if (info == null) return;
        e.Handled = true;
        if (!File.Exists(info.Path))
        {
            System.Windows.MessageBox.Show(this, "El archivo del proyecto ya no existe.", "Propiedades", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            var project = FUEngine.Editor.ProjectSerialization.Load(info.Path);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Id: " + (project.Id ?? "—"));
            sb.AppendLine("Nombre: " + (project.Nombre ?? ""));
            sb.AppendLine("Ruta: " + info.Path);
            sb.AppendLine("Directorio: " + (project.ProjectDirectory ?? ""));
            sb.AppendLine("Versión: " + (project.Version ?? ""));
            sb.AppendLine("Motor: " + (project.EngineVersion ?? "—"));
            sb.AppendLine("Resolución: " + project.GameResolutionWidth + "×" + project.GameResolutionHeight);
            sb.AppendLine("FPS: " + project.Fps);
            sb.AppendLine("TileSize: " + project.TileSize);
            sb.AppendLine("Escenas: " + (project.Scenes?.Count ?? 0));
            if (project.Scenes != null)
                foreach (var s in project.Scenes)
                    sb.AppendLine("  · " + (s.Name ?? s.Id) + " (Id: " + (s.Id ?? "") + ")");
            sb.AppendLine("Última modificación: " + info.LastModifiedDisplay);
            sb.AppendLine("Tamaño: " + info.ProjectSizeDisplay);
            sb.AppendLine("Assets: " + info.AssetsSizeDisplay);
            var w = new Window
            {
                Title = "Propiedades — " + (info.Name ?? "Proyecto"),
                Width = 480,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x0d, 0x11, 0x17))
            };
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16) };
            var txt = new TextBlock
            {
                Text = sb.ToString(),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xe6, 0xed, 0xf3)),
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap
            };
            scroll.Content = txt;
            w.Content = scroll;
            w.ShowDialog();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, "Error al cargar propiedades: " + ex.Message, "Propiedades", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TxtSearchProjects_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (TxtSearchProjects == null) return;
        _searchText = TxtSearchProjects.Text ?? "";
        var list = StartupService.LoadRecentProjects().Where(x => File.Exists(x.Path)).ToList();
        list = list.OrderByDescending(x => x.IsPinned).ThenByDescending(x => x.LastOpened).ToList();
        ApplyFilterAndSetSource(list);
    }

    private void CmbSortProjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbSortProjects?.SelectedItem is not string sort) return;
        _currentSortOrder = sort;
        var list = StartupService.LoadRecentProjects().Where(x => File.Exists(x.Path)).ToList();
        list = list.OrderByDescending(x => x.IsPinned).ThenByDescending(x => x.LastOpened).ToList();
        ApplyFilterAndSetSource(list);
    }

    private void OpenRecentProject(RecentProjectInfo info)
    {
        var path = info.Path;
        if (!File.Exists(path))
        {
            System.Windows.MessageBox.Show(this, "El proyecto ya no existe en esa ruta.", "Abrir", MessageBoxButton.OK, MessageBoxImage.Warning);
            LoadRecentProjects();
            return;
        }
        try
        {
            var project = ProjectSerialization.Load(path);
            OpenEditor(project);
            StartupService.AddRecentProject(path, project.Nombre, project.Descripcion, FUEngine.Core.EngineVersion.Current);
            Close();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, "No se pudo abrir: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCreateProject_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var panel = new NewProjectPanel();
            panel.CreateClicked += CreateProjectPanel_OnCreate;
            panel.CancelClicked += OnCreatePanelCancel;
            OverlayContentHost.Child = panel;
            OverlayPanel.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, "Error: " + ex.Message, "Crear proyecto", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CreateProjectPanel_OnCreate(object? sender, EventArgs e)
    {
        if (sender is not NewProjectPanel panel) return;
        try
        {
            var projectDir = panel.ProjectPath ?? "";
            if (string.IsNullOrWhiteSpace(projectDir))
            {
                System.Windows.MessageBox.Show(this, "Selecciona la carpeta del proyecto.", "Crear proyecto", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            var project = new ProjectInfo
            {
                Nombre = panel.ProjectName ?? "",
                Descripcion = panel.Description ?? "",
                ProjectDirectory = projectDir,
                TileSize = 16,
                MapWidth = 64,
                MapHeight = 64,
                Infinite = true,
                ChunkSize = 16,
                AutoSaveEnabled = true,
                AutoSaveIntervalMinutes = 5,
                AutoSaveMaxBackupsPerType = 10,
                AutoSaveFolder = "Autoguardados",
                AutoSaveOnClose = true
            };
            if (!Directory.Exists(projectDir))
            {
                if (panel.CreateProjectFolderIfMissing)
                    Directory.CreateDirectory(projectDir);
                else
                {
                    System.Windows.MessageBox.Show(this, "La carpeta del proyecto no existe. Active \"Crear la carpeta si no existe\".", "Crear proyecto", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
            }
            var proyectoConfig = new ProyectoConfigDto
            {
                Nombre = project.Nombre,
                Logo = "logo.png",
                Plantilla = "Blank",
                AutoguardadoActivo = true,
                IntervaloAutoguardadoMin = 5,
                MaxBackupsAutoguardado = 10,
                GuardarSoloCambios = true,
                Descripcion = project.Descripcion,
                Version = "0.1"
            };
            var projectPath = NewProjectStructure.Create(projectDir, project, null, proyectoConfig);
            StartupService.AddRecentProject(projectPath, project.Nombre, project.Descripcion, FUEngine.Core.EngineVersion.Current);
            HideOverlay();
            OpenEditor(project);
            Close();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, "No se pudo crear el proyecto: " + ex.Message, "Crear proyecto", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool _overlayClosing;
    private void HideOverlay()
    {
        if (OverlayPanel.Visibility != Visibility.Visible || _overlayClosing) return;
        _overlayClosing = true;
        try
        {
            if (OverlayContentHost.Child is NewProjectPanel panel)
            {
                panel.CreateClicked -= CreateProjectPanel_OnCreate;
                panel.CancelClicked -= OnCreatePanelCancel;
            }
            OverlayPanel.Visibility = Visibility.Collapsed;
            OverlayContentHost.Child = null;
        }
        finally
        {
            _overlayClosing = false;
        }
    }

    private void OnCreatePanelCancel(object? sender, EventArgs e) => HideOverlay();

    private void OverlayPanel_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.Handled) return;
        var src = e.OriginalSource as System.Windows.DependencyObject;
        if (src != null && OverlayContentHost != null && IsVisualDescendant(OverlayContentHost, src))
            return;
        HideOverlay();
    }

    private static bool IsVisualDescendant(System.Windows.DependencyObject ancestor, System.Windows.DependencyObject node)
    {
        while (node != null)
        {
            if (node == ancestor) return true;
            node = System.Windows.Media.VisualTreeHelper.GetParent(node);
        }
        return false;
    }

    private void OverlayContentHost_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private void BtnOpenProject_OnClick(object sender, RoutedEventArgs e)
    {
        var settings = EngineSettings.Load();
        var initialDir = !string.IsNullOrWhiteSpace(settings.DefaultProjectsPath) && Directory.Exists(settings.DefaultProjectsPath)
            ? settings.DefaultProjectsPath
            : (Directory.Exists(EngineSettings.GetDefaultProjectsRoot()) ? EngineSettings.GetDefaultProjectsRoot() : "");
        var openDlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Proyecto FUEngine (*.FUE)|*.FUE|Legacy (Project.json, proyecto.json)|Project.json;proyecto.json|Todos|*.*",
            Title = "Abrir proyecto",
            InitialDirectory = initialDir
        };
        if (openDlg.ShowDialog() != true) return;
        try
        {
            var project = ProjectSerialization.Load(openDlg.FileName);
            StartupService.AddRecentProject(openDlg.FileName, project.Nombre, project.Descripcion, FUEngine.Core.EngineVersion.Current);
            OpenEditor(project);
            Close();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, "No se pudo abrir el proyecto: " + ex.Message, "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Plantillas siguen en ventana modal; para UX consistente con "Crear proyecto" podría migrarse a overlay.
    private void BtnTemplates_OnClick(object sender, RoutedEventArgs e)
    {
        HideOverlay();
        var picker = new TemplatePickerWindow { Owner = this };
        if (picker.ShowDialog() != true || picker.SelectedTemplate == null) return;
        var templateData = TemplateProvider.GetTemplateData(picker.SelectedTemplate.Id);
        var optionsDlg = new CreateFromTemplateDialog(picker.SelectedTemplate, templateData);
        if (optionsDlg.ShowDialog() != true) return;
        var projectDir = optionsDlg.ProjectPath;
        var project = new ProjectInfo
        {
            Nombre = optionsDlg.ProjectName,
            Descripcion = templateData.Project.Descripcion,
            TileSize = optionsDlg.TileSize,
            MapWidth = optionsDlg.MapWidth,
            MapHeight = optionsDlg.MapHeight,
            Infinite = optionsDlg.Infinite,
            ProjectDirectory = projectDir
        };
        if (!Directory.Exists(projectDir))
            Directory.CreateDirectory(projectDir);
        var projectPath = System.IO.Path.Combine(projectDir, FUEngine.Editor.NewProjectStructure.ProjectFileName);
        ProjectSerialization.Save(project, projectPath);
        File.WriteAllText(System.IO.Path.Combine(projectDir, "mapa.json"), TemplateProvider.ToJson(templateData.Map));
        File.WriteAllText(System.IO.Path.Combine(projectDir, "objetos.json"), TemplateProvider.ToJson(templateData.Objects));
        var scriptsMerged = TemplateProvider.MergeWithCommonModules(templateData.Scripts);
        File.WriteAllText(System.IO.Path.Combine(projectDir, "scripts.json"), TemplateProvider.ToJson(scriptsMerged));
        File.WriteAllText(System.IO.Path.Combine(projectDir, "animaciones.json"), TemplateProvider.ToJson(templateData.Animations));
        StartupService.AddRecentProject(projectPath, project.Nombre, project.Descripcion, FUEngine.Core.EngineVersion.Current);
        OpenEditor(project);
        Close();
    }

    private void BtnTutorials_OnClick(object sender, RoutedEventArgs e)
    {
        System.Windows.MessageBox.Show(this, "Tutoriales y documentación próximamente. Mientras tanto, explora Crear proyecto y el editor.", "Tutoriales", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnConfig_OnClick(object sender, RoutedEventArgs e)
    {
        BtnSettings_OnClick(sender, e);
    }

    private void BtnQuickScript_OnClick(object sender, RoutedEventArgs e)
    {
        System.Windows.MessageBox.Show(this, "Crear script rápido: en una próxima versión podrás elegir Programación o Nodos sin abrir un proyecto.", "Script rápido", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnAyudaMotor_OnClick(object sender, RoutedEventArgs e)
    {
        new DocumentationWindow { Owner = this, InitialTopicId = EngineDocumentation.QuickStartTopicId }.Show();
    }

    private void BtnSettings_OnClick(object sender, RoutedEventArgs e)
    {
        // Abrir/focus el tab Configuración del motor en la misma ventana (índice 3: Hub=0, Assets=1, Proyectos=2, Configuración=3)
        if (MainTabControl != null)
            MainTabControl.SelectedIndex = 3;
        if (FrameEngineSettings != null && FrameEngineSettings.Content == null)
            FrameEngineSettings.Navigate(new Uri("Dialogs/SettingsPage.xaml", UriKind.Relative));
    }

    private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MainTabControl?.SelectedIndex == 1)
            RefreshGlobalLibraryTab();
        if (MainTabControl?.SelectedIndex != 3 || FrameEngineSettings == null) return;
        if (FrameEngineSettings.Content == null)
            FrameEngineSettings.Navigate(new Uri("Dialogs/SettingsPage.xaml", UriKind.Relative));
    }

    private void RefreshGlobalLibraryTab()
    {
        if (LibraryDataGrid == null) return;
        try
        {
            var st = EngineSettings.Load();
            var root = GlobalAssetLibraryService.ResolveSharedAssetsRoot(st);
            Directory.CreateDirectory(GlobalAssetLibraryService.GetLibraryFilesDirectory(root));
            if (TxtLibraryPathHint != null)
            {
                TxtLibraryPathHint.Text = string.IsNullOrWhiteSpace(st.SharedAssetsPath)
                    ? $"Sin carpeta personalizada en Configuración; usando la ruta por defecto: {root}"
                    : $"Carpeta compartida: {root}";
            }
            var manifest = GlobalAssetLibraryService.LoadManifest(root);
            _libraryRows.Clear();
            foreach (var entry in manifest.Entries)
                _libraryRows.Add(entry);
            LibraryDataGrid.ItemsSource = _libraryRows;
        }
        catch (Exception ex)
        {
            EditorLog.Warning($"Biblioteca: {ex.Message}", "Hub");
        }
    }

    private int GetLibraryDefaultTileSize()
    {
        if (CmbLibraryTileSize?.SelectedItem is ComboBoxItem item && item.Tag is string tag && int.TryParse(tag, out var n) && n > 0)
            return n;
        return 16;
    }

    private void BtnLibraryAdd_OnClick(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Multiselect = true,
            Filter = "Assets|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp;*.wav;*.mp3;*.ogg|Todas|*.*"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var st = EngineSettings.Load();
            var root = GlobalAssetLibraryService.ResolveSharedAssetsRoot(st);
            Directory.CreateDirectory(GlobalAssetLibraryService.GetLibraryFilesDirectory(root));
            GlobalAssetLibraryService.ImportFiles(root, dlg.FileNames, GetLibraryDefaultTileSize());
            RefreshGlobalLibraryTab();
            EditorLog.Toast($"{dlg.FileNames.Length} archivo(s) añadidos a la biblioteca.", LogLevel.Info, "Biblioteca");
        }
        catch (Exception ex)
        {
            EditorLog.Warning($"No se pudieron importar archivos: {ex.Message}", "Biblioteca");
        }
    }

    private void BtnLibrarySaveManifest_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var st = EngineSettings.Load();
            var root = GlobalAssetLibraryService.ResolveSharedAssetsRoot(st);
            var manifest = new GlobalLibraryManifestDto { Version = 1, Entries = _libraryRows.ToList() };
            GlobalAssetLibraryService.SaveManifest(root, manifest);
            EditorLog.Toast("Índice library.json guardado.", LogLevel.Info, "Biblioteca");
        }
        catch (Exception ex)
        {
            EditorLog.Warning($"No se pudo guardar la biblioteca: {ex.Message}", "Biblioteca");
        }
    }

    private void BtnLibraryRemove_OnClick(object sender, RoutedEventArgs e)
    {
        if (LibraryDataGrid?.SelectedItem is not GlobalLibraryEntryDto row) return;
        try
        {
            var st = EngineSettings.Load();
            var root = GlobalAssetLibraryService.ResolveSharedAssetsRoot(st);
            GlobalAssetLibraryService.RemoveEntry(root, row.Id, deleteFile: true);
            _libraryRows.Remove(row);
            EditorLog.Toast("Entrada eliminada.", LogLevel.Info, "Biblioteca");
        }
        catch (Exception ex)
        {
            EditorLog.Warning($"No se pudo eliminar: {ex.Message}", "Biblioteca");
        }
    }

    private void BtnSaveEngineConfig_OnClick(object sender, RoutedEventArgs e)
    {
        if (FrameEngineSettings?.Content is SettingsPage page)
        {
            page.SaveSettings();
            EditorLog.Toast("Configuración guardada.", LogLevel.Info, "Configuración");
        }
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        foreach (var file in Directory.GetFiles(sourceDir))
            File.Copy(file, System.IO.Path.Combine(destDir, System.IO.Path.GetFileName(file)));
        foreach (var sub in Directory.GetDirectories(sourceDir))
            CopyDirectory(sub, System.IO.Path.Combine(destDir, System.IO.Path.GetFileName(sub)));
    }

    private void OpenEditor(ProjectInfo project)
    {
        var editor = new EditorWindow(project);
        editor.Show();
        System.Windows.Application.Current.MainWindow = editor;
    }
}
