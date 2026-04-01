using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FUEngine;

public class AssetScanItem
{
    public string RelativePath { get; set; } = "";
    public string AssetType { get; set; } = "Otro";
    public string DisplayText => $"[{AssetType}] {Path.GetFileName(RelativePath)}";
}

public partial class ImportSceneAssetScanDialog : Window
{
    public bool UserChoseImport { get; private set; }
    private readonly List<AssetScanItem> _found;
    private readonly string? _sourceDir;
    private readonly string? _destDir;

    public ImportSceneAssetScanDialog(IReadOnlyList<AssetScanItem> found, IReadOnlyList<AssetScanItem> missing,
        string? sourceProjectDir, string? destProjectDir)
    {
        InitializeComponent();
        _found = found?.ToList() ?? new List<AssetScanItem>();
        _sourceDir = sourceProjectDir;
        _destDir = destProjectDir;
        LstFound.ItemsSource = _found.Count == 0 ? new[] { new AssetScanItem { RelativePath = "(ninguno)", AssetType = "" } } : _found;
        LstMissing.ItemsSource = (missing?.Count ?? 0) == 0 ? new[] { new AssetScanItem { RelativePath = "(ninguno)", AssetType = "" } } : missing;
        BtnCopyAssets.Visibility = _found.Count > 0 && !string.IsNullOrEmpty(_sourceDir) && !string.IsNullOrEmpty(_destDir) ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BtnCopyAssets_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_sourceDir) || string.IsNullOrEmpty(_destDir) || _found.Count == 0) return;
        var assetsRoot = Path.Combine(_destDir, "Assets");
        var toCopy = _found.Where(i => !string.IsNullOrEmpty(i.RelativePath) && i.RelativePath != "(ninguno)").ToList();
        var existing = new List<string>();
        foreach (var item in toCopy)
        {
            var dest = Path.Combine(assetsRoot, item.RelativePath);
            if (File.Exists(dest)) existing.Add(item.RelativePath);
        }
        var overwrite = true;
        var renameIfExists = false;
        if (existing.Count > 0)
        {
            var result = System.Windows.MessageBox.Show(this,
                existing.Count + " archivo(s) ya existen en el proyecto.\n\n¿Sobrescribir?\n• Sí = reemplazar los existentes.\n• No = renombrar (ej. enemy.png → enemy_1.png).\n• Cancelar = no copiar.",
                "Duplicados detectados",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);
            if (result == MessageBoxResult.Cancel) return;
            overwrite = result == MessageBoxResult.Yes;
            renameIfExists = result == MessageBoxResult.No;
        }
        var copied = 0;
        foreach (var item in toCopy)
        {
            var src = Path.Combine(_sourceDir, item.RelativePath);
            if (!File.Exists(src)) continue;
            var dest = Path.Combine(assetsRoot, item.RelativePath);
            if (File.Exists(dest) && renameIfExists)
                dest = GetNextAvailablePath(dest);
            else if (File.Exists(dest) && !overwrite)
                continue;
            try
            {
                var destDir = Path.GetDirectoryName(dest);
                if (!string.IsNullOrEmpty(destDir)) Directory.CreateDirectory(destDir);
                File.Copy(src, dest, overwrite: overwrite);
                copied++;
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show(this, "Error al copiar " + item.RelativePath + ": " + ex.Message, "Copiar assets", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        if (copied > 0)
            System.Windows.MessageBox.Show(this, copied + " archivo(s) copiados a " + assetsRoot, "Copiar assets", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static string GetNextAvailablePath(string basePath)
    {
        var dir = Path.GetDirectoryName(basePath) ?? "";
        var name = Path.GetFileNameWithoutExtension(basePath);
        var ext = Path.GetExtension(basePath);
        for (var n = 1; n <= 9999; n++)
        {
            var candidate = Path.Combine(dir, name + "_" + n + ext);
            if (!File.Exists(candidate)) return candidate;
        }
        return Path.Combine(dir, name + "_" + Guid.NewGuid().ToString("N")[..8] + ext);
    }

    private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
    {
        UserChoseImport = false;
        Close();
    }

    private void BtnImport_OnClick(object sender, RoutedEventArgs e)
    {
        UserChoseImport = true;
        Close();
    }
}
