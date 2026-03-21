using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FUEngine.Core;

namespace FUEngine;

/// <summary>Elemento de lista para el panel de capas (nombre, iconos, índice).</summary>
public class LayerListItem : INotifyPropertyChanged
{
    private string _displayName = "";
    private string _visibilityIcon = "👁";
    private string _lockIcon = "🔓";

    public int LayerIndex { get; set; }
    public MapLayerDescriptor Descriptor { get; set; } = null!;

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
        if (_tileMap == null) return;
        for (int i = 0; i < _tileMap.Layers.Count; i++)
        {
            var desc = _tileMap.Layers[i];
            _items.Add(new LayerListItem
            {
                LayerIndex = i,
                Descriptor = desc,
                DisplayName = desc.Name,
                VisibilityIcon = desc.IsVisible ? "👁" : "👁‍🗨",
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
        if (LayersList?.SelectedItem is LayerListItem item && item.LayerIndex != _activeLayerIndex)
        {
            _activeLayerIndex = item.LayerIndex;
            ActiveLayerChanged?.Invoke(this, _activeLayerIndex);
            LayerSelected?.Invoke(this, item.Descriptor);
        }
    }

    private void LayersList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (LayersList?.SelectedItem is not LayerListItem item) return;
        var dialog = MapHierarchyPanel.ShowRenameDialogPublic("Renombrar capa", item.DisplayName);
        if (string.IsNullOrWhiteSpace(dialog)) return;
        item.Descriptor.Name = dialog.Trim();
        item.DisplayName = dialog.Trim();
        LayerNameChanged?.Invoke(this, (item.LayerIndex, dialog.Trim()));
    }

    private void BtnVisible_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn || btn.DataContext is not LayerListItem item || _tileMap == null) return;
        int i = item.LayerIndex;
        if (i < 0 || i >= _tileMap.Layers.Count) return;
        var desc = _tileMap.Layers[i];
        desc.IsVisible = !desc.IsVisible;
        item.VisibilityIcon = desc.IsVisible ? "👁" : "👁‍🗨";
        LayerVisibilityToggled?.Invoke(this, (i, desc.IsVisible));
    }

    private void BtnLock_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn || btn.DataContext is not LayerListItem item || _tileMap == null) return;
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
                    var desc = new MapLayerDescriptor { Name = name, LayerType = t, SortOrder = _tileMap.Layers.Count };
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

    private void BtnMoveUp_OnClick(object sender, RoutedEventArgs e)
    {
        if (_tileMap == null || _activeLayerIndex <= 0) return;
        _tileMap.MoveLayer(_activeLayerIndex, _activeLayerIndex - 1);
        RefreshList();
        ActiveLayerIndex = _activeLayerIndex - 1;
        LayersReordered?.Invoke(this, EventArgs.Empty);
    }

    private void BtnMoveDown_OnClick(object sender, RoutedEventArgs e)
    {
        if (_tileMap == null || _activeLayerIndex < 0 || _activeLayerIndex >= _tileMap.Layers.Count - 1) return;
        _tileMap.MoveLayer(_activeLayerIndex, _activeLayerIndex + 1);
        RefreshList();
        ActiveLayerIndex = _activeLayerIndex + 1;
        LayersReordered?.Invoke(this, EventArgs.Empty);
    }

    private void LayersList_OnContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (LayersList?.SelectedItem is not LayerListItem item || _tileMap == null) return;
        var menu = new ContextMenu
        {
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x21, 0x26, 0x2d)),
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xe6, 0xed, 0xf3))
        };
        var rename = new MenuItem { Header = "Renombrar" };
        rename.Click += (_, _) =>
        {
            var dialog = MapHierarchyPanel.ShowRenameDialogPublic("Renombrar capa", item.DisplayName);
            if (string.IsNullOrWhiteSpace(dialog)) return;
            item.Descriptor.Name = dialog.Trim();
            item.DisplayName = dialog.Trim();
            LayerNameChanged?.Invoke(this, (item.LayerIndex, dialog.Trim()));
        };
        menu.Items.Add(rename);
        var moveUp = new MenuItem { Header = "Mover arriba" };
        moveUp.Click += (_, _) => BtnMoveUp_OnClick(sender, e);
        moveUp.IsEnabled = item.LayerIndex > 0;
        menu.Items.Add(moveUp);
        var moveDown = new MenuItem { Header = "Mover abajo" };
        moveDown.Click += (_, _) => BtnMoveDown_OnClick(sender, e);
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
    public event EventHandler<MapLayerDescriptor>? LayerSelected;
    public event EventHandler<(int layerIndex, string name)>? LayerNameChanged;
    public event EventHandler<(int layerIndex, bool visible)>? LayerVisibilityToggled;
    public event EventHandler<(int layerIndex, bool locked)>? LayerLockToggled;
    public event EventHandler<int>? LayerAdded;
    public event EventHandler? LayerRemoved;
    public event EventHandler? LayersReordered;
}
