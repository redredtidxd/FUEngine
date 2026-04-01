using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using FUEngine.Core;

namespace FUEngine;

public partial class LogPanel : System.Windows.Controls.UserControl
{
    private ICollectionView? _logView;

    public LogPanel()
    {
        InitializeComponent();
        Loaded += LogPanel_OnLoaded;
        Unloaded += LogPanel_OnUnloaded;
    }

    private void LogPanel_OnLoaded(object sender, RoutedEventArgs e)
    {
        _logView = System.Windows.Data.CollectionViewSource.GetDefaultView(EditorLog.Entries);
        _logView.Filter = FilterLogEntry;
        if (LogList != null) LogList.ItemsSource = _logView;
        EditorLog.EntryAdded += EditorLog_OnEntryAdded;
    }

    private void LogPanel_OnUnloaded(object sender, RoutedEventArgs e)
    {
        EditorLog.EntryAdded -= EditorLog_OnEntryAdded;
    }

    private bool FilterLogEntry(object obj)
    {
        if (obj is not LogEntry entry) return false;
        if (ChkFilterInfo != null && ChkFilterWarning != null && ChkFilterError != null && ChkFilterCritical != null && ChkFilterLua != null)
        {
            var levelOk = entry.Level switch
            {
                LogLevel.Info => ChkFilterInfo.IsChecked == true,
                LogLevel.Warning => ChkFilterWarning.IsChecked == true,
                LogLevel.Error => ChkFilterError.IsChecked == true,
                LogLevel.Critical => ChkFilterCritical.IsChecked == true,
                LogLevel.Lua => ChkFilterLua.IsChecked == true,
                _ => true
            };
            if (!levelOk) return false;
        }

        if (CmbFilterSource?.SelectedItem is ComboBoxItem item && item.Tag is string tag && !string.IsNullOrEmpty(tag))
        {
            if (string.IsNullOrEmpty(entry.Source) || !string.Equals(entry.Source, tag, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        var q = TxtSearch?.Text?.Trim();
        if (!string.IsNullOrEmpty(q))
        {
            var inMessage = entry.Message?.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
            var inSource = entry.Source?.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
            if (!inMessage && !inSource) return false;
        }

        return true;
    }

    private void Filter_OnChanged(object sender, RoutedEventArgs e) => _logView?.Refresh();

    private void TxtSearch_OnTextChanged(object sender, TextChangedEventArgs e) => _logView?.Refresh();

    private void EditorLog_OnEntryAdded(object? sender, LogEntry e)
    {
        if (LogList?.Items.Count <= 0) return;
        var listView = LogList;
        Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
        {
            try
            {
                if (listView?.Items.Count > 0)
                    listView.ScrollIntoView(listView.Items[^1]!);
            }
            catch
            {
                /* ListView virtualización / timing */
            }
        });
    }

    private void LogList_OnPreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (LogList == null) return;
        var hit = e.OriginalSource as DependencyObject;
        while (hit != null && hit is not System.Windows.Controls.ListViewItem)
            hit = VisualTreeHelper.GetParent(hit);
        if (hit is System.Windows.Controls.ListViewItem lvi && lvi.DataContext is LogEntry entry)
        {
            LogList.SelectedItem = entry;
            lvi.Focus();
        }
    }

    private void LogContextMenu_OnOpened(object sender, RoutedEventArgs e)
    {
        if (MenuCopyFilePath == null || MenuCopyCriticalSupport == null) return;
        if (LogList?.SelectedItem is not LogEntry entry)
        {
            MenuCopyFilePath.Visibility = Visibility.Collapsed;
            MenuCopyCriticalSupport.Visibility = Visibility.Collapsed;
            return;
        }

        var hasPath = !string.IsNullOrEmpty(TryResolveLogFilePath(entry));
        MenuCopyFilePath.Visibility = hasPath ? Visibility.Visible : Visibility.Collapsed;
        MenuCopyCriticalSupport.Visibility = entry.Level == LogLevel.Critical ? Visibility.Visible : Visibility.Collapsed;
    }

    private void LogContextMenu_CopyLine_OnClick(object sender, RoutedEventArgs e)
    {
        if (LogList?.SelectedItem is not LogEntry entry) return;
        TrySetClipboard(entry.FormattedLine);
    }

    private void LogContextMenu_CopyFilePath_OnClick(object sender, RoutedEventArgs e)
    {
        if (LogList?.SelectedItem is not LogEntry entry) return;
        var path = TryResolveLogFilePath(entry);
        if (string.IsNullOrEmpty(path)) return;
        TrySetClipboard(path);
    }

    private void LogContextMenu_CopyCriticalSupport_OnClick(object sender, RoutedEventArgs e)
    {
        if (LogList?.SelectedItem is not LogEntry entry || entry.Level != LogLevel.Critical) return;
        var sb = new StringBuilder();
        sb.AppendLine("FUEngine — reporte de consola (nivel Crítico)");
        sb.AppendLine($"Motor: {EngineVersion.Current}");
        sb.AppendLine(entry.FormattedLine);
        var resolved = TryResolveLogFilePath(entry);
        if (!string.IsNullOrWhiteSpace(resolved))
            sb.AppendLine($"Archivo: {resolved}");
        else if (!string.IsNullOrWhiteSpace(entry.FilePath))
            sb.AppendLine($"Archivo: {entry.FilePath}");
        if (entry.Line is int ln && ln > 0)
            sb.AppendLine($"Línea: {ln}");
        TrySetClipboard(sb.ToString().TrimEnd());
    }

    /// <summary>Ruta explícita en la entrada o inferida del mensaje (JSON entre paréntesis, script.lua:línea:, etc.).</summary>
    private static string? TryResolveLogFilePath(LogEntry entry)
    {
        var p = entry.FilePath?.Trim();
        if (!string.IsNullOrEmpty(p))
            return p;

        var msg = entry.Message;
        if (string.IsNullOrEmpty(msg)) return null;

        var m = Regex.Match(msg.Trim(), @"^([\w/\\\.\-]+\.lua):(\d+):", RegexOptions.IgnoreCase);
        if (m.Success)
            return m.Groups[1].Value.Replace('/', Path.DirectorySeparatorChar);

        m = Regex.Match(msg, @"\(([^)]+\.(?:json|lua|fue|config))\)", RegexOptions.IgnoreCase);
        if (m.Success && LooksLikeFilePath(m.Groups[1].Value))
            return m.Groups[1].Value.Trim();

        m = Regex.Match(msg, @"\(([^)]+)\)");
        if (m.Success && LooksLikeFilePath(m.Groups[1].Value))
            return m.Groups[1].Value.Trim();

        return null;
    }

    private static bool LooksLikeFilePath(string s)
    {
        s = s.Trim();
        if (s.Length < 2) return false;
        if (s.Length >= 3 && char.IsLetter(s[0]) && s[1] == ':') return true;
        if (s.StartsWith("\\\\", StringComparison.Ordinal)) return true;
        if (s.Contains('\\', StringComparison.Ordinal) || s.Contains('/')) return true;
        return Regex.IsMatch(s, @"\.(json|lua|fue|config|xml)$", RegexOptions.IgnoreCase);
    }

    private static void TrySetClipboard(string text)
    {
        try
        {
            System.Windows.Clipboard.SetText(text);
        }
        catch
        {
            /* Portapapeles bloqueado u otro proceso */
        }
    }

    private void LogList_OnMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (LogList?.SelectedItem is not LogEntry entry) return;

        var path = entry.FilePath?.Trim().Replace('\\', '/');
        int? line = entry.Line;

        if (string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(entry.Message))
        {
            var m = Regex.Match(entry.Message.Trim(), @"^([\w/\\\.\-]+\.lua):(\d+):", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                path = m.Groups[1].Value.Replace('\\', '/');
                if (int.TryParse(m.Groups[2].Value, out var ln))
                    line = ln;
            }
        }

        if (line is null or < 1 && !string.IsNullOrEmpty(entry.Message))
        {
            var m = Regex.Match(entry.Message, @":(\d+):");
            if (m.Success && int.TryParse(m.Groups[1].Value, out var ln))
                line = ln;
        }

        if (string.IsNullOrEmpty(path) || line is null or < 1) return;
        EditorLog.RaiseRequestOpenFileAtLine(path, line.Value);
    }

    private void BtnClear_OnClick(object sender, RoutedEventArgs e) => EditorLog.Clear();

    private void BtnStressTest_OnClick(object sender, RoutedEventArgs e)
    {
        EditorLog.Info("Prueba Info (consola centralizada).", "Test");
        EditorLog.Warning("Prueba aviso.", "Test");
        EditorLog.Error("Prueba error simulado.", "Test");
        EditorLog.Critical("Prueba crítico (invariante / I/O simulado).", "Test");
        EditorLog.Log("print() simulado desde Lua", LogLevel.Lua, "Lua");
        try
        {
            JsonDocument.Parse("{\"a\":1");
        }
        catch (JsonException jex)
        {
            var loc = jex.LineNumber.HasValue
                ? $"línea {jex.LineNumber}, posición {jex.BytePositionInLine}"
                : "sin línea";
            EditorLog.Critical($"JSON simulado (parse inválido): {loc}: {jex.Message}", "IO");
        }
    }
}
