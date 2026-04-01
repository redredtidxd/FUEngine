using System.Collections.Generic;
using System.Linq;
using WpfComboBox = System.Windows.Controls.ComboBox;

namespace FUEngine;

/// <summary>Presets para el combo «Fuente del motor» (familia + tamaño).</summary>
public static class EngineFontPresets
{
    public sealed record Entry(string Display, string Family, int Size);

    public static readonly IReadOnlyList<Entry> All = new[]
    {
        new Entry("Segoe UI — 12", "Segoe UI", 12),
        new Entry("Segoe UI — 11", "Segoe UI", 11),
        new Entry("Segoe UI — 14", "Segoe UI", 14),
        new Entry("Consolas — 12", "Consolas", 12),
        new Entry("Consolas — 11", "Consolas", 11),
        new Entry("Cascadia Mono — 12", "Cascadia Mono", 12),
        new Entry("Cascadia Mono — 11", "Cascadia Mono", 11),
    };

    public static void FillCombo(WpfComboBox? combo)
    {
        if (combo == null) return;
        combo.ItemsSource = All.Select(e => e.Display).ToList();
    }

    public static void ApplySelectionToSettings(WpfComboBox? combo, EngineSettings settings)
    {
        if (combo?.SelectedItem is not string display) return;
        var entry = All.FirstOrDefault(e => e.Display == display);
        if (entry == null) return;
        settings.EditorFontFamily = entry.Family;
        settings.EditorFontSize = entry.Size;
    }

    public static void SelectForSettings(WpfComboBox? combo, EngineSettings settings)
    {
        if (combo == null) return;
        var family = settings.EditorFontFamily?.Trim() ?? "Segoe UI";
        var size = settings.EditorFontSize;
        var exact = All.FirstOrDefault(e => string.Equals(e.Family, family, System.StringComparison.OrdinalIgnoreCase) && e.Size == size);
        if (exact != null)
        {
            combo.SelectedItem = exact.Display;
            return;
        }
        var byFamily = All.FirstOrDefault(e => string.Equals(e.Family, family, System.StringComparison.OrdinalIgnoreCase));
        combo.SelectedItem = (byFamily ?? All[0]).Display;
    }
}
