using System;
using System.Collections.Generic;
using System.Linq;

namespace FUEngine;

/// <summary>Perfiles de atajos (preset) que rellenan <see cref="EngineSettings.ShortcutBindings"/>.</summary>
public static class EditorShortcutPresets
{
    public const string DefaultId = "Default";
    public const string UnityId = "Unity";
    public const string PhotoshopId = "Photoshop";
    public const string CustomId = "Custom";

    public sealed record Choice(string Id, string Display);

    public static IReadOnlyList<Choice> ComboChoices { get; } = new[]
    {
        new Choice(DefaultId, "FUEngine (predeterminado)"),
        new Choice(UnityId, "Estilo Unity (Q W E R, T, Y — herramientas)"),
        new Choice(PhotoshopId, "Estilo Photoshop (V selección, H mano, B pincel…)"),
    };

    private static readonly Dictionary<string, string> UnityOverrides = new(StringComparer.Ordinal)
    {
        [EditorShortcutBindings.Tool1] = "Q",
        [EditorShortcutBindings.Tool2] = "W",
        [EditorShortcutBindings.Tool3] = "E",
        [EditorShortcutBindings.Tool4] = "R",
        [EditorShortcutBindings.Tool5] = "T",
    };

    /// <summary>Atajos inspirados en Photoshop; el pan con mano pasa a H (Espacio libre para texto en otros contextos).</summary>
    private static readonly Dictionary<string, string> PhotoshopOverrides = new(StringComparer.Ordinal)
    {
        [EditorShortcutBindings.HandPan] = "H",
        [EditorShortcutBindings.Tool1] = "B",
        [EditorShortcutBindings.Tool2] = "V",
        [EditorShortcutBindings.Tool3] = "J",
        [EditorShortcutBindings.Tool4] = "M",
        [EditorShortcutBindings.Tool5] = "I",
    };

    public static IReadOnlyDictionary<string, string> GetOverrides(string presetId)
    {
        if (string.Equals(presetId, UnityId, StringComparison.OrdinalIgnoreCase)) return UnityOverrides;
        if (string.Equals(presetId, PhotoshopId, StringComparison.OrdinalIgnoreCase)) return PhotoshopOverrides;
        return Empty;
    }

    private static readonly Dictionary<string, string> Empty = new(StringComparer.Ordinal);

    public static Dictionary<string, string> BuildResolvedMap(string presetId)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var d in EditorShortcutBindings.Definitions.Where(x => x.Rebindable && !x.Id.StartsWith('_')))
            map[d.Id] = d.DefaultDisplay;
        foreach (var kv in GetOverrides(presetId))
            map[kv.Key] = kv.Value;
        return map;
    }

    public static string EffectiveBinding(EngineSettings settings, string actionId)
    {
        if (settings.ShortcutBindings != null && settings.ShortcutBindings.TryGetValue(actionId, out var v) && !string.IsNullOrWhiteSpace(v))
            return v.Trim();
        var def = EditorShortcutBindings.Definitions.FirstOrDefault(d => d.Id == actionId);
        return def?.DefaultDisplay ?? "";
    }

    public static bool MatchesPreset(EngineSettings settings, string presetId)
    {
        if (string.Equals(presetId, CustomId, StringComparison.OrdinalIgnoreCase)) return true;
        var resolved = BuildResolvedMap(presetId);
        foreach (var id in resolved.Keys)
        {
            if (!string.Equals(EffectiveBinding(settings, id), resolved[id], StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }

    /// <summary>Aplica el preset al diccionario de atajos y actualiza <see cref="EngineSettings.ShortcutPreset"/>.</summary>
    public static void Apply(EngineSettings settings, string presetId)
    {
        settings.ShortcutBindings ??= new Dictionary<string, string>(StringComparer.Ordinal);
        if (string.Equals(presetId, DefaultId, StringComparison.OrdinalIgnoreCase))
        {
            foreach (var id in RebindableIds())
                settings.ShortcutBindings.Remove(id);
            settings.ShortcutPreset = DefaultId;
            return;
        }

        if (string.Equals(presetId, CustomId, StringComparison.OrdinalIgnoreCase))
        {
            settings.ShortcutPreset = CustomId;
            return;
        }

        if (!string.Equals(presetId, UnityId, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(presetId, PhotoshopId, StringComparison.OrdinalIgnoreCase))
            return;

        var resolved = BuildResolvedMap(presetId);
        foreach (var kv in resolved)
            settings.ShortcutBindings[kv.Key] = kv.Value;
        settings.ShortcutPreset = string.Equals(presetId, UnityId, StringComparison.OrdinalIgnoreCase) ? UnityId : PhotoshopId;
    }

    private static IEnumerable<string> RebindableIds() =>
        EditorShortcutBindings.Definitions.Where(d => d.Rebindable && !d.Id.StartsWith('_')).Select(d => d.Id);

    /// <summary>Ajusta <see cref="EngineSettings.ShortcutPreset"/> si el JSON no coincide con los bindings reales.</summary>
    public static void NormalizeAfterLoad(EngineSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ShortcutPreset))
        {
            settings.ShortcutPreset = DefaultId;
            if (!MatchesPreset(settings, DefaultId))
                settings.ShortcutPreset = CustomId;
            return;
        }

        var p = settings.ShortcutPreset.Trim();
        if (string.Equals(p, CustomId, StringComparison.OrdinalIgnoreCase))
            return;

        if (!ComboChoices.Any(c => string.Equals(c.Id, p, StringComparison.OrdinalIgnoreCase)))
        {
            settings.ShortcutPreset = CustomId;
            return;
        }

        if (!MatchesPreset(settings, p))
            settings.ShortcutPreset = CustomId;
    }
}
