using System.IO;
using System.Windows;
using System.Windows.Controls;
using FUEngine.Core;

namespace FUEngine.Dialogs;

public partial class BuildExportWindow : Window
{
    private readonly ProjectInfo _project;
    private readonly string _projectRoot;

    public BuildExportWindow(ProjectInfo project, string projectRootDirectory)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _projectRoot = projectRootDirectory ?? "";
        InitializeComponent();
        Loaded += (_, _) => BuildExportWindow_Loaded();
    }

    private void BuildExportWindow_Loaded()
    {
        CmbScene.Items.Clear();
        if (_project.Scenes == null || _project.Scenes.Count == 0)
        {
            CmbScene.Items.Add(new ComboBoxItem { Content = "Predeterminada (MainMapPath / MainObjectsPath)", Tag = -1 });
        }
        else
        {
            for (int i = 0; i < _project.Scenes.Count; i++)
            {
                var s = _project.Scenes[i];
                var label = string.IsNullOrWhiteSpace(s.Name) ? s.Id : s.Name;
                CmbScene.Items.Add(new ComboBoxItem { Content = label, Tag = i });
            }
        }

        CmbScene.SelectedIndex = 0;
        var baseName = ProjectBuildService.SanitizeExecutableBaseName(_project.Nombre);
        TxtExeName.Text = string.IsNullOrEmpty(baseName) ? "MiJuego" : baseName;
    }

    private void BtnBrowse_OnClick(object sender, RoutedEventArgs e)
    {
        using var dlg = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Carpeta donde se generará el ejecutable y Data/",
            UseDescriptionForTitle = true
        };
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            TxtOutput.Text = dlg.SelectedPath;
    }

    private void BtnCancel_OnClick(object sender, RoutedEventArgs e) => Close();

    private void BtnExport_OnClick(object sender, RoutedEventArgs e)
    {
        var outDir = TxtOutput.Text?.Trim();
        if (string.IsNullOrEmpty(outDir))
        {
            System.Windows.MessageBox.Show(this, "Elige una carpeta de salida.", "Exportar", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrEmpty(_projectRoot) || !Directory.Exists(_projectRoot))
        {
            System.Windows.MessageBox.Show(this, "La carpeta del proyecto no es válida.", "Exportar", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int sceneIndex = -1;
        if (CmbScene.SelectedItem is ComboBoxItem cb && cb.Tag is int si)
            sceneIndex = si;

        var exeName = TxtExeName.Text?.Trim() ?? "Game";
        var publish = ChkPublish.IsChecked == true;

        void AppendLog(string line)
        {
            Dispatcher.Invoke(() =>
            {
                TxtLog.AppendText(line + "\r\n");
                TxtLog.ScrollToEnd();
            });
        }

        BtnExport.IsEnabled = false;
        try
        {
            ProjectBuildService.Build(
                _project,
                _projectRoot,
                outDir,
                exeName,
                sceneIndex,
                publish,
                AppendLog);
            System.Windows.MessageBox.Show(this, "Build generada correctamente.", "Exportar", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
        catch (Exception ex)
        {
            AppendLog(ex.ToString());
            System.Windows.MessageBox.Show(this, ex.Message, "Exportar", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnExport.IsEnabled = true;
        }
    }
}

