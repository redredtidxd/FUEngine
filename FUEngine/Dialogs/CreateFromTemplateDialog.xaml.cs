using System.Windows;
using System.Windows.Controls;

namespace FUEngine;

public partial class CreateFromTemplateDialog : Window
{
    public string ProjectName => TxtName.Text.Trim();
    public string ProjectPath => TxtPath.Text.Trim();
    public bool Infinite => ChkInfinite.IsChecked == true;
    public int MapWidth => int.TryParse(TxtMapWidth.Text, out var w) && w > 0 ? w : 64;
    public int MapHeight => int.TryParse(TxtMapHeight.Text, out var h) && h > 0 ? h : 64;
    public int TileSize => int.TryParse(TxtTileSize.Text, out var t) && t >= 8 && t <= 64 ? t : 16;

    public CreateFromTemplateDialog(TemplateItem template, TemplateData data)
    {
        InitializeComponent();
        TxtMapWidth.Text = data.Project.MapWidth.ToString();
        TxtMapHeight.Text = data.Project.MapHeight.ToString();
        TxtTileSize.Text = data.Project.TileSize.ToString();
        ChkInfinite.IsChecked = data.Project.Infinite;
    }

    private void BtnBrowse_OnClick(object sender, RoutedEventArgs e)
    {
        var defaultPath = "";
        try { defaultPath = EngineSettings.Load().DefaultProjectsPath ?? ""; } catch { }
        if (string.IsNullOrWhiteSpace(defaultPath) || !System.IO.Directory.Exists(defaultPath))
            defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        using var dlg = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Carpeta del proyecto",
            UseDescriptionForTitle = true,
            SelectedPath = defaultPath
        };
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK && !string.IsNullOrEmpty(dlg.SelectedPath))
            TxtPath.Text = dlg.SelectedPath;
    }

    private void BtnCreate_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            System.Windows.MessageBox.Show(this, "Indica el nombre del proyecto.", "Crear desde plantilla", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(TxtPath.Text))
        {
            System.Windows.MessageBox.Show(this, "Selecciona la carpeta del proyecto.", "Crear desde plantilla", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
        Close();
    }
}
