using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
namespace FUEngine;

public partial class AudioTabContent : System.Windows.Controls.UserControl
{
    private AudioAssetRegistry? _registry;
    private AudioSystem? _audioSystem;
    private List<AudioAssetEntry> _allEntries = new();

    /// <summary>Se dispara al pulsar "Play in Game"; el editor debe reproducir el ID en el runtime activo.</summary>
    public event Action<string>? RequestPlayInGame;

    public AudioTabContent()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            FilterType.Items.Clear();
            FilterType.Items.Add("All");
            FilterType.Items.Add("SFX");
            FilterType.Items.Add("Music");
            FilterType.Items.Add("Ambient");
            FilterType.SelectedIndex = 0;
            if (SearchBox != null) SearchBox.TextChanged += (s, _) => ApplyFilter();
        };
    }

    public void SetContext(AudioAssetRegistry registry, AudioSystem audioSystem)
    {
        if (_registry != null)
            _registry.RegistryChanged -= OnRegistryChanged;
        _registry = registry;
        _audioSystem = audioSystem;
        if (_registry != null)
            _registry.RegistryChanged += OnRegistryChanged;
        RefreshList();
    }

    private void OnRegistryChanged()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => RefreshList());
            return;
        }
        RefreshList();
    }

    private void RefreshList()
    {
        _allEntries = _registry?.GetAll().ToList() ?? new List<AudioAssetEntry>();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var search = SearchBox?.Text?.Trim() ?? "";
        var typeFilter = FilterType?.SelectedItem?.ToString() ?? "All";
        var filtered = _allEntries.AsEnumerable();
        if (!string.IsNullOrEmpty(search))
        {
            var s = search.ToLowerInvariant();
            filtered = filtered.Where(e =>
                (e.Id?.ToLowerInvariant().Contains(s) == true) ||
                (e.Name?.ToLowerInvariant().Contains(s) == true));
        }
        if (typeFilter != "All")
        {
            var t = typeFilter.ToLowerInvariant();
            filtered = filtered.Where(e => string.Equals(e.Type, t, System.StringComparison.OrdinalIgnoreCase));
        }
        var list = filtered.ToList();
        AudioList.ItemsSource = list;
    }

    private void FilterType_OnSelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();

    private void AudioList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var entry = AudioList.SelectedItem as AudioAssetEntry;
        if (entry == null)
        {
            InspectorTitle.Text = "Inspector — Selecciona un sonido";
            TxtFile.Text = "—";
            TxtType.Text = "—";
            TxtDuration.Text = "—";
            return;
        }
        InspectorTitle.Text = $"Inspector — {entry.Id}";
        TxtFile.Text = entry.Name ?? "—";
        TxtType.Text = entry.Type ?? "—";
        TxtDuration.Text = entry.DurationSeconds > 0 ? $"{entry.DurationSeconds:F1}s" : "—";
    }

    private void BtnPreview_OnClick(object sender, RoutedEventArgs e)
    {
        var entry = AudioList.SelectedItem as AudioAssetEntry;
        if (entry != null)
            _audioSystem?.PlayPreview(entry.Id);
    }

    private void BtnStop_OnClick(object sender, RoutedEventArgs e)
    {
        _audioSystem?.StopPreview();
    }

    private void BtnReveal_OnClick(object sender, RoutedEventArgs e)
    {
        var entry = AudioList.SelectedItem as AudioAssetEntry;
        if (entry == null || string.IsNullOrEmpty(entry.FullPath) || !System.IO.File.Exists(entry.FullPath)) return;
        try
        {
            Process.Start("explorer.exe", $"/select,\"{entry.FullPath}\"");
        }
        catch { /* ignore */ }
    }

    private void BtnCopyId_OnClick(object sender, RoutedEventArgs e)
    {
        var entry = AudioList.SelectedItem as AudioAssetEntry;
        if (entry == null) return;
        try
        {
            System.Windows.Clipboard.SetText(entry.Id ?? "");
        }
        catch { /* ignore */ }
    }

    private void BtnCopyLua_OnClick(object sender, RoutedEventArgs e)
    {
        var entry = AudioList.SelectedItem as AudioAssetEntry;
        if (entry == null) return;
        try
        {
            System.Windows.Clipboard.SetText($"Audio.play(\"{entry.Id}\")");
        }
        catch { /* ignore */ }
    }

    private void BtnPlayInGame_OnClick(object sender, RoutedEventArgs e)
    {
        var entry = AudioList.SelectedItem as AudioAssetEntry;
        if (entry == null) return;
        RequestPlayInGame?.Invoke(entry.Id);
    }
}
