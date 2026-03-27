using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace FUEngine;

public partial class ProjectExplorerPanel : System.Windows.Controls.UserControl
{
    private string _projectDirectory = "";
    private string _projectName = "";
    private ProjectExplorerItem? _rootFull;
    private string _searchText = "";
    private int _filterIndex = 0; // 0=Todo, 1=Mapa, 2=Objetos, 3=Scripts, 4=Animaciones, 5=TileSet, 6=Seed
    private ProjectExplorerItem? _dragStartItem;
    private System.Windows.Point? _dragStartPoint;
    private ExplorerMetadataService? _metadataService;
    private HashSet<string>? _sceneUsedPaths;
    private int _sceneFilterMode; // 0=Todos, 1=Usados en escena, 2=No usados

    /// <summary>Modo compacto (Small Explorer bajo Jerarquía) vs completo (Large Explorer tab).</summary>
    public bool IsCompactMode { get; set; }

    /// <summary>Oculta la carpeta <c>Data/</c> en el árbol (índices del sistema); configurable en preferencias del motor.</summary>
    public bool HideDataFolderInExplorer { get; set; } = true;

    public const string DataFormatExplorerItem = "FUEngine.ExplorerItem";
    public const string DataFormatAssetPath = "FUEngine.AssetPath";

    public event EventHandler<ProjectExplorerItem?>? SelectionChanged;
    /// <summary>Se dispara al hacer doble clic en un archivo editable (JSON, script, etc.) para abrir el editor de código.</summary>
    public event EventHandler<ProjectExplorerItem?>? RequestOpenInEditor;
    /// <summary>Se dispara al elegir "Editar colisiones..." en una imagen para abrir el Collisions Editor.</summary>
    public event EventHandler<ProjectExplorerItem?>? RequestOpenInCollisionsEditor;
    /// <summary>Se dispara al elegir "Abrir en Tile por script" en un .lua para abrir el tab Lua Tile Gen.</summary>
    public event EventHandler<ProjectExplorerItem?>? RequestOpenInScriptableTile;
    /// <summary>Tras crear un .lua y registrarlo en scripts.json (id estable para asignar a objetos).</summary>
    public event EventHandler<ScriptRegisteredEventArgs>? LuaScriptRegistered;

    /// <summary>Tras mutar scripts.json (borrar/renombrar/duplicar script, carpeta de scripts, etc.).</summary>
    public event EventHandler? ScriptsRegistryChanged;

    /// <summary>Arrastrar un objeto desde la jerarquía/inspector sobre una carpeta del explorador: guardar como .seed en esa ruta.</summary>
    public event EventHandler<(string InstanceId, string TargetFolderPath)>? RequestExportObjectAsSeed;

    /// <summary>Crear desde menú contextual del explorador: triggers, etc. (objetos: solo desde la jerarquía del mapa).</summary>
    public event EventHandler? RequestCreateTriggerZone;

    public void OpenInEditor(ProjectExplorerItem? item)
    {
        if (item != null) RequestOpenInEditor?.Invoke(this, item);
    }

    public ProjectExplorerPanel()
    {
        InitializeComponent();
        CmbFilter.ItemsSource = new[] { "Todo", "Mapa / Tiles", "Objetos", "Scripts", "Animaciones", "TileSet", "Seed" };
        CmbFilter.SelectedIndex = 0;
    }

    /// <summary>Directorio raíz del proyecto (para importar archivos arrastrados desde el SO en el Explorador grande).</summary>
    public string ProjectDirectory => _projectDirectory;

    public void SetProject(string projectDirectory, string projectName)
    {
        _projectDirectory = projectDirectory ?? "";
        _projectName = projectName ?? "Proyecto";
        _gridCurrentFolderPath = _projectDirectory;
        RefreshTree();
        ApplyCompactMode();
    }

    public void ApplyCompactMode()
    {
        if (PreviewPanel != null)
            PreviewPanel.MaxHeight = IsCompactMode ? 100 : 160;
        if (CompactSectionsPanel != null)
            CompactSectionsPanel.Visibility = IsCompactMode ? Visibility.Visible : Visibility.Collapsed;
        if (CmbSceneFilter != null)
        {
            CmbSceneFilter.Visibility = IsCompactMode ? Visibility.Visible : Visibility.Collapsed;
            if (IsCompactMode && CmbSceneFilter.Items.Count == 0)
            {
                CmbSceneFilter.Items.Clear();
                CmbSceneFilter.Items.Add("Todos");
                CmbSceneFilter.Items.Add("Usados en escena");
                CmbSceneFilter.Items.Add("No usados");
                CmbSceneFilter.SelectedIndex = 0;
            }
        }
        if (IsCompactMode)
            RefreshQuickLists();
    }

    /// <summary>Actualiza Favoritos (incluye lo fijado) y Recientes. Todo lo fijado = favorito; una sola lista "Favoritos".</summary>
    private void RefreshQuickLists()
    {
        if (_metadataService == null) return;
        var fav = _metadataService.GetFavorites();
        var rec = _metadataService.GetRecent();
        var pin = _metadataService.GetPinned();
        var favoritosUnidos = fav.Union(pin, StringComparer.OrdinalIgnoreCase).Distinct().ToList();
        if (LstFavorites != null) { LstFavorites.ItemsSource = null; LstFavorites.ItemsSource = favoritosUnidos; }
        if (LstRecent != null) { LstRecent.ItemsSource = null; LstRecent.ItemsSource = rec.ToList(); }
        var byCreation = rec.OrderByDescending(p =>
        {
            var full = Path.IsPathRooted(p) ? p : Path.Combine(_projectDirectory ?? "", p);
            return File.Exists(full) ? File.GetCreationTimeUtc(full) : DateTime.MinValue;
        }).ToList();
        if (LstRecentByCreation != null) { LstRecentByCreation.ItemsSource = null; LstRecentByCreation.ItemsSource = byCreation; }
    }

    private void LstRecentByCreation_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (LstRecentByCreation?.SelectedItem is string path) SelectPathInTree(path);
    }

    private void LstFavorites_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (LstFavorites?.SelectedItem is string path) SelectPathInTree(path);
    }

    private void LstRecent_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (LstRecent?.SelectedItem is string path) SelectPathInTree(path);
    }

    private void LstQuick_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is System.Windows.Controls.ListBox lb && lb.SelectedItem is string path)
            SelectPathInTree(path);
    }

    /// <summary>Selecciona un archivo o carpeta en el árbol y dispara <see cref="SelectionChanged"/>.</summary>
    public void SelectAbsolutePath(string? fullPath) => SelectPathInTree(fullPath);

    private void SelectPathInTree(string? fullPath)
    {
        if (string.IsNullOrEmpty(fullPath) || ProjectTree?.ItemsSource == null) return;
        var normalized = fullPath.Replace('/', System.IO.Path.DirectorySeparatorChar);
        if (!Path.IsPathRooted(normalized) && !string.IsNullOrEmpty(_projectDirectory))
            normalized = Path.Combine(_projectDirectory, normalized);
        ProjectExplorerItem? found = null;
        foreach (ProjectExplorerItem root in ProjectTree.ItemsSource)
        {
            found = FindItemByPath(root, normalized);
            if (found != null) break;
        }
        if (found != null && ProjectTree != null)
        {
            var container = ProjectTree.ItemContainerGenerator.ContainerFromItem(found) as System.Windows.Controls.TreeViewItem;
            if (container != null)
                container.IsSelected = true;
        }
    }

    private static ProjectExplorerItem? FindItemByPath(ProjectExplorerItem node, string fullPath)
    {
        if (string.Equals(node.FullPath, fullPath, StringComparison.OrdinalIgnoreCase)) return node;
        foreach (var c in node.Children)
        {
            var found = FindItemByPath(c, fullPath);
            if (found != null) return found;
        }
        return null;
    }

    private void CmbSceneFilter_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _sceneFilterMode = CmbSceneFilter?.SelectedIndex ?? 0;
        RefreshTree();
    }

    private void ProjectTree_OnExpanded(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.TreeViewItem tvi && tvi.DataContext is ProjectExplorerItem item && item.IsFolder && _metadataService != null)
            SaveExpandedState();
    }

    private void ProjectTree_OnCollapsed(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.TreeViewItem tvi && tvi.DataContext is ProjectExplorerItem item && item.IsFolder && _metadataService != null)
            SaveExpandedState();
    }

    private void SaveExpandedState()
    {
        if (_metadataService == null || ProjectTree == null) return;
        var expanded = new List<string>();
        CollectExpandedPathsFromVisual(ProjectTree, expanded);
        _metadataService.SetExpandedFolderPaths(expanded);
    }

    private static void CollectExpandedPathsFromVisual(System.Windows.DependencyObject parent, List<string> list)
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is System.Windows.Controls.TreeViewItem tvi && tvi.DataContext is ProjectExplorerItem item && item.IsFolder)
            {
                if (tvi.IsExpanded)
                    list.Add(item.FullPath);
                CollectExpandedPathsFromVisual(tvi, list);
            }
        }
    }

    private void RestoreExpandedState()
    {
        if (ProjectTree?.ItemsSource == null) return;
        var paths = _metadataService != null ? _metadataService.GetExpandedFolderPaths().ToList() : new List<string>();
        if (paths.Count == 0)
        {
            ExpandRootAndFirstLevel();
            return;
        }
        if (_metadataService == null) return;
        foreach (ProjectExplorerItem root in ProjectTree.ItemsSource)
        {
            foreach (var path in paths.OrderBy(p => p.Count(c => c == Path.DirectorySeparatorChar)))
            {
                var node = FindItemByPath(root, path);
                if (node == null) continue;
                ExpandToItem(ProjectTree, root, node);
            }
        }
    }

    private void ExpandRootAndFirstLevel()
    {
        if (ProjectTree?.ItemsSource == null) return;
        foreach (ProjectExplorerItem root in ProjectTree.ItemsSource)
        {
            var rootContainer = ProjectTree.ItemContainerGenerator.ContainerFromItem(root) as System.Windows.Controls.TreeViewItem;
            if (rootContainer != null)
            {
                rootContainer.IsExpanded = true;
                foreach (var child in root.Children)
                {
                    rootContainer.ApplyTemplate();
                    var childContainer = rootContainer.ItemContainerGenerator.ContainerFromItem(child) as System.Windows.Controls.TreeViewItem;
                    if (childContainer != null)
                        childContainer.IsExpanded = true;
                }
            }
        }
    }

    private static void ExpandToItem(System.Windows.Controls.TreeView tree, ProjectExplorerItem root, ProjectExplorerItem target)
    {
        var pathToTarget = new List<ProjectExplorerItem>();
        if (!CollectPathToItem(root, target, pathToTarget)) return;
        System.Windows.Controls.TreeViewItem? current = null;
        foreach (var node in pathToTarget)
        {
            System.Windows.Controls.ItemsControl parent = current ?? (System.Windows.Controls.ItemsControl)tree;
            var container = parent.ItemContainerGenerator.ContainerFromItem(node) as System.Windows.Controls.TreeViewItem;
            if (container == null) return;
            container.IsExpanded = true;
            current = container;
        }
    }

    private static bool CollectPathToItem(ProjectExplorerItem from, ProjectExplorerItem target, List<ProjectExplorerItem> path)
    {
        path.Add(from);
        if (from == target) return true;
        foreach (var c in from.Children)
        {
            if (CollectPathToItem(c, target, path)) return true;
        }
        path.RemoveAt(path.Count - 1);
        return false;
    }

    public void SetMetadataService(ExplorerMetadataService? service)
    {
        _metadataService = service;
        RefreshTree();
    }

    public ExplorerMetadataService? GetMetadataService() => _metadataService;

    /// <summary>Rutas de assets usados en la escena actual (para filtro en Small Explorer).</summary>
    public void SetSceneUsedPaths(IReadOnlySet<string>? paths)
    {
        _sceneUsedPaths = paths != null ? new HashSet<string>(paths, StringComparer.OrdinalIgnoreCase) : null;
        RefreshTree();
    }

    /// <summary>Importa archivos desde rutas del SO al directorio indicado (o raíz del proyecto). Usado por el Explorador grande al soltar archivos desde el escritorio.</summary>
    public void ImportFilesFromPaths(string[] sourcePaths, string? targetDirectory = null)
    {
        var destDir = targetDirectory ?? _projectDirectory;
        if (string.IsNullOrEmpty(destDir) || !Directory.Exists(destDir)) return;
        foreach (var src in sourcePaths)
        {
            if (Directory.Exists(src)) continue;
            if (!File.Exists(src)) continue;
            var name = Path.GetFileName(src);
            if (string.IsNullOrEmpty(name)) continue;
            var dest = Path.Combine(destDir, name);
            if (string.Equals(src, dest, StringComparison.OrdinalIgnoreCase)) continue;
            try { File.Copy(src, dest, overwrite: true); } catch { /* ignorar por archivo */ }
        }
        RefreshTree();
    }

    private int _restoreVersion;

    private List<ProjectExplorerItem> _flatItems = new();
    private readonly List<ProjectExplorerItem> _gridFolderItems = new();
    private string _viewMode = "Tree";
    /// <summary>Carpeta mostrada en vista Grid (ruta absoluta bajo el proyecto).</summary>
    private string _gridCurrentFolderPath = "";

    public void RefreshTree()
    {
        _rootFull = BuildTree();
        var viewRoot = FilterAndClone(_rootFull);
        ProjectTree.ItemsSource = viewRoot != null ? new[] { viewRoot } : null;
        UpdateFlatList(viewRoot);
        var version = ++_restoreVersion;
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (version != _restoreVersion) return;
            RestoreExpandedState();
        }), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private static void FlattenInto(ProjectExplorerItem? item, List<ProjectExplorerItem> outList)
    {
        if (item == null) return;
        outList.Add(item);
        foreach (var c in item.Children)
            FlattenInto(c, outList);
    }

    private void UpdateFlatList(ProjectExplorerItem? viewRoot)
    {
        _flatItems.Clear();
        if (viewRoot != null) FlattenInto(viewRoot, _flatItems);
        if (ProjectList != null) { ProjectList.ItemsSource = null; ProjectList.ItemsSource = _flatItems; }
        if (ProjectGrid != null)
        {
            _gridFolderItems.Clear();
            foreach (var it in BuildGridFolderItems(viewRoot))
                _gridFolderItems.Add(it);
            ProjectGrid.ItemsSource = null;
            ProjectGrid.ItemsSource = _gridFolderItems;
        }
        UpdateGridBreadcrumb();
    }

    private IEnumerable<ProjectExplorerItem> BuildGridFolderItems(ProjectExplorerItem? viewRoot)
    {
        _ = viewRoot;
        if (_rootFull == null || string.IsNullOrEmpty(_projectDirectory)) yield break;
        var folder = string.IsNullOrEmpty(_gridCurrentFolderPath) ? _projectDirectory : _gridCurrentFolderPath;
        if (!folder.StartsWith(_projectDirectory, StringComparison.OrdinalIgnoreCase))
            folder = _projectDirectory;
        if (!Directory.Exists(folder))
            folder = _projectDirectory;
        var node = FindItemByPath(_rootFull, folder);
        if (node == null || !node.IsFolder)
        {
            folder = _projectDirectory;
            _gridCurrentFolderPath = folder;
            node = FindItemByPath(_rootFull, folder);
        }
        if (node == null) yield break;
        foreach (var c in node.Children)
        {
            if (c.IsFolder)
            {
                yield return c;
                continue;
            }
            if (_sceneUsedPaths != null && _sceneFilterMode != 0)
            {
                var used = _sceneUsedPaths.Contains(c.FullPath);
                if (_sceneFilterMode == 1 && !used) continue;
                if (_sceneFilterMode == 2 && used) continue;
            }
            if (MatchesFilter(c) && MatchesSearch(c))
                yield return c;
        }
    }

    private void UpdateGridBreadcrumb()
    {
        if (TxtGridFolder == null || BtnGridUp == null) return;
        if (string.IsNullOrEmpty(_projectDirectory))
        {
            TxtGridFolder.Text = "";
            BtnGridUp.IsEnabled = false;
            return;
        }
        var folder = string.IsNullOrEmpty(_gridCurrentFolderPath) ? _projectDirectory : _gridCurrentFolderPath;
        if (!folder.StartsWith(_projectDirectory, StringComparison.OrdinalIgnoreCase))
            folder = _projectDirectory;
        var rel = string.Equals(folder, _projectDirectory, StringComparison.OrdinalIgnoreCase)
            ? "(raíz)"
            : Path.GetRelativePath(_projectDirectory, folder).Replace('\\', '/');
        TxtGridFolder.Text = rel;
        BtnGridUp.IsEnabled = !string.Equals(folder, _projectDirectory, StringComparison.OrdinalIgnoreCase);
    }

    private void NavigateGridToFolder(string absoluteFolderPath)
    {
        if (string.IsNullOrEmpty(_projectDirectory)) return;
        var p = absoluteFolderPath ?? _projectDirectory;
        if (!p.StartsWith(_projectDirectory, StringComparison.OrdinalIgnoreCase))
            p = _projectDirectory;
        if (!Directory.Exists(p))
            p = _projectDirectory;
        _gridCurrentFolderPath = p;
        if (_rootFull != null)
        {
            var viewRoot = FilterAndClone(_rootFull);
            UpdateFlatList(viewRoot);
        }
        else
            RefreshTree();
    }

    private void BtnGridRoot_OnClick(object sender, RoutedEventArgs e)
    {
        NavigateGridToFolder(_projectDirectory);
    }

    private void BtnGridUp_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_projectDirectory)) return;
        var folder = string.IsNullOrEmpty(_gridCurrentFolderPath) ? _projectDirectory : _gridCurrentFolderPath;
        if (string.Equals(folder, _projectDirectory, StringComparison.OrdinalIgnoreCase)) return;
        var parent = Path.GetDirectoryName(folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrEmpty(parent) || !parent.StartsWith(_projectDirectory, StringComparison.OrdinalIgnoreCase))
            parent = _projectDirectory;
        NavigateGridToFolder(parent);
    }

    private static void ClearTreeViewSelection(System.Windows.Controls.ItemsControl parent)
    {
        foreach (var item in parent.Items)
        {
            if (parent.ItemContainerGenerator.ContainerFromItem(item) is System.Windows.Controls.TreeViewItem tvi)
            {
                tvi.IsSelected = false;
                ClearTreeViewSelection(tvi);
            }
        }
    }

    public void SetViewMode(string mode)
    {
        _viewMode = mode ?? "Tree";
        if (ProjectTree != null) ProjectTree.Visibility = _viewMode == "Tree" ? Visibility.Visible : Visibility.Collapsed;
        if (ProjectList != null) ProjectList.Visibility = _viewMode == "List" ? Visibility.Visible : Visibility.Collapsed;
        if (ProjectGridScroll != null) ProjectGridScroll.Visibility = _viewMode == "Grid" ? Visibility.Visible : Visibility.Collapsed;
        if (GridBreadcrumbPanel != null)
            GridBreadcrumbPanel.Visibility = _viewMode == "Grid" ? Visibility.Visible : Visibility.Collapsed;
        if (_viewMode == "Grid" && !string.IsNullOrEmpty(_projectDirectory))
        {
            if (string.IsNullOrEmpty(_gridCurrentFolderPath) || !Directory.Exists(_gridCurrentFolderPath))
                _gridCurrentFolderPath = _projectDirectory;
            if (_rootFull != null)
            {
                var viewRoot = FilterAndClone(_rootFull);
                UpdateFlatList(viewRoot);
            }
        }
        UpdateGridBreadcrumb();
    }

    private void ProjectList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProjectList?.SelectedItem is ProjectExplorerItem item)
            SelectionChanged?.Invoke(this, item);
        else
        {
            SelectionChanged?.Invoke(this, null);
            UpdatePreview(null);
        }
    }

    private void ProjectList_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not System.Windows.Controls.ListBox lb) return;
        var pos = e.GetPosition(lb);
        var hit = VisualTreeHelper.HitTest(lb, pos);
        DependencyObject? d = hit?.VisualHit;
        while (d != null && d is not System.Windows.Controls.ListBoxItem)
            d = VisualTreeHelper.GetParent(d);
        if (d is System.Windows.Controls.ListBoxItem) return;
        lb.SelectedItem = null;
        SelectionChanged?.Invoke(this, null);
        UpdatePreview(null);
    }

    private void ProjectList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ProjectList?.SelectedItem is ProjectExplorerItem item)
            OpenInEditor(item);
    }

    private void ProjectGridItem_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not System.Windows.FrameworkElement fe || fe.DataContext is not ProjectExplorerItem item) return;
        SelectionChanged?.Invoke(this, item);
        UpdatePreview(item);
        if (e.ClickCount != 2) return;
        if (item.IsFolder && !string.IsNullOrEmpty(item.FullPath))
        {
            NavigateGridToFolder(item.FullPath);
            e.Handled = true;
            return;
        }
        TryOpenExplorerItem(item);
    }

    private void ProjectGridScroll_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (ProjectGrid == null) return;
        var pos = e.GetPosition(ProjectGrid);
        var hit = VisualTreeHelper.HitTest(ProjectGrid, pos);
        if (hit?.VisualHit == null) return;
        DependencyObject? d = hit.VisualHit;
        while (d != null && d != ProjectGrid)
        {
            if (d is System.Windows.FrameworkElement { DataContext: ProjectExplorerItem })
                return;
            d = VisualTreeHelper.GetParent(d);
        }
        SelectionChanged?.Invoke(this, null);
        UpdatePreview(null);
    }

    public void SetModified(string fullPath, bool modified)
    {
        if (_rootFull == null) return;
        SetModifiedRecursive(_rootFull, fullPath, modified);
        var viewRoot = FilterAndClone(_rootFull);
        ProjectTree.ItemsSource = viewRoot != null ? new[] { viewRoot } : null;
        UpdateFlatList(viewRoot);
    }

    private ProjectExplorerItem? FilterAndClone(ProjectExplorerItem? source)
    {
        if (source == null) return null;
        var copy = new ProjectExplorerItem
        {
            Name = source.Name,
            FullPath = source.FullPath,
            IsFolder = source.IsFolder,
            FileType = source.FileType,
            Icon = source.Icon,
            IsModified = source.IsModified,
            IsMissing = source.IsMissing,
            Tags = source.Tags != null ? new List<string>(source.Tags) : null,
            Color = source.Color,
            Rating = source.Rating,
            CustomMetadata = source.CustomMetadata != null ? new Dictionary<string, string>(source.CustomMetadata) : null,
            IsLocked = source.IsLocked,
            IsProjectManifestFile = source.IsProjectManifestFile
        };
        if (_metadataService != null && !string.IsNullOrEmpty(source.FullPath))
        {
            var meta = _metadataService.GetAssetMeta(source.FullPath);
            if (meta != null)
            {
                copy.Tags = meta.Tags != null ? new List<string>(meta.Tags) : null;
                copy.Color = meta.Color;
                copy.Rating = meta.Rating;
                copy.CustomMetadata = meta.CustomMetadata != null ? new Dictionary<string, string>(meta.CustomMetadata) : null;
                copy.IsLocked = meta.IsLocked;
            }
        }
        if (_sceneUsedPaths != null && !source.IsFolder && _sceneFilterMode != 0)
        {
            var used = _sceneUsedPaths.Contains(source.FullPath);
            if (_sceneFilterMode == 1 && !used) return null;
            if (_sceneFilterMode == 2 && used) return null;
        }
        foreach (var c in source.Children)
        {
            var childCopy = FilterAndClone(c);
            if (childCopy != null)
                copy.Children.Add(childCopy);
        }
        if (source.IsFolder)
        {
            if (string.Equals(source.FullPath, _projectDirectory, StringComparison.OrdinalIgnoreCase))
                return copy;
            return copy.Children.Count > 0 ? copy : null;
        }
        return MatchesFilter(source) && MatchesSearch(source) ? copy : null;
    }

    private void SetModifiedRecursive(ProjectExplorerItem node, string fullPath, bool modified)
    {
        if (string.Equals(node.FullPath, fullPath, StringComparison.OrdinalIgnoreCase))
        {
            node.IsModified = modified;
            return;
        }
        foreach (var c in node.Children)
            SetModifiedRecursive(c, fullPath, modified);
    }

    /// <summary>Indica si la ruta está marcada como modificada (sin guardar).</summary>
    public bool IsPathModified(string fullPath)
    {
        if (_rootFull == null || string.IsNullOrEmpty(fullPath)) return false;
        return IsPathModifiedRecursive(_rootFull, fullPath);
    }

    private void TagProjectManifest(ProjectExplorerItem item)
    {
        if (item.IsFolder || string.IsNullOrEmpty(item.FullPath)) return;
        if (ProjectManifestPaths.IsActiveProjectManifestFile(item.FullPath, _projectDirectory))
        {
            item.IsProjectManifestFile = true;
            if (item.FileType == ProjectFileType.Project)
                item.Icon = "⚙";
        }
    }

    private static bool IsPathModifiedRecursive(ProjectExplorerItem node, string fullPath)
    {
        if (string.Equals(node.FullPath, fullPath, StringComparison.OrdinalIgnoreCase))
            return node.IsModified;
        foreach (var c in node.Children)
        {
            if (IsPathModifiedRecursive(c, fullPath)) return true;
        }
        return false;
    }

    /// <summary>Orden preferido de carpetas en la raíz del proyecto.</summary>
    private static readonly string[] PreferredFolderOrder = { "Assets", "Scenes", "Maps", "Scripts", "Seeds" };

    /// <summary>Orden preferido de archivos en la raíz del proyecto.</summary>
    private static readonly string[] PreferredFileOrder = { "Project.FUE", "Project.json", "Settings.json", "proyecto.json", "mapa.json", "objetos.json", "scripts.json", "animaciones.json" };
    /// <summary>Archivos de configuración interna que no se muestran en el explorador.</summary>
    private static readonly HashSet<string> HiddenFileNames = new(StringComparer.OrdinalIgnoreCase) { "settings.json", "proyecto.config", ".fuengine-editor.json" };

    private ProjectExplorerItem BuildTree()
    {
        var root = new ProjectExplorerItem
        {
            Name = _projectName,
            FullPath = _projectDirectory,
            IsFolder = true,
            FileType = ProjectFileType.Folder,
            Icon = "📁"
        };
        if (string.IsNullOrEmpty(_projectDirectory) || !Directory.Exists(_projectDirectory))
            return root;

        var rootFiles = new List<(string path, string name, ProjectFileType type, string icon)>();
        foreach (var file in Directory.GetFiles(_projectDirectory))
        {
            var name = Path.GetFileName(file);
            if (name == null || HiddenFileNames.Contains(name)) continue;
            var ext = Path.GetExtension(file);
            var type = name.ToLowerInvariant() switch
            {
                "project.fue" => ProjectFileType.Project,
                "project.json" => ProjectFileType.Project,
                "proyecto.json" => ProjectFileType.Project,
                "settings.json" => ProjectFileType.Generic,
                "mapa.json" => ProjectFileType.Map,
                "objetos.json" => ProjectFileType.Objects,
                "scripts.json" => ProjectFileType.Scripts,
                "animaciones.json" => ProjectFileType.Animations,
                _ when ext == ".png" || ext == ".jpg" || ext == ".jpeg" => ProjectFileType.Sprite,
                _ when string.Equals(ext, ".seed", StringComparison.OrdinalIgnoreCase) => ProjectFileType.Seed,
                _ when ext == ".json" => ProjectFileType.Generic,
                _ when ext == ".scene" => ProjectFileType.Scene,
                _ when ext == ".wav" || ext == ".ogg" || ext == ".mp3" => ProjectFileType.Sound,
                _ => ProjectFileType.Generic
            };
            var icon = type switch
            {
                ProjectFileType.Project => "📋",
                ProjectFileType.Map => "🗺",
                ProjectFileType.Objects => "📦",
                ProjectFileType.Scripts => "📜",
                ProjectFileType.Animations => "🎬",
                ProjectFileType.Scene => "🎞",
                ProjectFileType.Sprite => "🖼",
                ProjectFileType.Sound => "🔊",
                ProjectFileType.Seed => "🌱",
                _ => name.EndsWith("Settings.json", StringComparison.OrdinalIgnoreCase) ? "⚙" : "📄"
            };
            rootFiles.Add((file, name, type, icon));
        }

        var rootDirs = new List<(string path, string name)>();
        foreach (var dir in Directory.GetDirectories(_projectDirectory))
        {
            var name = Path.GetFileName(dir);
            if (name == null || name.StartsWith(".", StringComparison.Ordinal)) continue;
            if (HideDataFolderInExplorer && string.Equals(name, "Data", StringComparison.OrdinalIgnoreCase))
                continue;
            rootDirs.Add((dir, name));
        }

        SortAndAddFolders(root, rootDirs);
        SortAndAddRootFiles(root, rootFiles);
        return root;
    }

    private void SortAndAddFolders(ProjectExplorerItem root, List<(string path, string name)> dirs)
    {
        int OrderKey(string name)
        {
            var idx = Array.FindIndex(PreferredFolderOrder, s => string.Equals(s, name, StringComparison.OrdinalIgnoreCase));
            return idx >= 0 ? idx : PreferredFolderOrder.Length + string.Compare(name, "", StringComparison.OrdinalIgnoreCase);
        }
        foreach (var (path, name) in dirs.OrderBy(d => OrderKey(d.name)).ThenBy(d => d.name, StringComparer.OrdinalIgnoreCase))
        {
            var folderNode = new ProjectExplorerItem
            {
                Name = name,
                FullPath = path,
                IsFolder = true,
                FileType = ProjectFileType.Folder,
                Icon = "📁"
            };
            AddFolderContents(folderNode, path);
            root.Children.Add(folderNode);
        }
    }

    private void SortAndAddRootFiles(ProjectExplorerItem root, List<(string path, string name, ProjectFileType type, string icon)> rootFiles)
    {
        int OrderKey(string name)
        {
            var idx = Array.FindIndex(PreferredFileOrder, s => string.Equals(s, name, StringComparison.OrdinalIgnoreCase));
            return idx >= 0 ? idx : PreferredFileOrder.Length + string.Compare(name, "", StringComparison.OrdinalIgnoreCase);
        }
        foreach (var (path, name, type, icon) in rootFiles.OrderBy(f => OrderKey(f.name)).ThenBy(f => f.name, StringComparer.OrdinalIgnoreCase))
        {
            var fileItem = new ProjectExplorerItem
            {
                Name = name,
                FullPath = path,
                IsFolder = false,
                FileType = type,
                Icon = icon,
                IsMissing = !File.Exists(path)
            };
            TagProjectManifest(fileItem);
            root.Children.Add(fileItem);
        }
    }

    private void AddFolderContents(ProjectExplorerItem folder, string dirPath)
    {
        try
        {
            var parentName = Path.GetFileName(dirPath);
            var isMapsFolder = string.Equals(parentName, "Maps", StringComparison.OrdinalIgnoreCase);
            var isDataFolder = string.Equals(parentName, "Data", StringComparison.OrdinalIgnoreCase);
            var fileList = new List<(string path, string name, ProjectFileType type, string icon)>();
            foreach (var file in Directory.GetFiles(dirPath))
            {
                var name = Path.GetFileName(file);
                if (name == null || HiddenFileNames.Contains(name)) continue;
                var ext = Path.GetExtension(file).ToLowerInvariant();
                var type = ext switch
                {
                    ".png" or ".jpg" or ".jpeg" => ProjectFileType.Sprite,
                    ".wav" or ".ogg" or ".mp3" => ProjectFileType.Sound,
                    ".json" when isMapsFolder => ProjectFileType.Map,
                    ".json" => ProjectFileType.Generic,
                    ".scene" => ProjectFileType.Scene,
                    ".seed" => ProjectFileType.Seed,
                    _ => ProjectFileType.Generic
                };
                if (isDataFolder && ext == ".json")
                {
                    type = name.ToLowerInvariant() switch
                    {
                        "scripts.json" => ProjectFileType.Scripts,
                        "animaciones.json" => ProjectFileType.Animations,
                        "seeds.json" => ProjectFileType.Generic,
                        "audio.json" => ProjectFileType.Generic,
                        "triggerzones.json" => ProjectFileType.Generic,
                        _ => type
                    };
                }
                var icon = type == ProjectFileType.Map ? "🗺" : type switch
                {
                    ProjectFileType.Scene => "🎞",
                    ProjectFileType.Scripts => "📜",
                    ProjectFileType.Animations => "🎬",
                    ProjectFileType.Sprite => "🖼",
                    ProjectFileType.Sound => "🔊",
                    ProjectFileType.Seed => "🌱",
                    _ => "📄"
                };
                fileList.Add((file, name, type, icon));
            }
            foreach (var (path, name, type, icon) in fileList.OrderBy(f => f.name, StringComparer.OrdinalIgnoreCase))
            {
                var fileItem = new ProjectExplorerItem
                {
                    Name = name,
                    FullPath = path,
                    IsFolder = false,
                    FileType = type,
                    Icon = icon,
                    IsMissing = !File.Exists(path)
                };
                TagProjectManifest(fileItem);
                folder.Children.Add(fileItem);
            }
            var subDirs = Directory.GetDirectories(dirPath)
                .Select(sub => (path: sub, name: Path.GetFileName(sub)))
                .Where(t => t.name != null && !t.name.StartsWith(".", StringComparison.Ordinal))
                .OrderBy(t => t.name, StringComparer.OrdinalIgnoreCase);
            foreach (var (path, name) in subDirs)
            {
                if (name == null) continue;
                var subFolder = new ProjectExplorerItem
                {
                    Name = name,
                    FullPath = path,
                    IsFolder = true,
                    FileType = ProjectFileType.Folder,
                    Icon = "📁"
                };
                AddFolderContents(subFolder, path);
                folder.Children.Add(subFolder);
            }
        }
        catch { /* ignore access errors */ }
    }

    private bool MatchesFilter(ProjectExplorerItem item)
    {
        if (_filterIndex == 0) return true;
        if (item.IsFolder) return true;
        return _filterIndex switch
        {
            1 => item.FileType == ProjectFileType.Map,
            2 => item.FileType == ProjectFileType.Objects || item.FileType == ProjectFileType.Sprite,
            3 => item.FileType == ProjectFileType.Scripts,
            4 => item.FileType == ProjectFileType.Animations,
            5 => item.FileType == ProjectFileType.TileSet || (item.FileType == ProjectFileType.Generic && (item.FullPath?.Contains("Tileset", StringComparison.OrdinalIgnoreCase) == true || item.Name?.Contains("tileset", StringComparison.OrdinalIgnoreCase) == true)),
            6 => item.FileType == ProjectFileType.Seed
                 || (item.FileType == ProjectFileType.Generic && (item.FullPath != null && item.FullPath.Replace("\\", "/").IndexOf("/Seeds/", StringComparison.OrdinalIgnoreCase) >= 0 || item.Name?.StartsWith("seed", StringComparison.OrdinalIgnoreCase) == true)),
            _ => true
        };
    }

    private bool MatchesSearch(ProjectExplorerItem item)
    {
        if (string.IsNullOrWhiteSpace(_searchText)) return true;
        return item.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase);
    }

    private void TxtSearch_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = TxtSearch?.Text?.Trim() ?? "";
        RefreshTree();
    }

    private void CmbFilter_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _filterIndex = CmbFilter?.SelectedIndex ?? 0;
        RefreshTree();
    }

    private void ProjectTree_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        var item = ProjectTree?.SelectedItem as ProjectExplorerItem;
        SelectionChanged?.Invoke(this, item);
        UpdatePreview(item);
    }

    private void ProjectTree_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var item = GetSelectedItem();
        TryOpenExplorerItem(item);
    }

    /// <summary>Abre en el editor según tipo (misma lógica que doble clic en árbol).</summary>
    public void TryOpenExplorerItem(ProjectExplorerItem? item)
    {
        if (item == null || item.IsFolder || string.IsNullOrEmpty(item.FullPath)) return;
        var ext = Path.GetExtension(item.FullPath).ToLowerInvariant();
        if (ext == ".fue") return;
        if (ext == ".seed")
        {
            _metadataService?.RecordRecent(item.FullPath);
            RequestOpenInEditor?.Invoke(this, item);
            return;
        }
        _metadataService?.RecordRecent(item.FullPath);
        if (CreativeSuiteMetadata.IsImagePath(item.FullPath))
        {
            RequestOpenInEditor?.Invoke(this, item);
            return;
        }
        bool isEditable = ext == ".json" || ext == ".cs" || ext == ".txt" || ext == ".js" ||
                          item.FileType == ProjectFileType.Scripts || item.FileType == ProjectFileType.Project ||
                          item.FileType == ProjectFileType.Map || item.FileType == ProjectFileType.Objects ||
                          item.FileType == ProjectFileType.Animations || item.FileType == ProjectFileType.Generic;
        if (isEditable)
            RequestOpenInEditor?.Invoke(this, item);
    }

    private string? _previewSoundPath;

    private void UpdatePreview(ProjectExplorerItem? item)
    {
        if (ImgPreview != null) { ImgPreview.Source = null; ImgPreview.Visibility = Visibility.Collapsed; }
        if (TxtPreviewPlaceholder != null) TxtPreviewPlaceholder.Visibility = Visibility.Visible;
        if (PreviewSoundPanel != null) PreviewSoundPanel.Visibility = Visibility.Collapsed;
        if (PreviewScriptScroll != null) PreviewScriptScroll.Visibility = Visibility.Collapsed;
        _previewSoundPath = null;

        if (item == null)
        {
            if (TxtPreviewTitle != null) TxtPreviewTitle.Text = "Vista previa";
            if (TxtPreviewPlaceholder != null) { TxtPreviewPlaceholder.Text = "Seleccione un archivo"; }
            return;
        }
        if (TxtPreviewTitle != null) TxtPreviewTitle.Text = item.Name;

        var isImage = !string.IsNullOrEmpty(item.FullPath) && (item.FileType == ProjectFileType.Sprite || item.FileType == ProjectFileType.TileSet ||
                      new[] { ".png", ".jpg", ".jpeg" }.Contains(Path.GetExtension(item.FullPath).ToLowerInvariant()));
        if (isImage && !item.IsFolder && File.Exists(item.FullPath))
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(item.FullPath, UriKind.Absolute);
                bmp.DecodePixelWidth = 64;
                bmp.EndInit();
                bmp.Freeze();
                if (ImgPreview != null) { ImgPreview.Source = bmp; ImgPreview.Visibility = Visibility.Visible; }
                if (TxtPreviewPlaceholder != null) TxtPreviewPlaceholder.Visibility = Visibility.Collapsed;
            }
            catch { /* fallback to text */ }
        }

        if (!string.IsNullOrEmpty(item.FullPath) && item.FileType == ProjectFileType.Sound && !item.IsFolder && File.Exists(item.FullPath))
        {
            _previewSoundPath = item.FullPath;
            if (PreviewSoundPanel != null) { PreviewSoundPanel.Visibility = Visibility.Visible; }
            if (TxtPreviewPlaceholder != null) TxtPreviewPlaceholder.Visibility = Visibility.Collapsed;
        }

        var ext = Path.GetExtension(item.FullPath ?? "").ToLowerInvariant();
        var isScriptOrJson = item.FileType == ProjectFileType.Scripts || item.FileType == ProjectFileType.Generic && ext == ".json";
        if (isScriptOrJson && !item.IsFolder && !string.IsNullOrEmpty(item.FullPath) && File.Exists(item.FullPath))
        {
            try
            {
                var lines = File.ReadAllLines(item.FullPath).Take(15);
                var text = string.Join(Environment.NewLine, lines);
                if (TxtPreviewScript != null) { TxtPreviewScript.Text = text; }
                if (PreviewScriptScroll != null) { PreviewScriptScroll.Visibility = Visibility.Visible; }
                if (TxtPreviewPlaceholder != null) TxtPreviewPlaceholder.Visibility = Visibility.Collapsed;
            }
            catch { /* ignore */ }
        }

        var isSeed = ext == ".json" && (item.FullPath != null && item.FullPath.Replace("\\", "/").IndexOf("/Seeds/", StringComparison.OrdinalIgnoreCase) >= 0 || item.Name?.StartsWith("seed", StringComparison.OrdinalIgnoreCase) == true);
        if (isSeed && TxtPreviewPlaceholder != null && TxtPreviewPlaceholder.Visibility == Visibility.Visible)
        {
            TxtPreviewPlaceholder.Text = "Seed (objeto reutilizable)";
        }

        if (item.FileType == ProjectFileType.Animations || (!string.IsNullOrEmpty(item.FullPath) && item.FullPath.EndsWith("animaciones.json", StringComparison.OrdinalIgnoreCase)))
        {
            if (TxtPreviewPlaceholder != null && TxtPreviewPlaceholder.Visibility == Visibility.Visible)
                TxtPreviewPlaceholder.Text = "Preview de animación (próximamente)";
            // Si se implementa preview real con frames/timers, usar DispatcherTimer o background thread para no bloquear la UI.
        }

        if (TxtPreviewPlaceholder != null && TxtPreviewPlaceholder.Visibility == Visibility.Visible)
        {
            TxtPreviewPlaceholder.Text = item.IsFolder
                ? $"{item.Children.Count} elementos"
                : item.FileType switch
                {
                    ProjectFileType.Map => "Mapa (chunks y tiles)",
                    ProjectFileType.Objects => "Definiciones e instancias de objetos",
                    ProjectFileType.Scripts => "Scripts y eventos",
                    ProjectFileType.Animations => "Animaciones por frames",
                    ProjectFileType.Scene => "Descriptor de escena (.scene)",
                    ProjectFileType.Sprite => "Imagen / sprite",
                    ProjectFileType.TileSet => "TileSet",
                    ProjectFileType.Seed => "Seed prefab (.seed)",
                    _ => Path.GetFileName(item.FullPath ?? "")
                };
        }
    }

    private void BtnPreviewPlay_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_previewSoundPath) || !File.Exists(_previewSoundPath)) return;
        try
        {
            var player = new System.Windows.Media.MediaPlayer();
            player.Open(new Uri(_previewSoundPath, UriKind.Absolute));
            player.Play();
        }
        catch { /* ignore */ }
    }

    private void ProjectTree_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.TreeView tv)
        {
            var pos = e.GetPosition(tv);
            var hit = VisualTreeHelper.HitTest(tv, pos);
            DependencyObject? d = hit?.VisualHit;
            while (d != null && d is not System.Windows.Controls.TreeViewItem)
                d = VisualTreeHelper.GetParent(d);
            if (d is not System.Windows.Controls.TreeViewItem)
            {
                ClearTreeViewSelection(tv);
                SelectionChanged?.Invoke(this, null);
                UpdatePreview(null);
                _dragStartItem = null;
                _dragStartPoint = null;
                return;
            }
        }
        _dragStartItem = ProjectTree?.SelectedItem as ProjectExplorerItem;
        _dragStartPoint = e.GetPosition(ProjectTree);
    }

    private void ProjectTree_OnPreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragStartItem == null || _dragStartPoint == null || ProjectTree == null) return;
        var pos = e.GetPosition(ProjectTree);
        if (Math.Abs(pos.X - _dragStartPoint.Value.X) < 4 && Math.Abs(pos.Y - _dragStartPoint.Value.Y) < 4) return;
        _dragStartPoint = null;
        var paths = new List<string>();
        if (_dragStartItem.IsFolder)
            CollectPathsRecursive(_dragStartItem, paths);
        else
        {
            if (!string.IsNullOrEmpty(_dragStartItem.FullPath))
                paths.Add(_dragStartItem.FullPath);
        }
        if (paths.Count == 0) return;
        var data = new System.Windows.DataObject();
        data.SetData(DataFormatExplorerItem, paths.ToArray());
        if (!_dragStartItem.IsFolder && !string.IsNullOrEmpty(_dragStartItem.FullPath) && IsAssetPath(_dragStartItem))
            data.SetData(DataFormatAssetPath, _dragStartItem.FullPath);
        try
        {
            System.Windows.DragDrop.DoDragDrop(ProjectTree, data, System.Windows.DragDropEffects.Copy | System.Windows.DragDropEffects.Move);
        }
        finally
        {
            _dragStartItem = null;
        }
        e.Handled = true;
    }

    private static void CollectPathsRecursive(ProjectExplorerItem node, List<string> paths)
    {
        if (!node.IsFolder)
        {
            if (!string.IsNullOrEmpty(node.FullPath)) paths.Add(node.FullPath);
            return;
        }
        foreach (var c in node.Children)
            CollectPathsRecursive(c, paths);
    }

    private static bool IsAssetPath(ProjectExplorerItem item)
    {
        if (item.IsFolder || string.IsNullOrEmpty(item.FullPath)) return false;
        var ext = Path.GetExtension(item.FullPath).ToLowerInvariant();
        var name = Path.GetFileName(item.FullPath).ToLowerInvariant();
        if (ext == ".json" && (name.StartsWith("seed") || item.FullPath.Replace("\\", "/").IndexOf("/Seeds/", StringComparison.OrdinalIgnoreCase) >= 0 || name.Contains("tileset"))) return true;
        if (ext == ".seed") return true;
        if (ext == ".png" || ext == ".jpg" || ext == ".jpeg") return true;
        return false;
    }

    private void ProjectTree_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var dep = e.OriginalSource as System.Windows.DependencyObject;
        while (dep != null)
        {
            if (dep is System.Windows.Controls.TreeViewItem tvi)
            {
                tvi.IsSelected = true;
                tvi.Focus();
                break;
            }
            dep = System.Windows.Media.VisualTreeHelper.GetParent(dep);
        }
    }

    private void ProjectTree_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (ProjectTree == null) return;
        var item = ProjectTree.SelectedItem as ProjectExplorerItem;
        if (item == null)
        {
            var pt = e.GetPosition(ProjectTree);
            var hit = System.Windows.Media.VisualTreeHelper.HitTest(ProjectTree, pt);
            if (hit?.VisualHit is System.Windows.DependencyObject dep)
            {
                var tvi = FindParentTreeViewItem(dep);
                if (tvi != null)
                {
                    tvi.IsSelected = true;
                    item = ProjectTree.SelectedItem as ProjectExplorerItem;
                }
            }
            if (item == null && ProjectTree.Items.Count > 0)
                item = ProjectTree.Items[0] as ProjectExplorerItem;
        }
        var menu = BuildExplorerContextMenu(item);
        if (menu != null)
        {
            ProjectTree.ContextMenu = menu;
            menu.PlacementTarget = ProjectTree;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
            menu.IsOpen = true;
            e.Handled = true;
        }
    }

    private static System.Windows.Controls.TreeViewItem? FindParentTreeViewItem(System.Windows.DependencyObject? dep)
    {
        while (dep != null)
        {
            if (dep is System.Windows.Controls.TreeViewItem tvi) return tvi;
            dep = System.Windows.Media.VisualTreeHelper.GetParent(dep);
        }
        return null;
    }

    private void ProjectTree_OnContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (ProjectTree == null) { e.Handled = true; return; }
        var item = ProjectTree.SelectedItem as ProjectExplorerItem;
        var menu = BuildExplorerContextMenu(item);
        if (menu != null)
            ProjectTree.ContextMenu = menu;
    }

    private ContextMenu? BuildExplorerContextMenu(ProjectExplorerItem? item)
    {
        var menu = new ContextMenu
        {
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x21, 0x26, 0x2d)),
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xe6, 0xed, 0xf3)),
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x30, 0x36, 0x3d)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(2),
            HasDropShadow = false
        };
        menu.Items.Clear();
        var isFolder = item?.IsFolder ?? false;
        var targetDir = item != null ? (isFolder ? item.FullPath : (string.IsNullOrEmpty(item.FullPath) ? _projectDirectory : Path.GetDirectoryName(item.FullPath) ?? _projectDirectory)) : _projectDirectory;

        if (isFolder || item == null)
        {
            var nuevo = new MenuItem { Header = "Nuevo" };
            var miCarpeta = new MenuItem { Header = "Carpeta" };
            miCarpeta.Click += (_, _) => CreateNewFolder(targetDir);
            nuevo.Items.Add(miCarpeta);
            var miPixelGroup = new MenuItem { Header = "Nuevo Pixel/Tile Group" };
            miPixelGroup.Click += (_, _) => System.Windows.MessageBox.Show("Crear grupo: usa la Jerarquía del mapa para añadir grupos.", "Explorador", System.Windows.MessageBoxButton.OK);
            nuevo.Items.Add(miPixelGroup);
            var miTriggerZone = new MenuItem { Header = "Nuevo Trigger Zone" };
            miTriggerZone.Click += (_, _) => RequestCreateTriggerZone?.Invoke(this, EventArgs.Empty);
            nuevo.Items.Add(miTriggerZone);
            var miTileSet = new MenuItem { Header = "Archivo TileSet" };
            miTileSet.Click += (_, _) => CreateNewTileSet(targetDir);
            nuevo.Items.Add(miTileSet);
            var miSeed = new MenuItem { Header = "Seed" };
            miSeed.Click += (_, _) => CreateNewSeed(targetDir);
            nuevo.Items.Add(miSeed);
            var miScript = new MenuItem { Header = "Script JSON" };
            miScript.Click += (_, _) => CreateNewScriptJson(targetDir);
            nuevo.Items.Add(miScript);
            var miLua = new MenuItem { Header = "Script Lua (.lua)" };
            miLua.Click += (_, _) => CreateNewLuaScript(targetDir);
            nuevo.Items.Add(miLua);
            var miSonido = new MenuItem { Header = "Sonido" };
            var miSonidoImport = new MenuItem { Header = "Importar archivo de audio..." };
            miSonidoImport.Click += (_, _) => CreateSoundFromFile(targetDir);
            miSonido.Items.Add(miSonidoImport);
            var miSonidoRecord = new MenuItem { Header = "Grabar con micrófono..." };
            miSonidoRecord.Click += (_, _) => CreateSoundFromMicrophone(targetDir);
            miSonido.Items.Add(miSonidoRecord);
            nuevo.Items.Add(miSonido);
            menu.Items.Add(nuevo);
            menu.Items.Add(new Separator());
        }

        if (item != null)
        {
            if (!item.IsFolder)
            {
                var miOpen = new MenuItem { Header = "Abrir" };
                miOpen.Click += (_, _) => { if (!string.IsNullOrEmpty(item.FullPath)) _metadataService?.RecordRecent(item.FullPath); RequestOpenInEditor?.Invoke(this, item); };
                menu.Items.Add(miOpen);
                if (CreativeSuiteMetadata.IsImagePath(item.FullPath))
                {
                    var miCollisions = new MenuItem { Header = "Editar colisiones..." };
                    miCollisions.Click += (_, _) =>
                    {
                        if (!string.IsNullOrEmpty(item.FullPath)) _metadataService?.RecordRecent(item.FullPath);
                        RequestOpenInCollisionsEditor?.Invoke(this, item);
                    };
                    menu.Items.Add(miCollisions);
                }
                if (string.Equals(Path.GetExtension(item.FullPath), ".lua", StringComparison.OrdinalIgnoreCase))
                {
                    var miScriptableTile = new MenuItem { Header = "Abrir en Tile por script" };
                    miScriptableTile.Click += (_, _) =>
                    {
                        if (!string.IsNullOrEmpty(item.FullPath)) _metadataService?.RecordRecent(item.FullPath);
                        RequestOpenInScriptableTile?.Invoke(this, item);
                    };
                    menu.Items.Add(miScriptableTile);
                }
            }
            if (_metadataService != null && !string.IsNullOrEmpty(item.FullPath))
            {
                var isFav = _metadataService.IsFavorite(item.FullPath);
                var miFav = new MenuItem { Header = isFav ? "Quitar de favoritos" : "Añadir a favoritos" };
                miFav.Click += (_, _) => { if (isFav) _metadataService.RemoveFavorite(item.FullPath); else _metadataService.AddFavorite(item.FullPath); RefreshQuickLists(); };
                menu.Items.Add(miFav);
            }
            var miDup = new MenuItem { Header = "Duplicar" };
            miDup.Click += (_, _) => DuplicateItem(item);
            menu.Items.Add(miDup);
            var miDel = new MenuItem { Header = "Eliminar" };
            miDel.Click += (_, _) => DeleteItem(item);
            menu.Items.Add(miDel);
            var miRename = new MenuItem { Header = "Renombrar" };
            miRename.Click += (_, _) => RenameItem(item);
            menu.Items.Add(miRename);
            menu.Items.Add(new Separator());
            var miShow = new MenuItem { Header = "Mostrar en carpeta" };
            miShow.Click += (_, _) => ShowInFolder(item);
            menu.Items.Add(miShow);
            var miProps = new MenuItem { Header = "Propiedades" };
            miProps.Click += (_, _) => ShowProperties(item);
            menu.Items.Add(miProps);
        }

        var miCopyPath = new MenuItem { Header = "Copiar ruta" };
        miCopyPath.Click += (_, _) => { if (item != null && !string.IsNullOrEmpty(item.FullPath)) try { System.Windows.Clipboard.SetText(item.FullPath); } catch { } };
        menu.Items.Add(new Separator());
        menu.Items.Add(miCopyPath);

        return menu;
    }

    private void CreateNewFolder(string parentDir)
    {
        if (string.IsNullOrEmpty(parentDir))
        {
            System.Windows.MessageBox.Show("No hay carpeta de proyecto seleccionada. Abre un proyecto primero.", "Explorador", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!Directory.Exists(parentDir)) return;
        var name = MapHierarchyPanel.ShowRenameDialogPublic("Nueva carpeta", "NuevaCarpeta");
        if (string.IsNullOrWhiteSpace(name)) return;
        name = name.Trim();
        var invalid = Path.GetInvalidFileNameChars();
        if (name.IndexOfAny(invalid) >= 0) { System.Windows.MessageBox.Show("Nombre no válido.", "Explorador", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        var path = Path.Combine(parentDir, name);
        if (Directory.Exists(path)) { System.Windows.MessageBox.Show("La carpeta ya existe.", "Explorador", MessageBoxButton.OK); return; }
        try
        {
            Directory.CreateDirectory(path);
            RefreshTree();
        }
        catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void CreateNewTileSet(string targetDir)
    {
        if (string.IsNullOrEmpty(targetDir))
        {
            System.Windows.MessageBox.Show("No hay carpeta de proyecto seleccionada. Abre un proyecto primero.", "Explorador", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var path = Path.Combine(targetDir, "tileset.json");
        path = GetUniquePath(path);
        try
        {
            File.WriteAllText(path, "{\n  \"id\": \"tileset_1\",\n  \"tiles\": []\n}\n");
            RefreshTree();
        }
        catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void CreateNewSeed(string targetDir)
    {
        if (string.IsNullOrEmpty(targetDir))
        {
            System.Windows.MessageBox.Show("No hay carpeta de proyecto seleccionada. Abre un proyecto primero.", "Explorador", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var path = Path.Combine(targetDir, "seed.json");
        path = GetUniquePath(path);
        try
        {
            File.WriteAllText(path, "{\n  \"id\": \"seed_1\",\n  \"definitionId\": \"\",\n  \"components\": []\n}\n");
            RefreshTree();
        }
        catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void CreateNewScriptJson(string targetDir)
    {
        if (string.IsNullOrEmpty(targetDir))
        {
            System.Windows.MessageBox.Show("No hay carpeta de proyecto seleccionada. Abre un proyecto primero.", "Explorador", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var path = Path.Combine(targetDir, "script.json");
        path = GetUniquePath(path);
        try
        {
            File.WriteAllText(path, "{\n  \"id\": \"script_1\",\n  \"events\": []\n}\n");
            RefreshTree();
        }
        catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void CreateNewLuaScript(string targetDir)
    {
        if (string.IsNullOrEmpty(targetDir))
        {
            System.Windows.MessageBox.Show("No hay carpeta de proyecto seleccionada. Abre un proyecto primero.", "Explorador", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!Directory.Exists(targetDir)) return;
        var input = MapHierarchyPanel.ShowRenameDialogPublic("Nuevo script Lua", "NuevoScript");
        if (string.IsNullOrWhiteSpace(input)) return;
        var name = input.Trim();
        var invalid = Path.GetInvalidFileNameChars();
        if (name.IndexOfAny(invalid) >= 0)
        {
            System.Windows.MessageBox.Show("Nombre no válido.", "Explorador", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!name.EndsWith(".lua", StringComparison.OrdinalIgnoreCase))
            name += ".lua";
        var path = Path.Combine(targetDir, name);
        path = GetUniquePath(path);
        var baseName = Path.GetFileNameWithoutExtension(path);
        try
        {
            File.WriteAllText(path, DefaultLuaScriptTemplate.Format(baseName), System.Text.Encoding.UTF8);
            RefreshTree();
            EditorLog.Info($"Creado {Path.GetFileName(path)} con plantilla onStart/onUpdate/onDestroy.", "Scripts");
            if (!string.IsNullOrEmpty(_projectDirectory))
            {
                if (ScriptRegistryProjectWriter.TryRegisterLuaFile(_projectDirectory, path, out var regId, out var relPath, out var regErr))
                {
                    LuaScriptRegistered?.Invoke(this, new ScriptRegisteredEventArgs(regId, relPath));
                    EditorLog.Info($"Registrado en scripts.json: {relPath} (id={regId}).", "scripts.json");
                }
                else if (!string.IsNullOrEmpty(regErr))
                {
                    EditorLog.Warning($"Script creado pero scripts.json no actualizado: {regErr}", "scripts.json");
                    System.Windows.MessageBox.Show(
                        $"Se creó el archivo, pero no se pudo actualizar scripts.json:\n{regErr}",
                        "Registro de scripts",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CreateSoundFromFile(string targetDir)
    {
        if (string.IsNullOrEmpty(targetDir))
        {
            System.Windows.MessageBox.Show("No hay carpeta de proyecto seleccionada. Abre un proyecto primero.", "Explorador", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!Directory.Exists(targetDir)) return;
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Importar archivo de audio",
            Filter = "Audio|*.wav;*.ogg;*.mp3|WAV|*.wav|OGG|*.ogg|MP3|*.mp3|Todos|*.*"
        };
        if (dlg.ShowDialog() != true || string.IsNullOrEmpty(dlg.FileName)) return;
        try
        {
            var name = Path.GetFileName(dlg.FileName);
            var dest = Path.Combine(targetDir, name);
            dest = GetUniquePath(dest);
            File.Copy(dlg.FileName, dest);
            RefreshTree();
            System.Windows.MessageBox.Show($"Importado: {Path.GetFileName(dest)}", "Sonido", System.Windows.MessageBoxButton.OK);
        }
        catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message, "Error al importar", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error); }
    }

    private void CreateSoundFromMicrophone(string targetDir)
    {
        System.Windows.MessageBox.Show("Grabación con micrófono no disponible en esta versión. Use \"Importar archivo de audio\" o una herramienta externa.", "Sonido", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    private static string GetUniquePath(string path)
    {
        if (!File.Exists(path)) return path;
        var dir = Path.GetDirectoryName(path) ?? "";
        var name = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);
        for (int i = 1; i < 1000; i++)
        {
            var next = Path.Combine(dir, name + "_" + i + ext);
            if (!File.Exists(next)) return next;
        }
        return path;
    }

    public void DuplicateItem(ProjectExplorerItem item)
    {
        if (string.IsNullOrEmpty(item.FullPath)) return;
        string dest;
        if (item.IsFolder)
        {
            dest = item.FullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + "_copia";
            if (Directory.Exists(dest)) dest = GetUniqueFolderPath(dest);
            try
            {
                CopyDirectory(item.FullPath, dest);
                RefreshTree();
            }
            catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
        else
        {
            var dir = Path.GetDirectoryName(item.FullPath) ?? "";
            var baseName = Path.GetFileNameWithoutExtension(item.FullPath);
            var ext = Path.GetExtension(item.FullPath);
            dest = Path.Combine(dir, baseName + "_copia" + ext);
            dest = GetUniquePath(dest);
            try
            {
                File.Copy(item.FullPath, dest);
                RefreshTree();
                if (string.Equals(ext, ".lua", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(_projectDirectory))
                {
                    if (ScriptRegistryProjectWriter.TryRegisterLuaFile(_projectDirectory, dest, out _, out _, out var dupErr))
                        ScriptsRegistryChanged?.Invoke(this, EventArgs.Empty);
                    else if (!string.IsNullOrEmpty(dupErr))
                        EditorLog.Warning($"Duplicado creado pero scripts.json: {dupErr}", "scripts.json");
                }
            }
            catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
    }

    private static string GetUniqueFolderPath(string path)
    {
        if (!Directory.Exists(path)) return path;
        for (int i = 1; i < 1000; i++)
        {
            var next = path + "_" + i;
            if (!Directory.Exists(next)) return next;
        }
        return path;
    }

    private static void CopyDirectory(string src, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(src))
            File.Copy(file, Path.Combine(dest, Path.GetFileName(file)));
        foreach (var sub in Directory.GetDirectories(src))
            CopyDirectory(sub, Path.Combine(dest, Path.GetFileName(sub)!));
    }

    public void RenameItem(ProjectExplorerItem item)
    {
        if (string.IsNullOrEmpty(item.FullPath)) return;
        if (item.IsLocked && System.Windows.MessageBox.Show("Este asset está bloqueado. ¿Desbloquear y renombrar?", "Asset bloqueado", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        // Nota: operaciones por batch/script que llamen a RenameItem sin pasar por UI deben respetar IsLocked si se desea consistencia.
        var name = MapHierarchyPanel.ShowRenameDialogPublic("Renombrar", item.Name);
        if (string.IsNullOrWhiteSpace(name)) return;
        name = name.Trim();
        var invalid = Path.GetInvalidFileNameChars();
        if (name.IndexOfAny(invalid) >= 0) { System.Windows.MessageBox.Show("Nombre no válido.", "Explorador", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        var parent = item.IsFolder ? Path.GetDirectoryName(item.FullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) : Path.GetDirectoryName(item.FullPath);
        if (string.IsNullOrEmpty(parent)) return;
        var dest = item.IsFolder ? Path.Combine(parent, name) : Path.Combine(parent, name + Path.GetExtension(item.FullPath));
        if (string.Equals(item.FullPath, dest, StringComparison.OrdinalIgnoreCase)) return;
        if (File.Exists(dest) || Directory.Exists(dest)) { System.Windows.MessageBox.Show("Ya existe un elemento con ese nombre.", "Explorador", MessageBoxButton.OK); return; }
        var oldPath = item.FullPath;
        try
        {
            if (item.IsFolder) Directory.Move(oldPath, dest);
            else File.Move(oldPath, dest);
            RefreshTree();
            if (!string.IsNullOrEmpty(_projectDirectory))
            {
                if (item.IsFolder)
                {
                    if (ScriptRegistryProjectWriter.TryRenameFolderInRegistry(_projectDirectory, oldPath, dest, out var foldErr))
                        ScriptsRegistryChanged?.Invoke(this, EventArgs.Empty);
                    else if (!string.IsNullOrEmpty(foldErr))
                        EditorLog.Warning($"Renombrar carpeta: scripts.json no actualizado: {foldErr}", "scripts.json");
                }
                else if (string.Equals(Path.GetExtension(oldPath), ".lua", StringComparison.OrdinalIgnoreCase))
                {
                    if (ScriptRegistryProjectWriter.TryRenameAbsolutePaths(_projectDirectory, oldPath, dest, out var luaErr))
                        ScriptsRegistryChanged?.Invoke(this, EventArgs.Empty);
                    else if (!string.IsNullOrEmpty(luaErr))
                        EditorLog.Warning($"Renombrar script: scripts.json no actualizado: {luaErr}", "scripts.json");
                }
            }
        }
        catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    public void DeleteItem(ProjectExplorerItem item)
    {
        if (string.IsNullOrEmpty(item.FullPath)) return;
        if (item.IsLocked && System.Windows.MessageBox.Show("Este asset está bloqueado. ¿Eliminar de todos modos?", "Asset bloqueado", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        // Nota: operaciones por batch/script que llamen a DeleteItem sin pasar por UI deben comprobar IsLocked si se desea consistencia.
        if (System.Windows.MessageBox.Show($"¿Eliminar '{item.Name}'?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        var pathToRemove = item.FullPath;
        try
        {
            if (item.IsFolder)
            {
                if (!string.IsNullOrEmpty(_projectDirectory))
                {
                    if (ScriptRegistryProjectWriter.TryRemoveScriptsUnderFolder(_projectDirectory, pathToRemove, out _, out var folderRegErr))
                        ScriptsRegistryChanged?.Invoke(this, EventArgs.Empty);
                    else if (!string.IsNullOrEmpty(folderRegErr))
                        EditorLog.Warning($"Eliminar carpeta: scripts.json no actualizado: {folderRegErr}", "scripts.json");
                }

                Directory.Delete(pathToRemove, true);
            }
            else
            {
                if (string.Equals(Path.GetExtension(pathToRemove), ".lua", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(_projectDirectory))
                {
                    if (ScriptRegistryProjectWriter.TryRemoveByAbsolutePath(_projectDirectory, pathToRemove, out _, out var luaErr))
                        ScriptsRegistryChanged?.Invoke(this, EventArgs.Empty);
                    else if (!string.IsNullOrEmpty(luaErr))
                        EditorLog.Warning($"Eliminar script: scripts.json no actualizado: {luaErr}", "scripts.json");
                }

                File.Delete(pathToRemove);
            }

            RefreshTree();
        }
        catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    public void ShowInFolder(ProjectExplorerItem item)
    {
        if (item == null || string.IsNullOrEmpty(item.FullPath)) return;
        var path = item.FullPath;
        try
        {
            if (File.Exists(path))
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{path}\"");
            else if (Directory.Exists(path))
                System.Diagnostics.Process.Start("explorer.exe", path);
        }
        catch { }
    }

    private void ShowProperties(ProjectExplorerItem item)
    {
        if (item == null || string.IsNullOrEmpty(item.FullPath)) return;
        var path = item.FullPath;
        string typeStr = item.FileType.ToString();
        string sizeStr = "";
        if (item.IsFolder)
        {
            try { var count = Directory.GetFileSystemEntries(path).Length; sizeStr = $"{count} elementos"; } catch { sizeStr = "-"; }
        }
        else if (File.Exists(path))
        {
            try { var fi = new FileInfo(path); sizeStr = $"{fi.Length} bytes"; } catch { sizeStr = "-"; }
        }
        System.Windows.MessageBox.Show($"Nombre: {item.Name}\nTipo: {typeStr}\nRuta: {path}\n{(item.IsFolder ? "Elementos: " : "Tamaño: ")}{sizeStr}", "Propiedades", MessageBoxButton.OK);
    }

    private void MenuCopyPath_OnClick(object sender, RoutedEventArgs e)
    {
        var item = GetSelectedItem();
        if (item == null) return;
        try
        {
            System.Windows.Clipboard.SetText(item.FullPath);
        }
        catch { /* ignore */ }
    }

    private void MenuOpenInExplorer_OnClick(object sender, RoutedEventArgs e)
    {
        var item = GetSelectedItem();
        var path = item?.FullPath ?? _projectDirectory;
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            if (File.Exists(path))
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{path}\"");
            else if (Directory.Exists(path))
                System.Diagnostics.Process.Start("explorer.exe", path);
        }
        catch { /* ignore */ }
    }

    private void MenuUseAsTemplate_OnClick(object sender, RoutedEventArgs e)
    {
        var item = GetSelectedItem();
        if (item == null) return;
        System.Windows.MessageBox.Show("Usar como plantilla: en desarrollo.", "Explorador", MessageBoxButton.OK);
    }

    private void MenuNewSeed_OnClick(object sender, RoutedEventArgs e)
    {
        System.Windows.MessageBox.Show("Nuevo seed: en desarrollo.", "Explorador", MessageBoxButton.OK);
    }

    private void MenuNewScript_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_projectDirectory))
        {
            System.Windows.MessageBox.Show("Abre un proyecto primero.", "Explorador", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var scriptsDir = Path.Combine(_projectDirectory, "Scripts");
        var target = Directory.Exists(scriptsDir) ? scriptsDir : _projectDirectory;
        CreateNewLuaScript(target);
    }

    private void MenuGenerarPlaceholder_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_projectDirectory)) return;
        var path = PlaceholderGenerator.GenerateSpritePlaceholder(_projectDirectory, "sprite_placeholder", 32);
        if (path != null)
        {
            RefreshTree();
            System.Windows.MessageBox.Show($"Placeholder creado: {path}", "Sprite placeholder", MessageBoxButton.OK);
        }
    }

    private ProjectExplorerItem? GetSelectedItem()
    {
        return ProjectTree?.SelectedItem as ProjectExplorerItem;
    }

    private string? ResolveDropFolderForTree(ProjectExplorerItem? target)
    {
        if (string.IsNullOrEmpty(_projectDirectory)) return null;
        if (target == null) return _projectDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (target.IsFolder && !string.IsNullOrEmpty(target.FullPath))
            return target.FullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!string.IsNullOrEmpty(target.FullPath))
            return Path.GetDirectoryName(target.FullPath);
        return _projectDirectory;
    }

    private string? ResolveDropFolderForGrid(System.Windows.Point positionOnGrid)
    {
        if (string.IsNullOrEmpty(_projectDirectory) || ProjectGrid == null) return null;
        var hit = VisualTreeHelper.HitTest(ProjectGrid, positionOnGrid);
        DependencyObject? d = hit?.VisualHit;
        while (d != null && d != ProjectGrid)
        {
            if (d is System.Windows.FrameworkElement fe && fe.DataContext is ProjectExplorerItem item)
            {
                if (item.IsFolder && !string.IsNullOrEmpty(item.FullPath))
                    return item.FullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (!item.IsFolder && !string.IsNullOrEmpty(item.FullPath))
                    return Path.GetDirectoryName(item.FullPath);
            }
            d = VisualTreeHelper.GetParent(d);
        }
        var folder = string.IsNullOrEmpty(_gridCurrentFolderPath) ? _projectDirectory : _gridCurrentFolderPath;
        return folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static ProjectExplorerItem? GetListItemAtPosition(System.Windows.Controls.ListBox lb, System.Windows.Point position)
    {
        var dep = VisualTreeHelper.HitTest(lb, position)?.VisualHit;
        while (dep != null)
        {
            if (dep is System.Windows.Controls.ListBoxItem lbi && lbi.DataContext is ProjectExplorerItem item)
                return item;
            dep = VisualTreeHelper.GetParent(dep);
        }
        return null;
    }

    private void ProjectList_OnDragOver(object sender, System.Windows.DragEventArgs e)
    {
        if (sender is not System.Windows.Controls.ListBox lb) return;
        if (e.Data.GetDataPresent(ObjectInspectorPanel.DataFormatObjectInstanceId))
        {
            var target = GetListItemAtPosition(lb, e.GetPosition(lb));
            var dropFolder = ResolveDropFolderForTree(target);
            e.Effects = !string.IsNullOrEmpty(dropFolder) && Directory.Exists(dropFolder) ? System.Windows.DragDropEffects.Copy : System.Windows.DragDropEffects.None;
            e.Handled = true;
            return;
        }
        e.Effects = System.Windows.DragDropEffects.None;
        e.Handled = true;
    }

    private void ProjectList_OnDrop(object sender, System.Windows.DragEventArgs e)
    {
        if (sender is not System.Windows.Controls.ListBox lb) return;
        if (!e.Data.GetDataPresent(ObjectInspectorPanel.DataFormatObjectInstanceId)) return;
        var id = e.Data.GetData(ObjectInspectorPanel.DataFormatObjectInstanceId) as string;
        var target = GetListItemAtPosition(lb, e.GetPosition(lb));
        var dropFolder = ResolveDropFolderForTree(target);
        if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(dropFolder))
            RequestExportObjectAsSeed?.Invoke(this, (id, dropFolder));
        e.Handled = true;
    }

    private void ProjectGridScroll_OnDragOver(object sender, System.Windows.DragEventArgs e)
    {
        if (ProjectGrid == null) return;
        if (e.Data.GetDataPresent(ObjectInspectorPanel.DataFormatObjectInstanceId))
        {
            var pos = e.GetPosition(ProjectGrid);
            var dropFolder = ResolveDropFolderForGrid(pos);
            e.Effects = !string.IsNullOrEmpty(dropFolder) && Directory.Exists(dropFolder) ? System.Windows.DragDropEffects.Copy : System.Windows.DragDropEffects.None;
            e.Handled = true;
            return;
        }
        e.Effects = System.Windows.DragDropEffects.None;
        e.Handled = true;
    }

    private void ProjectGridScroll_OnDrop(object sender, System.Windows.DragEventArgs e)
    {
        if (ProjectGrid == null) return;
        if (!e.Data.GetDataPresent(ObjectInspectorPanel.DataFormatObjectInstanceId)) return;
        var id = e.Data.GetData(ObjectInspectorPanel.DataFormatObjectInstanceId) as string;
        var pos = e.GetPosition(ProjectGrid);
        var dropFolder = ResolveDropFolderForGrid(pos);
        if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(dropFolder))
            RequestExportObjectAsSeed?.Invoke(this, (id, dropFolder));
        e.Handled = true;
    }

    private void ProjectTree_OnDragOver(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            e.Effects = System.Windows.DragDropEffects.Copy;
            e.Handled = true;
            return;
        }
        if (e.Data.GetDataPresent(DataFormatExplorerItem))
        {
            var paths = e.Data.GetData(DataFormatExplorerItem) as string[];
            var target = GetItemAtPosition(ProjectTree, e.GetPosition(ProjectTree));
            var targetDir = target?.FullPath?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? "";
            var allowed = target != null && target.IsFolder && paths != null && paths.Length > 0 &&
                !paths.Any(p => string.Equals(p.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), targetDir, StringComparison.OrdinalIgnoreCase)) &&
                !paths.Any(p => targetDir.StartsWith(p.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
            e.Effects = allowed ? System.Windows.DragDropEffects.Move : System.Windows.DragDropEffects.None;
            e.Handled = true;
            return;
        }
        if (e.Data.GetDataPresent(ObjectInspectorPanel.DataFormatObjectInstanceId))
        {
            var target = GetItemAtPosition(ProjectTree, e.GetPosition(ProjectTree));
            var dropFolder = ResolveDropFolderForTree(target);
            e.Effects = !string.IsNullOrEmpty(dropFolder) && Directory.Exists(dropFolder) ? System.Windows.DragDropEffects.Copy : System.Windows.DragDropEffects.None;
            e.Handled = true;
            return;
        }
        e.Effects = System.Windows.DragDropEffects.None;
        e.Handled = true;
    }

    private static ProjectExplorerItem? GetItemAtPosition(System.Windows.Controls.TreeView tree, System.Windows.Point position)
    {
        var dep = System.Windows.Media.VisualTreeHelper.HitTest(tree, position)?.VisualHit;
        while (dep != null)
        {
            if (dep is System.Windows.Controls.TreeViewItem tvi && tvi.DataContext is ProjectExplorerItem item)
                return item;
            dep = System.Windows.Media.VisualTreeHelper.GetParent(dep);
        }
        return null;
    }

    private void ProjectTree_OnDrop(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetDataPresent(ObjectInspectorPanel.DataFormatObjectInstanceId))
        {
            var id = e.Data.GetData(ObjectInspectorPanel.DataFormatObjectInstanceId) as string;
            var target = GetItemAtPosition(ProjectTree, e.GetPosition(ProjectTree));
            var dropFolder = ResolveDropFolderForTree(target);
            if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(dropFolder))
                RequestExportObjectAsSeed?.Invoke(this, (id, dropFolder));
            e.Handled = true;
            return;
        }
        if (e.Data.GetDataPresent(DataFormatExplorerItem))
        {
            var paths = e.Data.GetData(DataFormatExplorerItem) as string[];
            var target = GetItemAtPosition(ProjectTree, e.GetPosition(ProjectTree));
            var targetDir = target != null && target.IsFolder ? target.FullPath : _projectDirectory;
            if (paths != null && paths.Length > 0 && !string.IsNullOrEmpty(targetDir))
            {
                try
                {
                    foreach (var src in paths)
                    {
                        if (string.IsNullOrEmpty(src)) continue;
                        var name = Path.GetFileName(src.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                        if (string.IsNullOrEmpty(name)) continue;
                        var dest = Directory.Exists(src) ? Path.Combine(targetDir, name) : Path.Combine(targetDir, Path.GetFileName(src));
                        if (string.Equals(src, dest, StringComparison.OrdinalIgnoreCase)) continue;
                        if (Directory.Exists(src))
                            Directory.Move(src, dest);
                        else if (File.Exists(src))
                            File.Move(src, dest);
                    }
                    RefreshTree();
                }
                catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message, "Error al mover", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
            _dragStartItem = null;
            e.Handled = true;
            return;
        }
        if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)) return;
        var files = (string[]?)e.Data.GetData(System.Windows.DataFormats.FileDrop);
        if (files == null || files.Length == 0 || string.IsNullOrEmpty(_projectDirectory)) return;
        var dropDir = _projectDirectory;
        var item = GetItemAtPosition(ProjectTree, e.GetPosition(ProjectTree)) ?? GetSelectedItem();
        if (item != null && !string.IsNullOrEmpty(item.FullPath))
        {
            if (item.IsFolder)
                dropDir = item.FullPath;
            else
                dropDir = Path.GetDirectoryName(item.FullPath) ?? _projectDirectory;
        }
        try
        {
            foreach (var src in files)
            {
                if (Directory.Exists(src))
                    continue;
                if (!File.Exists(src)) continue;
                var name = Path.GetFileName(src);
                if (string.IsNullOrEmpty(name)) continue;
                var dest = Path.Combine(dropDir, name);
                if (string.Equals(src, dest, StringComparison.OrdinalIgnoreCase)) continue;
                File.Copy(src, dest, overwrite: true);
            }
            RefreshTree();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Error al importar", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        e.Handled = true;
    }
}
