using System.IO;
using System.Windows;
using FUEngine.Core;
using FUEngine.Editor;

namespace FUEngine;

public partial class ExportPartialWindow : Window
{
    private readonly ProjectInfo _project;
    private readonly TileMap _tileMap;
    private readonly ObjectLayer _objectLayer;
    private readonly ScriptRegistry _scriptRegistry;

    public ExportPartialWindow(ProjectInfo project, TileMap tileMap, ObjectLayer objectLayer, ScriptRegistry scriptRegistry)
    {
        _project = project;
        _tileMap = tileMap;
        _objectLayer = objectLayer;
        _scriptRegistry = scriptRegistry;
        InitializeComponent();
    }

    private void BtnBrowse_OnClick(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Elige un archivo dentro de la carpeta de destino (se usará su carpeta)",
            Filter = "Todos|*.*"
        };
        if (dlg.ShowDialog() == true)
        {
            var dir = Path.GetDirectoryName(dlg.FileName);
            if (!string.IsNullOrEmpty(dir)) TxtDestPath.Text = dir;
        }
    }

    private void BtnExport_OnClick(object sender, RoutedEventArgs e)
    {
        var dest = TxtDestPath?.Text?.Trim();
        if (string.IsNullOrEmpty(dest))
        {
            System.Windows.MessageBox.Show(this, "Elige una carpeta de destino.", "Exportar", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            Directory.CreateDirectory(dest);
            if (ChkMapa?.IsChecked == true && File.Exists(_project.MapPath))
                File.Copy(_project.MapPath, Path.Combine(dest, "mapa.json"), true);
            if (ChkObjetos?.IsChecked == true && File.Exists(_project.ObjectsPath))
                File.Copy(_project.ObjectsPath, Path.Combine(dest, "objetos.json"), true);
            if (ChkScripts?.IsChecked == true && File.Exists(_project.ScriptsPath))
                File.Copy(_project.ScriptsPath, Path.Combine(dest, "scripts.json"), true);
            if (ChkAnimaciones?.IsChecked == true && File.Exists(_project.AnimacionesPath))
                File.Copy(_project.AnimacionesPath, Path.Combine(dest, "animaciones.json"), true);
            if (ChkProyecto?.IsChecked == true)
            {
                var dir = _project.ProjectDirectory ?? "";
                var fuePath = Path.Combine(dir, FUEngine.Editor.NewProjectStructure.ProjectFileName);
                var projPath = Path.Combine(dir, "proyecto.json");
                var projectJson = Path.Combine(dir, "Project.json");
                var src = File.Exists(fuePath) ? fuePath : File.Exists(projPath) ? projPath : File.Exists(projectJson) ? projectJson : null;
                if (src != null)
                    File.Copy(src, Path.Combine(dest, Path.GetFileName(src)), true);
            }
            EditorLog.Info($"Exportación parcial a: {dest}", "Exportar");
            System.Windows.MessageBox.Show(this, "Exportación completada.", "Exportar", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCancel_OnClick(object sender, RoutedEventArgs e) => Close();
}
