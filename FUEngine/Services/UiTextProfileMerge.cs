using System.IO;
using System.Text.Json;
using FUEngine.Core;

namespace FUEngine;

/// <summary>Fusiona JSON de perfiles <c>.fuetextstyle</c> y <c>.fuetypewriter</c> sobre los datos del elemento.</summary>
public static class UiTextProfileMerge
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static UITextStyle MergeTextStyle(UITextStyle elementBase, string? profileRelativePath, string projectRoot)
    {
        var r = elementBase.Clone();
        if (string.IsNullOrWhiteSpace(profileRelativePath) || string.IsNullOrWhiteSpace(projectRoot))
            return r;
        var path = Path.IsPathRooted(profileRelativePath)
            ? profileRelativePath
            : Path.GetFullPath(Path.Combine(projectRoot, profileRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!File.Exists(path)) return r;
        try
        {
            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            ApplyTextStylePatch(r, doc.RootElement);
        }
        catch
        {
            /* ignorar perfil roto */
        }
        return r;
    }

    public static UITypewriterSettings MergeTypewriter(UITypewriterSettings? elementBase, string? profileRelativePath, string projectRoot)
    {
        var r = (elementBase ?? new UITypewriterSettings()).Clone();
        if (string.IsNullOrWhiteSpace(profileRelativePath) || string.IsNullOrWhiteSpace(projectRoot))
            return r;
        var path = Path.IsPathRooted(profileRelativePath)
            ? profileRelativePath
            : Path.GetFullPath(Path.Combine(projectRoot, profileRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!File.Exists(path)) return r;
        try
        {
            var json = File.ReadAllText(path);
            var patch = JsonSerializer.Deserialize<UITypewriterSettings>(json, JsonOpts);
            if (patch == null) return r;
            CopyTypewriterNonDefaults(patch, r);
        }
        catch
        {
            /* ignorar */
        }
        return r;
    }

    private static void CopyTypewriterNonDefaults(UITypewriterSettings patch, UITypewriterSettings target)
    {
        var def = new UITypewriterSettings();
        if (patch.Enabled != def.Enabled) target.Enabled = patch.Enabled;
        if (Math.Abs(patch.CharsPerSecond - def.CharsPerSecond) > 1e-6) target.CharsPerSecond = patch.CharsPerSecond;
        if (patch.FadeInPerChar != def.FadeInPerChar) target.FadeInPerChar = patch.FadeInPerChar;
        if (Math.Abs(patch.FadeInDurationSeconds - def.FadeInDurationSeconds) > 1e-6) target.FadeInDurationSeconds = patch.FadeInDurationSeconds;
        if (patch.PunctuationPausesEnabled != def.PunctuationPausesEnabled) target.PunctuationPausesEnabled = patch.PunctuationPausesEnabled;
        if (Math.Abs(patch.PauseAfterCommaSeconds - def.PauseAfterCommaSeconds) > 1e-6) target.PauseAfterCommaSeconds = patch.PauseAfterCommaSeconds;
        if (Math.Abs(patch.PauseAfterPeriodSeconds - def.PauseAfterPeriodSeconds) > 1e-6) target.PauseAfterPeriodSeconds = patch.PauseAfterPeriodSeconds;
        if (Math.Abs(patch.PauseAfterQuestionSeconds - def.PauseAfterQuestionSeconds) > 1e-6) target.PauseAfterQuestionSeconds = patch.PauseAfterQuestionSeconds;
        if (Math.Abs(patch.PauseAfterExclamationSeconds - def.PauseAfterExclamationSeconds) > 1e-6) target.PauseAfterExclamationSeconds = patch.PauseAfterExclamationSeconds;
        if (!string.IsNullOrEmpty(patch.SoundPath)) target.SoundPath = patch.SoundPath;
        if (patch.SoundTrigger != def.SoundTrigger) target.SoundTrigger = patch.SoundTrigger;
        if (Math.Abs(patch.SoundVolume - def.SoundVolume) > 1e-6) target.SoundVolume = patch.SoundVolume;
    }

    private static void ApplyTextStylePatch(UITextStyle target, JsonElement root)
    {
        foreach (var p in root.EnumerateObject())
        {
            switch (p.Name)
            {
                case "fontFamily":
                    if (p.Value.ValueKind == JsonValueKind.String)
                        target.FontFamily = p.Value.GetString() ?? target.FontFamily;
                    break;
                case "fontSize":
                    if (p.Value.TryGetDouble(out var fs)) target.FontSize = fs;
                    break;
                case "fontSizeUnit":
                    if (p.Value.ValueKind == JsonValueKind.String &&
                        Enum.TryParse<UITextFontUnit>(p.Value.GetString(), true, out var u))
                        target.FontSizeUnit = u;
                    break;
                case "color":
                    if (p.Value.ValueKind == JsonValueKind.String)
                        target.Color = p.Value.GetString() ?? target.Color;
                    break;
                case "opacity":
                    if (p.Value.TryGetDouble(out var op)) target.Opacity = op;
                    break;
                case "alignment":
                    if (p.Value.ValueKind == JsonValueKind.String &&
                        Enum.TryParse<UITextAlignmentKind>(p.Value.GetString(), true, out var al))
                        target.Alignment = al;
                    break;
                case "enableKerning":
                    if (p.Value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                        target.EnableKerning = p.Value.GetBoolean();
                    break;
                case "richTextEnabled":
                    if (p.Value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                        target.RichTextEnabled = p.Value.GetBoolean();
                    break;
                case "outlineThickness":
                    if (p.Value.TryGetDouble(out var ot)) target.OutlineThickness = ot;
                    break;
                case "outlineColor":
                    if (p.Value.ValueKind == JsonValueKind.String)
                        target.OutlineColor = p.Value.GetString() ?? target.OutlineColor;
                    break;
                case "shadowOffsetX":
                    if (p.Value.TryGetDouble(out var sx)) target.ShadowOffsetX = sx;
                    break;
                case "shadowOffsetY":
                    if (p.Value.TryGetDouble(out var sy)) target.ShadowOffsetY = sy;
                    break;
                case "shadowColor":
                    if (p.Value.ValueKind == JsonValueKind.String)
                        target.ShadowColor = p.Value.GetString() ?? target.ShadowColor;
                    break;
                case "shadowBlur":
                    if (p.Value.TryGetDouble(out var sb)) target.ShadowBlur = sb;
                    break;
                case "lineSpacing":
                    if (p.Value.TryGetDouble(out var ls)) target.LineSpacing = ls;
                    break;
                case "letterSpacing":
                    if (p.Value.TryGetDouble(out var lsp)) target.LetterSpacing = lsp;
                    break;
                case "glyphEffects":
                    if (p.Value.ValueKind == JsonValueKind.Object)
                    {
                        target.GlyphEffects ??= new UITextGlyphEffects();
                        ApplyGlyphEffectsPatch(target.GlyphEffects, p.Value);
                    }
                    break;
            }
        }
    }

    private static void ApplyGlyphEffectsPatch(UITextGlyphEffects g, JsonElement root)
    {
        foreach (var p in root.EnumerateObject())
        {
            switch (p.Name)
            {
                case "shakeEnabled":
                    if (p.Value.ValueKind is JsonValueKind.True or JsonValueKind.False) g.ShakeEnabled = p.Value.GetBoolean();
                    break;
                case "shakeIntensityPixels":
                    if (p.Value.TryGetDouble(out var sh)) g.ShakeIntensityPixels = sh;
                    break;
                case "waveEnabled":
                    if (p.Value.ValueKind is JsonValueKind.True or JsonValueKind.False) g.WaveEnabled = p.Value.GetBoolean();
                    break;
                case "waveAmplitudePixels":
                    if (p.Value.TryGetDouble(out var wa)) g.WaveAmplitudePixels = wa;
                    break;
                case "waveFrequency":
                    if (p.Value.TryGetDouble(out var wf)) g.WaveFrequency = wf;
                    break;
                case "rainbowEnabled":
                    if (p.Value.ValueKind is JsonValueKind.True or JsonValueKind.False) g.RainbowEnabled = p.Value.GetBoolean();
                    break;
                case "rainbowCyclesPerSecond":
                    if (p.Value.TryGetDouble(out var rc)) g.RainbowCyclesPerSecond = rc;
                    break;
            }
        }
    }
}
