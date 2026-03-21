using System.IO;
using System.Linq;
using System.Windows;

namespace FUEngine;

public partial class UnusedAssetsDialog : Window
{
    public UnusedAssetsDialog(string projectDirectory)
    {
        InitializeComponent();
        var unused = UnusedAssetScanner.GetUnusedFiles(projectDirectory ?? "");
        var display = unused.Select(p => Path.GetFileName(p) + "  →  " + p).ToList();
        UnusedList.ItemsSource = display;
        TxtSummary.Text = display.Count == 0
            ? "No se encontraron assets no usados."
            : $"{display.Count} archivo(s) que no están referenciados por mapa, objetos ni seeds:";
    }

    private void BtnClose_OnClick(object sender, RoutedEventArgs e) => Close();
}
