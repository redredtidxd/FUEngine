using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using FUEngine.Core;
using FUEngine.Editor;
using UIElementCore = FUEngine.Core.UIElement;

namespace FUEngine;

/// <summary>Convierte int? a Visibility: null -> Collapsed, else Visible.</summary>
public class LayerIndexToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Tipo de nodo en la jerarquía del mapa.</summary>
public enum MapHierarchyNodeKind
{
    MapRoot,
    LayersFolder,
    TileLayer,
    ObjectLayer,
    ObjectsFolder,
    ObjectInstance,
    GroupsFolder,
    PixelGroup,
    TriggersFolder,
    TriggerZone,
    UIFolder,
    UICanvasNode,
    UIElementNode
}

/// <summary>Elemento de la jerarquía del mapa (nodo raíz, carpeta, layer, instancia o trigger).</summary>
public class MapHierarchyItem : System.ComponentModel.INotifyPropertyChanged
{
    public string DisplayName { get; set; } = "";
    public string Icon { get; set; } = "📄";
    public MapHierarchyNodeKind NodeKind { get; set; }
    public ObjectInstance? ObjectInstance { get; set; }
    public TriggerZone? TriggerZone { get; set; }
    public UICanvas? UICanvas { get; set; }
    public UIElementCore? UIElement { get; set; }
    /// <summary>Nombre de capa cuando NodeKind es TileLayer u ObjectLayer (para Renombrar / reordenar).</summary>
    public string? LayerName { get; set; }
    /// <summary>Índice de capa (0-based) para TileLayer/ObjectLayer; null en otros nodos.</summary>
    public int? LayerIndex { get; set; }
    bool _isVisible = true;
    public bool IsVisible { get => _isVisible; set { if (_isVisible == value) return; _isVisible = value; PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsVisible))); } }
    public ObservableCollection<MapHierarchyItem> Children { get; } = new();
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
}

public partial class MapHierarchyPanel : System.Windows.Controls.UserControl
{
    public event EventHandler<ObjectInstance?>? ObjectSelected;
    public event EventHandler<TriggerZone?>? TriggerSelected;
    public event EventHandler? RequestCreateUICanvas;
    public event EventHandler<(UICanvas canvas, UIElementCore? parent, UIElementKind kind)>? RequestCreateUIElement;
    public event EventHandler<UICanvas?>? UICanvasSelected;
    public event EventHandler<UIElementCore?>? UIElementSelected;

    private ObjectLayer? _objectLayer;
    private IList<TriggerZone>? _triggers;
    private Action? _saveTriggers;
    private UIRoot? _uiRoot;
    private MapHierarchyItem? _dragStartLayerItem;
    private System.Windows.Point? _dragStartPoint;
    public const string DataFormatLayerReorder = "FUEngine.HierarchyLayerReorder";

    public MapHierarchyPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Asigna la lista de triggers y el callback para guardar (llamar tras añadir/eliminar/renombrar).
    /// </summary>
    public void SetTriggerData(IList<TriggerZone>? triggers, Action? saveTriggers)
    {
        _triggers = triggers;
        _saveTriggers = saveTriggers;
    }

    /// <summary>
    /// Construye la jerarquía: nombre del mapa, Layers, Objects, Groups, Triggers, UI (Canvas).
    /// </summary>
    /// <param name="visibleLayerIndices">Índices de capas visibles (si null, todas visibles).</param>
    /// <param name="uiRoot">Raíz UI de la escena (Canvas). Si null, no se muestra nodo UI.</param>
    public void SetMapStructure(string mapDisplayName, IReadOnlyList<string>? layerNames, ObjectLayer? layer, IList<TriggerZone>? triggers = null, IReadOnlySet<int>? visibleLayerIndices = null, UIRoot? uiRoot = null)
    {
        _objectLayer = layer;
        if (triggers != null) _triggers = triggers;
        _uiRoot = uiRoot;

        var root = new MapHierarchyItem
        {
            DisplayName = string.IsNullOrWhiteSpace(mapDisplayName) ? "Mapa" : mapDisplayName,
            Icon = "🗺",
            NodeKind = MapHierarchyNodeKind.MapRoot
        };

        var layersFolder = new MapHierarchyItem { DisplayName = "Layers", Icon = "📑", NodeKind = MapHierarchyNodeKind.LayersFolder };
        var names = layerNames ?? NewProjectStructure.DefaultLayerNames;
        for (int i = 0; i < names.Count; i++)
        {
            var name = names[i];
            var isObjectLayer = string.Equals(name, "Objects", StringComparison.OrdinalIgnoreCase);
            var layerType = isObjectLayer ? "object layer" : "tile layer";
            layersFolder.Children.Add(new MapHierarchyItem
            {
                DisplayName = $"{name} ({layerType})",
                Icon = "▢",
                NodeKind = isObjectLayer ? MapHierarchyNodeKind.ObjectLayer : MapHierarchyNodeKind.TileLayer,
                LayerName = name,
                LayerIndex = i,
                IsVisible = visibleLayerIndices == null || visibleLayerIndices.Contains(i)
            });
        }
        root.Children.Add(layersFolder);

        var objectsFolder = new MapHierarchyItem { DisplayName = "Objetos", Icon = "📦", NodeKind = MapHierarchyNodeKind.ObjectsFolder };
        if (layer != null)
        {
            foreach (var inst in layer.Instances)
            {
                var def = layer.GetDefinition(inst.DefinitionId);
                var name = string.IsNullOrWhiteSpace(inst.Nombre) ? (def?.Nombre ?? inst.InstanceId) : inst.Nombre;
                objectsFolder.Children.Add(new MapHierarchyItem
                {
                    DisplayName = name,
                    Icon = "◆",
                    NodeKind = MapHierarchyNodeKind.ObjectInstance,
                    ObjectInstance = inst
                });
            }
        }
        root.Children.Add(objectsFolder);

        var groupsFolder = new MapHierarchyItem { DisplayName = "Groups", Icon = "📁", NodeKind = MapHierarchyNodeKind.GroupsFolder };
        groupsFolder.Children.Add(new MapHierarchyItem { DisplayName = "DefaultGroup", Icon = "◇", NodeKind = MapHierarchyNodeKind.PixelGroup });
        root.Children.Add(groupsFolder);

        var triggersFolder = new MapHierarchyItem { DisplayName = "Triggers", Icon = "⚡", NodeKind = MapHierarchyNodeKind.TriggersFolder };
        foreach (var t in _triggers ?? Array.Empty<TriggerZone>())
        {
            triggersFolder.Children.Add(new MapHierarchyItem
            {
                DisplayName = string.IsNullOrWhiteSpace(t.Nombre) ? t.Id : t.Nombre,
                Icon = "⚡",
                NodeKind = MapHierarchyNodeKind.TriggerZone,
                TriggerZone = t
            });
        }
        root.Children.Add(triggersFolder);

        if (_uiRoot != null && _uiRoot.Canvases.Count >= 0)
        {
            var uiFolder = new MapHierarchyItem { DisplayName = "UI", Icon = "🖼", NodeKind = MapHierarchyNodeKind.UIFolder };
            foreach (var canvas in _uiRoot.Canvases)
            {
                var canvasItem = new MapHierarchyItem
                {
                    DisplayName = string.IsNullOrWhiteSpace(canvas.Name) ? canvas.Id : canvas.Name,
                    Icon = "▢",
                    NodeKind = MapHierarchyNodeKind.UICanvasNode,
                    UICanvas = canvas
                };
                foreach (var child in canvas.Children)
                    canvasItem.Children.Add(BuildUIElementItem(child));
                uiFolder.Children.Add(canvasItem);
            }
            root.Children.Add(uiFolder);
        }

        HierarchyTree.ItemsSource = new[] { root };
    }

    private static MapHierarchyItem BuildUIElementItem(UIElementCore e)
    {
        var icon = e.Kind switch { UIElementKind.Button => "🔘", UIElementKind.Text => "T", UIElementKind.Image => "🖼", UIElementKind.Panel => "▦", _ => "▢" };
        var name = string.IsNullOrEmpty(e.Id) ? e.Kind.ToString() : e.Id;
        var item = new MapHierarchyItem
        {
            DisplayName = name,
            Icon = icon,
            NodeKind = MapHierarchyNodeKind.UIElementNode,
            UIElement = e
        };
        foreach (var child in e.Children)
            item.Children.Add(BuildUIElementItem(child));
        return item;
    }

    /// <summary>
    /// Compatibilidad: usa solo capa de objetos y nombre por defecto "Mapa".
    /// </summary>
    public void SetObjects(ObjectLayer? layer)
    {
        SetMapStructure("Mapa", NewProjectStructure.DefaultLayerNames, layer);
    }

    private void HierarchyTree_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is MapHierarchyItem item)
        {
            ObjectSelected?.Invoke(this, item.ObjectInstance);
            TriggerSelected?.Invoke(this, item.TriggerZone);
            UICanvasSelected?.Invoke(this, item.UICanvas);
            UIElementSelected?.Invoke(this, item.UIElement);
        }
    }

    private void HierarchyTree_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        SelectTreeViewItemUnderMouse(e.OriginalSource as DependencyObject);
    }

    private void HierarchyTree_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (HierarchyTree == null) return;
        var item = HierarchyTree.SelectedItem as MapHierarchyItem;
        if (item == null)
        {
            var pt = e.GetPosition(HierarchyTree);
            var hit = VisualTreeHelper.HitTest(HierarchyTree, pt);
            if (hit?.VisualHit is DependencyObject dep)
            {
                var tvi = FindParentTreeViewItem(dep);
                if (tvi != null)
                {
                    tvi.IsSelected = true;
                    item = HierarchyTree.SelectedItem as MapHierarchyItem;
                }
            }
            if (item == null && HierarchyTree.Items.Count > 0)
                item = HierarchyTree.Items[0] as MapHierarchyItem;
        }
        if (item != null)
        {
            var menu = BuildContextMenu(item);
            if (menu != null)
            {
                HierarchyTree.ContextMenu = menu;
                menu.PlacementTarget = HierarchyTree;
                menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                menu.IsOpen = true;
                e.Handled = true;
            }
        }
    }

    private static TreeViewItem? FindParentTreeViewItem(DependencyObject? dep)
    {
        while (dep != null)
        {
            if (dep is TreeViewItem tvi) return tvi;
            dep = VisualTreeHelper.GetParent(dep);
        }
        return null;
    }

    private static void SelectTreeViewItemUnderMouse(DependencyObject? source)
    {
        var dep = source;
        while (dep != null)
        {
            if (dep is TreeViewItem tvi)
            {
                tvi.IsSelected = true;
                tvi.Focus();
                break;
            }
            dep = VisualTreeHelper.GetParent(dep);
        }
    }

    private void HierarchyTree_OnContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (HierarchyTree == null) { e.Handled = true; return; }
        var item = HierarchyTree.SelectedItem as MapHierarchyItem;
        if (item == null) { e.Handled = true; return; }
        var menu = BuildContextMenu(item);
        if (menu == null) { e.Handled = true; return; }
        HierarchyTree.ContextMenu = menu;
    }

    private ContextMenu? BuildContextMenu(MapHierarchyItem item)
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

        switch (item.NodeKind)
        {
            case MapHierarchyNodeKind.MapRoot:
            case MapHierarchyNodeKind.LayersFolder:
                AddMenuItem(menu, "Crear", null!, false);
                AddSubMenuItem(menu, "Crear", "Nuevo Tile Layer", (s, _) => OnRequestAddTileLayer());
                AddSubMenuItem(menu, "Crear", "Nuevo Object Layer", (s, _) => OnRequestAddObjectLayer());
                AddSubMenuItem(menu, "Crear", "Nuevo Grupo de pixeles/tiles", (s, _) => OnRequestAddPixelGroup());
                AddSubMenuItem(menu, "Crear", "Nuevo Trigger Zone", (s, _) => OnRequestCreateTrigger());
                AddSubMenuItem(menu, "Crear", "Nuevo Objeto", (s, _) => OnRequestCreateObject());
                AddSubMenuItem(menu, "Crear", "UI", null!, false);
                AddSubSubMenuItem(menu, "Crear", "UI", "Canvas", (s, _) => RequestCreateUICanvas?.Invoke(this, EventArgs.Empty));
                break;
            case MapHierarchyNodeKind.TileLayer:
            case MapHierarchyNodeKind.ObjectLayer:
                AddMenuItem(menu, "Duplicar", (s, _) => { }, item.NodeKind == MapHierarchyNodeKind.TileLayer);
                AddMenuItem(menu, "Eliminar", (s, _) => OnRequestRemoveLayer(item));
                AddMenuItem(menu, "Renombrar", (s, _) => OnRequestRenameLayer(item));
                AddMenuItem(menu, "Propiedades", (s, _) => OnRequestProperties(item));
                break;
            case MapHierarchyNodeKind.ObjectsFolder:
                AddMenuItem(menu, "Nuevo Objeto", (s, _) => OnRequestCreateObject());
                break;
            case MapHierarchyNodeKind.ObjectInstance:
                AddMenuItem(menu, "Duplicar", (s, _) => OnRequestDuplicateObject(item));
                AddMenuItem(menu, "Eliminar", (s, _) => OnRequestDeleteObject(item));
                AddMenuItem(menu, "Renombrar", (s, _) => OnRequestRenameObject(item));
                AddMenuItem(menu, "Propiedades", (s, _) => OnRequestProperties(item));
                break;
            case MapHierarchyNodeKind.GroupsFolder:
            case MapHierarchyNodeKind.PixelGroup:
                AddMenuItem(menu, "Nuevo grupo hijo", (s, _) => OnRequestAddPixelGroup());
                AddMenuItem(menu, "Duplicar", (s, _) => { });
                AddMenuItem(menu, "Eliminar", (s, _) => { });
                AddMenuItem(menu, "Renombrar", (s, _) => OnRequestRenameGroup(item));
                AddMenuItem(menu, "Propiedades", (s, _) => OnRequestProperties(item));
                break;
            case MapHierarchyNodeKind.TriggersFolder:
                AddMenuItem(menu, "Nuevo Trigger Zone", (s, _) => OnRequestCreateTrigger());
                break;
            case MapHierarchyNodeKind.TriggerZone:
                AddMenuItem(menu, "Duplicar", (s, _) => OnRequestDuplicateTrigger(item));
                AddMenuItem(menu, "Eliminar", (s, _) => OnRequestDeleteTrigger(item));
                AddMenuItem(menu, "Renombrar", (s, _) => OnRequestRenameTrigger(item));
                AddMenuItem(menu, "Propiedades", (s, _) => OnRequestProperties(item));
                break;
            case MapHierarchyNodeKind.UIFolder:
                AddMenuItem(menu, "Crear", null!, false);
                AddSubMenuItem(menu, "Crear", "UI", null!, false);
                AddSubSubMenuItem(menu, "Crear", "UI", "Canvas", (s, _) => RequestCreateUICanvas?.Invoke(this, EventArgs.Empty));
                break;
            case MapHierarchyNodeKind.UICanvasNode:
                if (item.UICanvas != null)
                {
                    AddMenuItem(menu, "Crear", null!, false);
                    AddSubMenuItem(menu, "Crear", "UI", null!, false);
                    AddSubSubMenuItem(menu, "Crear", "UI", "Button", (s, _) => RequestCreateUIElement?.Invoke(this, (item.UICanvas!, null, UIElementKind.Button)));
                    AddSubSubMenuItem(menu, "Crear", "UI", "Text", (s, _) => RequestCreateUIElement?.Invoke(this, (item.UICanvas!, null, UIElementKind.Text)));
                    AddSubSubMenuItem(menu, "Crear", "UI", "Image", (s, _) => RequestCreateUIElement?.Invoke(this, (item.UICanvas!, null, UIElementKind.Image)));
                    AddSubSubMenuItem(menu, "Crear", "UI", "Panel", (s, _) => RequestCreateUIElement?.Invoke(this, (item.UICanvas!, null, UIElementKind.Panel)));
                }
                AddMenuItem(menu, "Abrir en tab UI", (s, _) => OnRequestOpenCanvasInTab(item.UICanvas));
                break;
            case MapHierarchyNodeKind.UIElementNode:
                if (item.UIElement != null)
                {
                    var canvas = FindCanvasContaining(item.UIElement);
                    if (canvas != null)
                    {
                        AddMenuItem(menu, "Crear", null!, false);
                        AddSubMenuItem(menu, "Crear", "UI", null!, false);
                        AddSubSubMenuItem(menu, "Crear", "UI", "Button", (s, _) => RequestCreateUIElement?.Invoke(this, (canvas, item.UIElement!, UIElementKind.Button)));
                        AddSubSubMenuItem(menu, "Crear", "UI", "Text", (s, _) => RequestCreateUIElement?.Invoke(this, (canvas, item.UIElement!, UIElementKind.Text)));
                        AddSubSubMenuItem(menu, "Crear", "UI", "Image", (s, _) => RequestCreateUIElement?.Invoke(this, (canvas, item.UIElement!, UIElementKind.Image)));
                        AddSubSubMenuItem(menu, "Crear", "UI", "Panel", (s, _) => RequestCreateUIElement?.Invoke(this, (canvas, item.UIElement!, UIElementKind.Panel)));
                    }
                }
                AddMenuItem(menu, "Propiedades", (s, _) => UIElementSelected?.Invoke(this, item.UIElement));
                break;
            default:
                return null;
        }
        return menu;
    }

    private void AddMenuItem(ContextMenu menu, string header, RoutedEventHandler click, bool isPlaceholder = false)
    {
        var mi = new MenuItem { Header = header };
        if (click != null && !isPlaceholder) mi.Click += click;
        else if (isPlaceholder) mi.Click += (s, _) => System.Windows.MessageBox.Show("Próximamente.", "Jerarquía", MessageBoxButton.OK);
        menu.Items.Add(mi);
    }

    private void AddSubMenuItem(ContextMenu menu, string parentHeader, string subHeader, RoutedEventHandler? click, bool isPlaceholder = false)
    {
        MenuItem? parent = null;
        foreach (var c in menu.Items)
        {
            if (c is MenuItem m && (m.Header as string) == parentHeader) { parent = m; break; }
        }
        if (parent == null)
        {
            parent = new MenuItem { Header = parentHeader };
            menu.Items.Add(parent);
        }
        var sub = new MenuItem { Header = subHeader };
        if (click != null && !isPlaceholder) sub.Click += click;
        parent.Items.Add(sub);
    }

    private void AddSubSubMenuItem(ContextMenu menu, string grandParentHeader, string parentHeader, string subHeader, RoutedEventHandler click)
    {
        MenuItem? grandParent = null;
        foreach (var c in menu.Items)
        {
            if (c is MenuItem m && (m.Header as string) == grandParentHeader) { grandParent = m; break; }
        }
        if (grandParent == null) return;
        MenuItem? parent = null;
        foreach (var c in grandParent.Items)
        {
            if (c is MenuItem m && (m.Header as string) == parentHeader) { parent = m; break; }
        }
        if (parent == null)
        {
            parent = new MenuItem { Header = parentHeader };
            grandParent.Items.Add(parent);
        }
        var sub = new MenuItem { Header = subHeader };
        sub.Click += click;
        parent.Items.Add(sub);
    }

    private UICanvas? FindCanvasContaining(UIElementCore element)
    {
        if (_uiRoot == null) return null;
        foreach (var c in _uiRoot.Canvases)
            if (ContainsElement(c.Children, element)) return c;
        return null;
    }

    private static bool ContainsElement(List<UIElementCore> elements, UIElementCore target)
    {
        foreach (var e in elements)
        {
            if (e == target) return true;
            if (ContainsElement(e.Children, target)) return true;
        }
        return false;
    }

    public event EventHandler<UICanvas?>? RequestOpenCanvasInTab;

    private void OnRequestOpenCanvasInTab(UICanvas? canvas)
    {
        if (canvas != null) RequestOpenCanvasInTab?.Invoke(this, canvas);
    }

    private void OnRequestCreateObject()
    {
        RequestCreateObject?.Invoke(this, EventArgs.Empty);
    }

    private void OnRequestCreateTrigger()
    {
        if (_triggers == null || _saveTriggers == null) return;
        var zone = new TriggerZone { Nombre = "Nueva zona", Width = 2, Height = 2 };
        _triggers.Add(zone);
        _saveTriggers();
        RequestRefresh?.Invoke(this, EventArgs.Empty);
    }

    private void OnRequestDuplicateObject(MapHierarchyItem item)
    {
        if (item.ObjectInstance == null || _objectLayer == null) return;
        RequestDuplicateObject?.Invoke(this, item.ObjectInstance);
    }

    private void OnRequestDeleteObject(MapHierarchyItem item)
    {
        if (item.ObjectInstance == null) return;
        RequestDeleteObject?.Invoke(this, item.ObjectInstance);
    }

    private void OnRequestRenameObject(MapHierarchyItem item)
    {
        if (item.ObjectInstance == null) return;
        RequestRenameObject?.Invoke(this, item.ObjectInstance);
    }

    private void OnRequestDuplicateTrigger(MapHierarchyItem item)
    {
        if (item.TriggerZone == null || _triggers == null || _saveTriggers == null) return;
        var clone = new TriggerZone
        {
            Id = Guid.NewGuid().ToString("N"),
            Nombre = item.TriggerZone.Nombre + " (copia)",
            X = item.TriggerZone.X + 2,
            Y = item.TriggerZone.Y,
            Width = item.TriggerZone.Width,
            Height = item.TriggerZone.Height,
            ScriptIdOnEnter = item.TriggerZone.ScriptIdOnEnter,
            ScriptIdOnExit = item.TriggerZone.ScriptIdOnExit,
            Tags = new List<string>(item.TriggerZone.Tags ?? new List<string>())
        };
        _triggers.Add(clone);
        _saveTriggers();
        RequestRefresh?.Invoke(this, EventArgs.Empty);
    }

    private void OnRequestDeleteTrigger(MapHierarchyItem item)
    {
        if (item.TriggerZone == null || _triggers == null || _saveTriggers == null) return;
        if (System.Windows.MessageBox.Show("¿Eliminar esta zona trigger?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        _triggers.Remove(item.TriggerZone);
        _saveTriggers();
        RequestRefresh?.Invoke(this, EventArgs.Empty);
    }

    private void OnRequestRenameTrigger(MapHierarchyItem item)
    {
        if (item.TriggerZone == null) return;
        var name = ShowRenameDialog("Renombrar trigger", item.TriggerZone.Nombre);
        if (!string.IsNullOrWhiteSpace(name)) { item.TriggerZone.Nombre = name.Trim(); _saveTriggers?.Invoke(); RequestRefresh?.Invoke(this, EventArgs.Empty); }
    }

    /// <summary>Diálogo genérico para renombrar (usado también desde EditorWindow).</summary>
    public static string? ShowRenameDialogPublic(string title, string currentName) => ShowRenameDialog(title, currentName);

    private static string? ShowRenameDialog(string title, string currentName)
    {
        var w = new Window
        {
            Title = title,
            Width = 320,
            Height = 120,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize
        };
        var stack = new StackPanel { Margin = new Thickness(12) };
        var label = new TextBlock { Text = "Nombre:", Margin = new Thickness(0, 0, 0, 4) };
        var tb = new System.Windows.Controls.TextBox { Text = currentName ?? "", Margin = new Thickness(0, 0, 0, 12) };
        var panel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
        var ok = new System.Windows.Controls.Button { Content = "Aceptar", IsDefault = true, Width = 80, Margin = new Thickness(0, 0, 8, 0) };
        var cancel = new System.Windows.Controls.Button { Content = "Cancelar", IsCancel = true, Width = 80 };
        string? result = null;
        ok.Click += (_, _) => { result = tb.Text; w.DialogResult = true; w.Close(); };
        cancel.Click += (_, _) => w.Close();
        w.Closing += (_, args) => { if (w.DialogResult != true) result = null; };
        stack.Children.Add(label);
        stack.Children.Add(tb);
        panel.Children.Add(ok);
        panel.Children.Add(cancel);
        stack.Children.Add(panel);
        w.Content = stack;
        w.ShowDialog();
        return result;
    }

    private void OnRequestRemoveLayer(MapHierarchyItem item) { System.Windows.MessageBox.Show("Eliminar capa: próximamente.", "Jerarquía", MessageBoxButton.OK); }
    private void OnRequestRenameLayer(MapHierarchyItem item) { System.Windows.MessageBox.Show("Renombrar capa: próximamente.", "Jerarquía", MessageBoxButton.OK); }
    private void OnRequestAddTileLayer() { RequestAddLayer?.Invoke(this, EventArgs.Empty); }
    private void OnRequestAddObjectLayer() { }
    private void OnRequestAddPixelGroup() { }
    private void OnRequestRenameGroup(MapHierarchyItem item) { }
    private void OnRequestProperties(MapHierarchyItem item)
    {
        if (item.ObjectInstance != null) { ObjectSelected?.Invoke(this, item.ObjectInstance); return; }
        if (item.TriggerZone != null) { TriggerSelected?.Invoke(this, item.TriggerZone); return; }
    }

    public event EventHandler? RequestCreateObject;
    public event EventHandler? RequestAddLayer;
    public event EventHandler? RequestRefresh;
    public event EventHandler<ObjectInstance>? RequestDuplicateObject;
    public event EventHandler<ObjectInstance>? RequestDeleteObject;
    public event EventHandler<ObjectInstance>? RequestRenameObject;
    public event EventHandler<string>? RequestInstantiateAsset;
    public event EventHandler<(int layerIndex, bool visible)>? LayerVisibilityToggled;
    public event EventHandler<(int fromIndex, int toIndex)>? RequestReorderLayers;

    private void HierarchyTree_OnPreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var tree = sender as System.Windows.Controls.TreeView;
        if (tree == null) return;
        _dragStartPoint = e.GetPosition(tree);
        _dragStartLayerItem = GetHierarchyItemAtPosition(tree, _dragStartPoint.Value);
        if (_dragStartLayerItem == null || (_dragStartLayerItem.NodeKind != MapHierarchyNodeKind.TileLayer && _dragStartLayerItem.NodeKind != MapHierarchyNodeKind.ObjectLayer))
            _dragStartLayerItem = null;
    }

    private void HierarchyTree_OnPreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.LeftButton != System.Windows.Input.MouseButtonState.Pressed || _dragStartLayerItem == null || !_dragStartLayerItem.LayerIndex.HasValue || HierarchyTree == null) return;
        var pos = e.GetPosition(HierarchyTree);
        if (_dragStartPoint.HasValue && Math.Abs(pos.X - _dragStartPoint.Value.X) < 4 && Math.Abs(pos.Y - _dragStartPoint.Value.Y) < 4) return;
        var idx = _dragStartLayerItem.LayerIndex.Value;
        var data = new System.Windows.DataObject(DataFormatLayerReorder, idx);
        try { System.Windows.DragDrop.DoDragDrop(HierarchyTree, data, System.Windows.DragDropEffects.Move); }
        finally { _dragStartLayerItem = null; _dragStartPoint = null; }
        e.Handled = true;
    }

    private void LayerVisibilityCheckBox_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.CheckBox cb || cb.DataContext is not MapHierarchyItem item || !item.LayerIndex.HasValue) return;
        item.IsVisible = cb.IsChecked == true;
        LayerVisibilityToggled?.Invoke(this, (item.LayerIndex.Value, item.IsVisible));
    }

    private void HierarchyTree_OnDragOver(object sender, System.Windows.DragEventArgs e)
    {
        var tree = sender as System.Windows.Controls.TreeView;
        var target = tree != null ? GetHierarchyItemAtPosition(tree, e.GetPosition(tree)) : null;

        if (e.Data.GetDataPresent(DataFormatLayerReorder))
        {
            var fromIdx = e.Data.GetData(DataFormatLayerReorder) is int i ? i : (int?)null;
            var ok = fromIdx.HasValue && target != null && target.LayerIndex.HasValue && (target.NodeKind == MapHierarchyNodeKind.TileLayer || target.NodeKind == MapHierarchyNodeKind.ObjectLayer) && target.LayerIndex != fromIdx;
            e.Effects = ok ? System.Windows.DragDropEffects.Move : System.Windows.DragDropEffects.None;
            e.Handled = true;
            return;
        }
        if (!e.Data.GetDataPresent(ProjectExplorerPanel.DataFormatAssetPath))
        {
            e.Effects = System.Windows.DragDropEffects.None;
            e.Handled = true;
            return;
        }
        var assetOk = target != null && (target.NodeKind == MapHierarchyNodeKind.ObjectsFolder || target.NodeKind == MapHierarchyNodeKind.ObjectLayer);
        e.Effects = assetOk ? System.Windows.DragDropEffects.Copy : System.Windows.DragDropEffects.None;
        e.Handled = true;
    }

    private void HierarchyTree_OnDrop(object sender, System.Windows.DragEventArgs e)
    {
        var tree = sender as System.Windows.Controls.TreeView;
        var target = tree != null ? GetHierarchyItemAtPosition(tree, e.GetPosition(tree)) : null;

        if (e.Data.GetDataPresent(DataFormatLayerReorder))
        {
            var fromIdx = e.Data.GetData(DataFormatLayerReorder) is int fi ? fi : (int?)null;
            if (fromIdx.HasValue && target != null && target.LayerIndex.HasValue && (target.NodeKind == MapHierarchyNodeKind.TileLayer || target.NodeKind == MapHierarchyNodeKind.ObjectLayer))
            {
                RequestReorderLayers?.Invoke(this, (fromIdx.Value, target.LayerIndex.Value));
            }
            e.Handled = true;
            return;
        }
        if (!e.Data.GetDataPresent(ProjectExplorerPanel.DataFormatAssetPath)) return;
        var path = e.Data.GetData(ProjectExplorerPanel.DataFormatAssetPath) as string;
        if (string.IsNullOrEmpty(path) || (target?.NodeKind != MapHierarchyNodeKind.ObjectsFolder && target?.NodeKind != MapHierarchyNodeKind.ObjectLayer))
            return;
        RequestInstantiateAsset?.Invoke(this, path);
        e.Handled = true;
    }

    private static MapHierarchyItem? GetHierarchyItemAtPosition(System.Windows.Controls.TreeView tree, System.Windows.Point position)
    {
        var hit = System.Windows.Media.VisualTreeHelper.HitTest(tree, position)?.VisualHit;
        while (hit != null)
        {
            if (hit is System.Windows.FrameworkElement fe && fe.DataContext is MapHierarchyItem item)
                return item;
            hit = System.Windows.Media.VisualTreeHelper.GetParent(hit);
        }
        return null;
    }
}
