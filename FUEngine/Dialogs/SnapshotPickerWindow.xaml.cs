using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FUEngine;

public partial class SnapshotPickerWindow : Window
{
    public string? SelectedName { get; private set; }

    public SnapshotPickerWindow(IEnumerable<string> snapshotNames)
    {
        InitializeComponent();
        SnapshotList.ItemsSource = snapshotNames.ToList();
        if (SnapshotList.Items.Count > 0) SnapshotList.SelectedIndex = 0;
    }

    private void SnapshotList_OnDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SnapshotList.SelectedItem is string) Accept();
    }

    private void BtnAceptar_OnClick(object sender, RoutedEventArgs e) => Accept();
    private void BtnCancelar_OnClick(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Accept()
    {
        SelectedName = SnapshotList.SelectedItem as string;
        DialogResult = !string.IsNullOrEmpty(SelectedName);
        Close();
    }
}
