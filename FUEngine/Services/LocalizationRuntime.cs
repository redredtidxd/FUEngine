using System.Globalization;
using System.IO;
using FUEngine.Runtime;

namespace FUEngine;

/// <summary>Carga <c>Data/localization.json</c> y resuelve claves por idioma activo.</summary>
public sealed class LocalizationRuntime : IUiLocaleProvider
{
    private readonly Dictionary<string, Dictionary<string, string>> _entries = new(StringComparer.OrdinalIgnoreCase);
    private string _defaultLocale = "en";
    private string _fallbackLocale = "en";
    private string _currentLocale = "en";

    public string CurrentLocale => _currentLocale;

    public void LoadFromProject(string projectRoot)
    {
        _entries.Clear();
        if (string.IsNullOrWhiteSpace(projectRoot)) return;
        var path = Path.Combine(projectRoot, "Data", "localization.json");
        if (!LocalizationFileData.TryLoad(path, out var data, out _))
            return;
        _defaultLocale = NormLocale(data.DefaultLocale);
        _fallbackLocale = NormLocale(data.FallbackLocale);
        _currentLocale = _defaultLocale;
        foreach (var kv in data.Entries)
            _entries[kv.Key] = kv.Value;
    }

    public void SetLocale(string? cultureOrLanguage)
    {
        if (string.IsNullOrWhiteSpace(cultureOrLanguage)) return;
        try
        {
            var c = CultureInfo.GetCultureInfo(cultureOrLanguage.Trim());
            _currentLocale = NormLocale(c.TwoLetterISOLanguageName);
        }
        catch
        {
            _currentLocale = NormLocale(cultureOrLanguage);
        }
    }

    void IUiLocaleProvider.SetLocale(string? code) => SetLocale(code);

    string IUiLocaleProvider.GetLocale() => _currentLocale;

    /// <summary>Inicializa el idioma activo desde el sistema (una vez al iniciar Play).</summary>
    public void ApplySystemLocale()
    {
        try
        {
            _currentLocale = NormLocale(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
        }
        catch
        {
            _currentLocale = _defaultLocale;
        }
    }

    public string Resolve(string key, string fallbackLiteral)
    {
        if (string.IsNullOrWhiteSpace(key)) return fallbackLiteral;
        if (!_entries.TryGetValue(key.Trim(), out var map)) return fallbackLiteral;
        if (map.TryGetValue(_currentLocale, out var t) && !string.IsNullOrEmpty(t)) return t;
        if (map.TryGetValue(_fallbackLocale, out t) && !string.IsNullOrEmpty(t)) return t;
        if (map.TryGetValue(_defaultLocale, out t) && !string.IsNullOrEmpty(t)) return t;
        foreach (var v in map.Values)
        {
            if (!string.IsNullOrEmpty(v)) return v;
        }
        return fallbackLiteral;
    }

    private static string NormLocale(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "en";
        var t = s.Trim().ToLowerInvariant();
        return t.Length >= 2 ? t[..2] : t;
    }
}
