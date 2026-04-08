using FUEngine.Core;

namespace FUEngine;

/// <summary>Resuelve texto localizado y perfiles globales para UI Text/Button.</summary>
public static class UiTextResolve
{
    public readonly record struct Resolved(
        string DisplayText,
        UITextStyle Style,
        UITextLayoutSettings Layout,
        UITypewriterSettings? Typewriter);

    public static Resolved Resolve(UIElement el, string projectRoot, LocalizationRuntime? loc)
    {
        var layout = el.TextLayout?.Clone() ?? new UITextLayoutSettings();
        var baseStyle = el.TextStyle?.Clone() ?? new UITextStyle();
        var style = UiTextProfileMerge.MergeTextStyle(baseStyle, el.TextStyleProfilePath, projectRoot);
        var tw = UiTextProfileMerge.MergeTypewriter(el.Typewriter, el.TypewriterProfilePath, projectRoot);

        var display = GetDisplayText(el, loc);
        return new Resolved(display, style, layout, tw);
    }

    public static string GetDisplayText(UIElement el, LocalizationRuntime? loc)
    {
        if (!string.IsNullOrWhiteSpace(el.LocalizationKey))
        {
            if (loc != null)
                return loc.Resolve(el.LocalizationKey.Trim(), el.Text ?? "");
            return el.Text ?? "";
        }
        return el.Text ?? "";
    }

    /// <summary>Cadena estable para reiniciar el typewriter si cambia idioma, clave o texto fuente.</summary>
    public static string TypewriterSnapshotKey(UIElement el, LocalizationRuntime? loc) =>
        $"{el.LocalizationKey}\u001f{el.Text}\u001f{loc?.CurrentLocale ?? ""}\u001f{el.TextStyleProfilePath}\u001f{el.TypewriterProfilePath}";
}
