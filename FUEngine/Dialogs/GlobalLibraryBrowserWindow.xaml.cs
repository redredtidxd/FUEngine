using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FUEngine.Core;
using FUEngine.Editor;

namespace FUEngine;

public partial class GlobalLibraryBrowserWindow : Window
{
    private readonly ProjectInfo _project;
    private string _sharedRoot = "";
    private List<GlobalLibraryEntryDto> _entries = new();

    public GlobalLibraryBrowserWindow(ProjectInfo project)
    {
        InitializeComponent();
        _project = project ?? throw new ArgumentNullException(nameof(project));
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        EngineTypography.ApplyToRoot(this);
        if (CmbKindFilter != null)
        {
            CmbKindFilter.Items.Clear();
            CmbKindFilter.Items.Add("(todos)");
            foreach (var k in GlobalLibraryKinds.All)
                CmbKindFilter.Items.Add(k);
            CmbKindFilter.SelectedIndex = 0;
        }
        ReloadManifest();
    }

    private void ReloadManifest()
    {
        var st = EngineSettings.Load();
        _sharedRoot = GlobalAssetLibraryService.ResolveSharedAssetsRoot(st);
        if (TxtBrowserHint != null)
        {
            TxtBrowserHint.Text = string.IsNullOrWhiteSpace(st.SharedAssetsPath)
                ? $"Biblioteca en (por defecto): {_sharedRoot}"
                : $"Biblioteca en: {_sharedRoot}";
        }
        var manifest = GlobalAssetLibraryService.LoadManifest(_sharedRoot);
        _entries = manifest.Entries.ToList();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var q = _entries.AsEnumerable();
        var filter = TxtFilter?.Text?.Trim() ?? "";
        if (filter.Length > 0)
        {
            q = q.Where(e =>
                (e.DisplayName?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (e.RelativePath?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (e.Tags != null && e.Tags.Any(t => t.Contains(filter, StringComparison.OrdinalIgnoreCase))));
        }
        if (CmbKindFilter?.SelectedItem is string sk && sk != "(todos)")
            q = q.Where(e => string.Equals(e.Kind, sk, StringComparison.OrdinalIgnoreCase));
        if (LibraryBrowserList != null)
            LibraryBrowserList.ItemsSource = q.ToList();
    }

    private void TxtFilter_OnTextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private void CmbKindFilter_OnSelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();

    private void BtnAddToProject_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn || btn.Tag is not GlobalLibraryEntryDto entry) return;
        var dir = _project.ProjectDirectory ?? "";
        if (string.IsNullOrWhiteSpace(dir))
        {
            System.Windows.MessageBox.Show("El proyecto no tiene carpeta asignada.", "Biblioteca", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            var registerAudio = ChkRegisterAudio?.IsChecked == true;
            var manifestRel = string.IsNullOrWhiteSpace(_project.AudioManifestPath) ? "audio.json" : _project.AudioManifestPath.Trim();
            var rel = GlobalAssetLibraryService.CopyEntryToProject(
                entry,
                _sharedRoot,
                dir,
                _project.AssetsRootFolder ?? "Assets",
                registerAudio,
                manifestRel);
            if (rel != null)
                EditorLog.Toast($"Añadido al proyecto: {rel}", LogLevel.Info, "Assets");
            else
                EditorLog.Warning("No se pudo copiar el asset (¿archivo ausente en la biblioteca?).", "Assets");
        }
        catch (Exception ex)
        {
            EditorLog.Warning($"Error al copiar: {ex.Message}", "Assets");
        }
    }
}
