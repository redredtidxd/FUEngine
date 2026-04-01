using System.Windows;
using System.Windows.Media;

namespace FUEngine;

public partial class ExplorerTabContent
{
    private ProjectExplorerItem? _selectedItem;

    public ExplorerTabContent()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            ProjectExplorer.SelectionChanged += OnExplorerSelectionChanged;
            UpdateShortcutButtons();
        };
    }

    public ProjectExplorerPanel GetExplorerPanel() => ProjectExplorer;

    private void OnExplorerSelectionChanged(object? sender, ProjectExplorerItem? item)
    {
        _selectedItem = item;
        UpdateShortcutButtons();
    }

    private void UpdateShortcutButtons()
    {
        var hasFile = _selectedItem != null && !_selectedItem.IsFolder;
        if (BtnActionOpen != null) BtnActionOpen.Visibility = hasFile ? Visibility.Visible : Visibility.Collapsed;
        if (BtnActionReveal != null) BtnActionReveal.Visibility = _selectedItem != null ? Visibility.Visible : Visibility.Collapsed;
        if (BtnActionFavorite != null) BtnActionFavorite.Visibility = _selectedItem != null ? Visibility.Visible : Visibility.Collapsed;
        if (BtnActionDuplicate != null) BtnActionDuplicate.Visibility = _selectedItem != null ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ExplorerTabContent_OnDragOver(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            e.Effects = System.Windows.DragDropEffects.Copy;
        else
            e.Effects = System.Windows.DragDropEffects.None;
        e.Handled = true;
    }

    private void ExplorerTabContent_OnDrop(object sender, System.Windows.DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            return;
        var files = (string[]?)e.Data.GetData(System.Windows.DataFormats.FileDrop);
        if (files == null || files.Length == 0)
            return;
        ProjectExplorer.ImportFilesFromPaths(files);
        e.Handled = true;
    }

    private void BtnView_OnClick(object sender, RoutedEventArgs e)
    {
        var tag = (sender as System.Windows.FrameworkElement)?.Tag as string;
        if (string.IsNullOrEmpty(tag)) return;
        ProjectExplorer.SetViewMode(tag);
        UpdateViewButtonStyles(tag);
    }

    private void UpdateViewButtonStyles(string activeTag)
    {
        if (BtnViewTree != null) BtnViewTree.Background = activeTag == "Tree" ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x38, 0x8b, 0xfd)) : new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x21, 0x26, 0x2d));
        if (BtnViewList != null) BtnViewList.Background = activeTag == "List" ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x38, 0x8b, 0xfd)) : new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x21, 0x26, 0x2d));
        if (BtnViewGrid != null) BtnViewGrid.Background = activeTag == "Grid" ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x38, 0x8b, 0xfd)) : new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x21, 0x26, 0x2d));
    }

    private void BtnBulkRename_OnClick(object sender, RoutedEventArgs e)
    {
        System.Windows.MessageBox.Show("Renombrar múltiple próximamente.", "Explorador", MessageBoxButton.OK);
    }

    private void BtnUnusedScanner_OnClick(object sender, RoutedEventArgs e)
    {
        var w = Window.GetWindow(this);
        var dialog = new UnusedAssetsDialog(ProjectExplorer.ProjectDirectory) { Owner = w };
        dialog.ShowDialog();
    }

    private void BtnActionOpen_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedItem != null && !_selectedItem.IsFolder)
            ProjectExplorer.OpenInEditor(_selectedItem);
    }

    private void BtnActionReveal_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedItem != null)
            ProjectExplorer.ShowInFolder(_selectedItem);
    }

    private void BtnActionFavorite_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedItem == null || string.IsNullOrEmpty(_selectedItem.FullPath)) return;
        var meta = ProjectExplorer.GetMetadataService();
        if (meta == null) return;
        if (meta.IsFavorite(_selectedItem.FullPath))
            meta.RemoveFavorite(_selectedItem.FullPath);
        else
            meta.AddFavorite(_selectedItem.FullPath);
        ProjectExplorer.RefreshTree();
    }

    private void BtnActionDuplicate_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedItem != null)
            ProjectExplorer.DuplicateItem(_selectedItem);
    }
}
