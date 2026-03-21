using System.IO;
using System.Windows;
using System.Windows.Controls;
using FUEngine.Editor;

namespace FUEngine;

/// <summary>Panel para crear proyecto, usable en overlay dentro de StartupWindow (misma ventana).</summary>
public partial class NewProjectPanel : System.Windows.Controls.UserControl
{
    public event EventHandler? CreateClicked;
    public event EventHandler? CancelClicked;

    public string ProjectName => TxtName?.Text?.Trim() ?? "";
    public string Description => TxtDescription?.Text?.Trim() ?? "";
    public string ProjectPath => TxtPath?.Text?.Trim() ?? "";
    public bool CreateProjectFolderIfMissing => ChkCreateFolder?.IsChecked == true;

    public NewProjectPanel()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateDefaultPath();
    }

    private void TxtName_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateDefaultPath();
    }

    private void UpdateDefaultPath()
    {
        if (TxtPath == null) return;
        var name = TxtName?.Text?.Trim() ?? "";
        var root = EngineSettings.EnsureDefaultProjectsRoot();
        var folder = NewProjectStructure.SanitizeFolderName(name);
        var uniquePath = NewProjectStructure.GetUniqueProjectPath(root, folder);
        TxtPath.Text = uniquePath;
    }

    private void BtnBrowse_OnClick(object sender, RoutedEventArgs e)
    {
        using var dlg = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Carpeta donde se creará el proyecto",
            SelectedPath = TxtPath?.Text ?? ""
        };
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK && TxtPath != null)
            TxtPath.Text = dlg.SelectedPath;
    }

    private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
    {
        CancelClicked?.Invoke(this, EventArgs.Empty);
    }

    private void BtnCreate_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ProjectName))
        {
            System.Windows.MessageBox.Show(System.Windows.Window.GetWindow(this), "Escribe el nombre del proyecto.", "Crear proyecto", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(ProjectPath))
        {
            System.Windows.MessageBox.Show(System.Windows.Window.GetWindow(this), "Selecciona la carpeta del proyecto.", "Crear proyecto", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }
        if (!CreateProjectFolderIfMissing && !Directory.Exists(ProjectPath))
        {
            System.Windows.MessageBox.Show(System.Windows.Window.GetWindow(this), "La carpeta no existe. Activa \"Crear la carpeta si no existe\" o elige otra ruta.", "Crear proyecto", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }
        CreateClicked?.Invoke(this, EventArgs.Empty);
    }
}
