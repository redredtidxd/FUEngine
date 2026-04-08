using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace FUEngine;

public partial class LocalizationTabContent : WpfUserControl
{
    public event EventHandler<bool>? DirtyChanged;

    private string? _projectDir;
    private readonly LocalizationFileData _model = new();
    private DataTable _table = new();
    private bool _suspendDirty;
    private bool _loading;

    public LocalizationTabContent()
    {
        InitializeComponent();
    }

    public void SetProjectDirectory(string dir)
    {
        _projectDir = string.IsNullOrWhiteSpace(dir) ? null : dir.Trim();
        ReloadFromDisk();
    }

    private string JsonAbsolutePath =>
        Path.Combine(_projectDir ?? "", LocalizationFileData.DefaultRelativePath.Replace('/', Path.DirectorySeparatorChar));

    private void ReloadFromDisk()
    {
        _loading = true;
        _suspendDirty = true;
        _model.DefaultLocale = "es";
        _model.FallbackLocale = "en";
        _model.Entries.Clear();
        if (!string.IsNullOrEmpty(_projectDir))
        {
            if (!LocalizationFileData.TryLoad(JsonAbsolutePath, out var data, out _))
                data = new LocalizationFileData();
            _model.DefaultLocale = data.DefaultLocale;
            _model.FallbackLocale = data.FallbackLocale;
            foreach (var kv in data.Entries)
                _model.Entries[kv.Key] = new Dictionary<string, string>(kv.Value, StringComparer.OrdinalIgnoreCase);
        }

        TxtDefaultLocale.Text = _model.DefaultLocale;
        TxtFallbackLocale.Text = _model.FallbackLocale;
        RebuildTableFromModel();
        _suspendDirty = false;
        _loading = false;
        DirtyChanged?.Invoke(this, false);
    }

    private void RebuildTableFromModel()
    {
        var locales = _model.CollectAllLocaleCodes().OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        _table = new DataTable();
        _table.Columns.Add("Key", typeof(string));
        foreach (var loc in locales)
            _table.Columns.Add(loc.ToUpperInvariant(), typeof(string));

        foreach (var kv in _model.Entries.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            var row = _table.NewRow();
            row["Key"] = kv.Key;
            foreach (var loc in locales)
            {
                kv.Value.TryGetValue(loc, out var txt);
                row[loc.ToUpperInvariant()] = txt ?? "";
            }

            _table.Rows.Add(row);
        }

        GridLoc.ItemsSource = _table.DefaultView;
    }

    private void MarkDirty()
    {
        if (_suspendDirty || _loading) return;
        DirtyChanged?.Invoke(this, true);
    }

    private static string NormLocaleField(string? s)
    {
        var t = (s ?? "").Trim().ToLowerInvariant();
        return t.Length >= 2 ? t[..2] : (string.IsNullOrEmpty(t) ? "en" : t);
    }

    private void PushLocaleFieldsIntoModel()
    {
        _model.DefaultLocale = NormLocaleField(TxtDefaultLocale.Text);
        _model.FallbackLocale = NormLocaleField(TxtFallbackLocale.Text);
    }

    private void SyncModelFromTable()
    {
        PushLocaleFieldsIntoModel();
        _model.Entries.Clear();
        foreach (DataRow row in _table.Rows)
        {
            if (row.RowState == DataRowState.Deleted) continue;
            var key = (row["Key"]?.ToString() ?? "").Trim();
            if (string.IsNullOrEmpty(key)) continue;
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataColumn col in _table.Columns)
            {
                if (col.ColumnName.Equals("Key", StringComparison.OrdinalIgnoreCase)) continue;
                var code = col.ColumnName.Trim().ToLowerInvariant();
                code = code.Length >= 2 ? code[..2] : code;
                if (string.IsNullOrEmpty(code)) continue;
                var val = row[col]?.ToString() ?? "";
                map[code] = val;
            }

            _model.Entries[key] = map;
        }
    }

    private void BtnReload_OnClick(object sender, RoutedEventArgs e)
    {
        ReloadFromDisk();
    }

    private void BtnSaveJson_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_projectDir))
        {
            System.Windows.MessageBox.Show("No hay proyecto abierto.", "Localización", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SyncModelFromTable();
        _model.Save(JsonAbsolutePath);
        DirtyChanged?.Invoke(this, false);
        System.Windows.MessageBox.Show($"Guardado:\n{JsonAbsolutePath}", "Localización", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnImportCsv_OnClick(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "CSV (*.csv)|*.csv|Todos|*.*",
            Title = "Importar localización CSV"
        };
        if (dlg.ShowDialog() != true) return;
        if (!LocalizationCsvIO.TryImport(dlg.FileName, _model, out var err))
        {
            System.Windows.MessageBox.Show(err ?? "Error al importar.", "Localización", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        TxtDefaultLocale.Text = _model.DefaultLocale;
        TxtFallbackLocale.Text = _model.FallbackLocale;
        RebuildTableFromModel();
        MarkDirty();
    }

    private void BtnExportCsv_OnClick(object sender, RoutedEventArgs e)
    {
        SyncModelFromTable();
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv",
            FileName = "localization_export.csv",
            Title = "Exportar localización CSV"
        };
        if (dlg.ShowDialog() != true) return;
        var locales = _table.Columns.Cast<DataColumn>()
            .Where(c => !c.ColumnName.Equals("Key", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.ColumnName)
            .ToList();
        if (!LocalizationCsvIO.TryExport(dlg.FileName, _model, locales, out var err))
            System.Windows.MessageBox.Show(err ?? "Error al exportar.", "Localización", MessageBoxButton.OK, MessageBoxImage.Error);
        else
            System.Windows.MessageBox.Show($"Exportado:\n{dlg.FileName}", "Localización", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnAddRow_OnClick(object sender, RoutedEventArgs e)
    {
        var row = _table.NewRow();
        row["Key"] = $"KEY_{_table.Rows.Count + 1}";
        foreach (DataColumn col in _table.Columns)
        {
            if (!col.ColumnName.Equals("Key", StringComparison.OrdinalIgnoreCase))
                row[col.ColumnName] = "";
        }

        _table.Rows.Add(row);
        MarkDirty();
    }

    private void BtnRemoveRow_OnClick(object sender, RoutedEventArgs e)
    {
        if (GridLoc.SelectedItem is not DataRowView drv) return;
        drv.Row.Delete();
        MarkDirty();
    }

    private void GridLoc_OnCellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        MarkDirty();
    }

    private void LocaleField_OnLostFocus(object sender, RoutedEventArgs e)
    {
        MarkDirty();
    }
}
