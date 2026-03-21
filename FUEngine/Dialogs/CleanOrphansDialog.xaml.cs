using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FUEngine;

public class OrphanFileItem : INotifyPropertyChanged
{
    public string FullPath { get; set; } = "";
    private bool _isSelected;
    public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); } }
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public partial class CleanOrphansDialog : Window
{
    public List<string> PathsToDelete { get; private set; } = new();

    public CleanOrphansDialog(IEnumerable<string> orphanPaths)
    {
        InitializeComponent();
        var items = orphanPaths.Select(p => new OrphanFileItem { FullPath = p, IsSelected = false }).ToList();
        LstOrphans.ItemsSource = items;
    }

    private void BtnSelectAll_OnClick(object sender, RoutedEventArgs e)
    {
        if (LstOrphans.ItemsSource is IEnumerable<OrphanFileItem> items)
            foreach (var i in items) i.IsSelected = true;
    }

    private void BtnSelectNone_OnClick(object sender, RoutedEventArgs e)
    {
        if (LstOrphans.ItemsSource is IEnumerable<OrphanFileItem> items)
            foreach (var i in items) i.IsSelected = false;
    }

    private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
    {
        PathsToDelete.Clear();
        Close();
    }

    private void BtnDelete_OnClick(object sender, RoutedEventArgs e)
    {
        if (LstOrphans.ItemsSource is IEnumerable<OrphanFileItem> items)
            PathsToDelete = items.Where(i => i.IsSelected).Select(i => i.FullPath).ToList();
        Close();
    }
}
