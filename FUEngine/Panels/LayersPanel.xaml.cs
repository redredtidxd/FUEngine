using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using FUEngine.Core;

namespace FUEngine;

/// <summary>Elemento de lista para el panel de capas (nombre, iconos, índice).</summary>
public class LayerListItem : INotifyPropertyChanged
{
    private string _displayName = "";
    private string _visibilityIcon = "✓";
    private string _lockIcon = "🔓";
    private bool _isEditingName;

    public int LayerIndex { get; set; }
    public MapLayerDescriptor Descriptor { get; set; } = null!;

    public bool IsEditingName
    {
        get => _isEditingName;
        set { _isEditingName = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEditingName))); }
    }

    public string DisplayName
    {
        get => _displayName;
        set { _displayName = value ?? ""; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName))); }
    }

    public string VisibilityIcon
    {
        get => _visibilityIcon;
        set { _visibilityIcon = value ?? "👁"; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VisibilityIcon))); }
    }

    public string LockIcon
    {
        get => _lockIcon;
        set { _lockIcon = value ?? "🔓"; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LockIcon))); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public partial class LayersPanel : System.Windows.Controls.UserControl
{
    private TileMap? _tileMap;
    private int _activeLayerIndex;
    private readonly ObservableCollection<LayerListItem> _items = new();

    public LayersPanel()
    {
        InitializeComponent();
        LayersList.ItemsSource = _items;
    }

    /// <summary>Índice de la capa activa (para pintar).</summary>
    public int ActiveLayerIndex
    {
        get => _activeLayerIndex;
        set
        {
            if (_activeLayerIndex == value) return;
            _activeLayerIndex = value;
            SyncSelection();
            ActiveLayerChanged?.Invoke(this, value);
        }
    }

    public void SetTileMap(TileMap? map)
    {
        _tileMap = map;
        RefreshList();
    }

    public void RefreshList()
    {
        _items.Clear();
        if (_tileMap == null)
            return;
        for (int i = 0; i < _tileMap.Layers.Count; i++)
        {
            var desc = _tileMap.Layers[i];
            var display = string.IsNullOrWhiteSpace(desc.Name) ? $"Capa {i}" : desc.Name;
            if (string.IsNullOrWhiteSpace(desc.Name))
                desc.Name = display;
            _items.Add(new LayerListItem
            {
                LayerIndex = i,
                Descriptor = desc,
                DisplayName = display,
                VisibilityIcon = desc.IsVisible ? "✓" : "✕",
                LockIcon = desc.IsLocked ? "🔒" : "🔓"
            });
        }
        SyncSelection();
    }

    private void SyncSelection()
    {
        if (LayersList == null || _items.Count == 0) return;
        if (_activeLayerIndex >= 0 && _activeLayerIndex < _items.Count)
        {
            LayersList.SelectedIndex = _activeLayerIndex;
        }
    }

    private void LayersList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LayersList == null) return;
        if (LayersList.SelectedItem is not LayerListItem item)
        {
            LayerSelected?.Invoke(this, null);
            return;
        }
        if (item.LayerIndex != _activeLayerIndex)
        {
            _activeLayerIndex = item.LayerIndex;
            ActiveLayerChanged?.Invoke(this, _activeLayerIndex);
        }
        LayerSelected?.Invoke(this, item.Descriptor);
    }

    private void LayersList_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (LayersList == null) return;
        if (e.OriginalSource is System.Windows.Controls.Button) return;
        if (e.OriginalSource is System.Windows.Controls.TextBox) return;
        DependencyObject? dep = e.OriginalSource as DependencyObject;
        while (dep != null && dep is not ListBoxItem)
            dep = VisualTreeHelper.GetParent(dep);
        if (dep is not ListBoxItem lbi || lbi.DataContext is not LayerListItem item) return;
        var prev = LayersList.SelectedItem;
        LayersList.SelectedItem = item;
        LayersList.Focus();
        if (Equals(prev, item))
            LayerSelected?.Invoke(this, item.Descriptor);
    }

    private void LayersList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is System.Windows.Controls.Button) return;
        if (e.OriginalSource is System.Windows.Controls.TextBox) return;
        if (LayersList?.SelectedItem is not LayerListItem item) return;
        e.Handled = true;
        BeginRename(item);
    }

    private void BeginRename(LayerListItem item)
    {
        if (_tileMap == null) return;
        item.IsEditingName = true;
        LayersList?.UpdateLayout();
        Dispatcher.BeginInvoke(() =>
        {
            LayersList?.UpdateLayout();
            if (LayersList?.ItemContainerGenerator.ContainerFromItem(item) is ListBoxItem lbi)
            {
                var tb = FindVisualChild<System.Windows.Controls.TextBox>(lbi);
                if (tb != null)
                {
                    tb.Focus();
                    Keyboard.Focus(tb);
                    tb.SelectAll();
                }
            }
        }, DispatcherPriority.Loaded);
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T t) return t;
            var nested = FindVisualChild<T>(child);
            if (nested != null) return nested;
        }
        return null;
    }

    private void LayerNameEdit_OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox tb || tb.DataContext is not LayerListItem item) return;
        CommitLayerRename(item, tb.Text);
    }

    private void LayerNameEdit_OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox tb || tb.DataContext is not LayerListItem item) return;
        if (e.Key == Key.Enter)
        {
            CommitLayerRename(item, tb.Text);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            var fallback = string.IsNullOrWhiteSpace(item.Descriptor.Name) ? $"Capa {item.LayerIndex}" : item.Descriptor.Name;
            item.DisplayName = fallback;
            item.IsEditingName = false;
            e.Handled = true;
        }
    }

    private void CommitLayerRename(LayerListItem item, string? text)
    {
        if (_tileMap == null) return;
        var name = (text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
            name = string.IsNullOrWhiteSpace(item.Descriptor.Name) ? $"Capa {item.LayerIndex}" : item.Descriptor.Name!;
        item.Descriptor.Name = name;
        item.DisplayName = name;
        item.IsEditingName = false;
        LayerNameChanged?.Invoke(this, (item.LayerIndex, name));
    }

    private void BtnVisible_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn || btn.DataContext is not LayerListItem item || _tileMap == null) return;
        var wasAlreadyThisRow = LayersList.SelectedItem == item;
        LayersList.SelectedItem = item;
        if (item.LayerIndex != _activeLayerIndex)
            ActiveLayerIndex = item.LayerIndex;
        else if (wasAlreadyThisRow)
            LayerSelected?.Invoke(this, item.Descriptor);
        int i = item.LayerIndex;
        if (i < 0 || i >= _tileMap.Layers.Count) return;
        var desc = _tileMap.Layers[i];
        desc.IsVisible = !desc.IsVisible;
        item.VisibilityIcon = desc.IsVisible ? "✓" : "✕";
        LayerVisibilityToggled?.Invoke(this, (i, desc.IsVisible));
    }

    private void BtnLock_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn || btn.DataContext is not LayerListItem item || _tileMap == null) return;
        var wasAlreadyThisRow = LayersList.SelectedItem == item;
        LayersList.SelectedItem = item;
        if (item.LayerIndex != _activeLayerIndex)
            ActiveLayerIndex = item.LayerIndex;
        else if (wasAlreadyThisRow)
            LayerSelected?.Invoke(this, item.Descriptor);
        int i = item.LayerIndex;
        if (i < 0 || i >= _tileMap.Layers.Count) return;
        var desc = _tileMap.Layers[i];
        desc.IsLocked = !desc.IsLocked;
        item.LockIcon = desc.IsLocked ? "🔒" : "🔓";
        LayerLockToggled?.Invoke(this, (i, desc.IsLocked));
    }

    private void BtnAdd_OnClick(object sender, RoutedEventArgs e)
    {
        if (_tileMap == null) return;
        var menu = new ContextMenu
        {
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x21, 0x26, 0x2d)),
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xe6, 0xed, 0xf3))
        };
        var custom = new MenuItem { Header = "Capa personalizada" };
        custom.Click += (_, _) =>
        {
            var desc = new MapLayerDescriptor { Name = "Capa personalizada", LayerType = LayerType.Background, SortOrder = _tileMap!.Layers.Count };
            int idx = _tileMap.AddLayer(desc);
            RefreshList();
            ActiveLayerIndex = idx;
            LayerAdded?.Invoke(this, idx);
        };
        menu.Items.Add(custom);
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "Capas predefinidas", IsEnabled = false });
        foreach (LayerType type in Enum.GetValues(typeof(LayerType)))
        {
            var (icon, name) = type switch
            {
                LayerType.Background => ("▢", "Suelo"),
                LayerType.Solid => ("▣", "Paredes"),
                LayerType.Objects => ("◇", "Objetos"),
                LayerType.Foreground => ("▤", "Superposición"),
                _ => ("•", type.ToString())
            };
            var mi = new MenuItem { Header = $"{icon} {name}", Tag = type };
            mi.Click += (_, _) =>
            {
                if (mi.Tag is LayerType t)
                {
                    var desc = new MapLayerDescriptor { Name = name, LayerType = t, SortOrder = _tileMap!.Layers.Count };
                    int idx = _tileMap.AddLayer(desc);
                    RefreshList();
                    ActiveLayerIndex = idx;
                    LayerAdded?.Invoke(this, idx);
                }
            };
            menu.Items.Add(mi);
        }
        menu.PlacementTarget = BtnAdd;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        menu.IsOpen = true;
    }

    private void BtnRemove_OnClick(object sender, RoutedEventArgs e)
    {
        if (_tileMap == null || _activeLayerIndex < 0 || _activeLayerIndex >= _tileMap.Layers.Count) return;
        if (_tileMap.Layers.Count <= 1)
        {
            System.Windows.MessageBox.Show("Debe haber al menos una capa.", "Capas", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (_tileMap.LayerHasAnyTiles(_activeLayerIndex))
        {
            if (System.Windows.MessageBox.Show("Esta capa tiene tiles. ¿Eliminar de todos modos?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
        }
        _tileMap.RemoveLayerAt(_activeLayerIndex);
        int newActive = _activeLayerIndex >= _tileMap.Layers.Count ? _tileMap.Layers.Count - 1 : _activeLayerIndex;
        if (newActive < 0) newActive = 0;
        RefreshList();
        ActiveLayerIndex = newActive;
        LayerRemoved?.Invoke(this, EventArgs.Empty);
    }

    private void LayersList_OnContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (LayersList == null || _tileMap == null) return;
        if (e.OriginalSource is DependencyObject dep)
        {
            DependencyObject? cur = dep;
            while (cur != null && cur is not ListBoxItem)
                cur = VisualTreeHelper.GetParent(cur);
            if (cur is ListBoxItem lbi && lbi.DataContext is LayerListItem clicked)
                LayersList.SelectedItem = clicked;
        }
        if (LayersList.SelectedItem is not LayerListItem item) return;
        var menu = new ContextMenu
        {
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x21, 0x26, 0x2d)),
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xe6, 0xed, 0xf3))
        };
        var rename = new MenuItem { Header = "Renombrar" };
        rename.Click += (_, _) => BeginRename(item);
        menu.Items.Add(rename);
        var moveUp = new MenuItem { Header = "Mover arriba" };
        moveUp.Click += (_, _) =>
        {
            if (_tileMap == null || item.LayerIndex <= 0) return;
            _tileMap.MoveLayer(item.LayerIndex, item.LayerIndex - 1);
            RefreshList();
            ActiveLayerIndex = item.LayerIndex - 1;
            LayersReordered?.Invoke(this, EventArgs.Empty);
        };
        moveUp.IsEnabled = item.LayerIndex > 0;
        menu.Items.Add(moveUp);
        var moveDown = new MenuItem { Header = "Mover abajo" };
        moveDown.Click += (_, _) =>
        {
            if (_tileMap == null || item.LayerIndex < 0 || item.LayerIndex >= _tileMap.Layers.Count - 1) return;
            _tileMap.MoveLayer(item.LayerIndex, item.LayerIndex + 1);
            RefreshList();
            ActiveLayerIndex = item.LayerIndex + 1;
            LayersReordered?.Invoke(this, EventArgs.Empty);
        };
        moveDown.IsEnabled = item.LayerIndex < _tileMap.Layers.Count - 1;
        menu.Items.Add(moveDown);
        menu.Items.Add(new Separator());
        var remove = new MenuItem { Header = "Eliminar capa" };
        remove.Click += (_, _) => BtnRemove_OnClick(sender, e);
        remove.IsEnabled = _tileMap.Layers.Count > 1;
        menu.Items.Add(remove);
        LayersList.ContextMenu = menu;
    }

    public event EventHandler<int>? ActiveLayerChanged;
    public event EventHandler<MapLayerDescriptor?>? LayerSelected;
    public event EventHandler<(int layerIndex, string name)>? LayerNameChanged;
    public event EventHandler<(int layerIndex, bool visible)>? LayerVisibilityToggled;
    public event EventHandler<(int layerIndex, bool locked)>? LayerLockToggled;
    public event EventHandler<int>? LayerAdded;
    public event EventHandler? LayerRemoved;
    public event EventHandler? LayersReordered;
}
