using System.Collections;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

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
        if (ChkFilterInfo != null && ChkFilterWarning != null && ChkFilterError != null)
        {
            var levelOk = entry.Level switch
            {
                LogLevel.Info => ChkFilterInfo.IsChecked == true,
                LogLevel.Warning => ChkFilterWarning.IsChecked == true,
                LogLevel.Error => ChkFilterError.IsChecked == true,
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
}
