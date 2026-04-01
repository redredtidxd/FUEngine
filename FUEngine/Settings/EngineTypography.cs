using System.Windows;

namespace FUEngine;

/// <summary>Aplica la fuente del motor a una ventana o control raíz (herencia a controles hijos).</summary>
public static class EngineTypography
{
    public static void ApplyToRoot(System.Windows.Controls.Control? root)
    {
        if (root == null) return;
        var s = EngineSettings.Load();
        ApplyToRoot(root, s.EditorFontFamily, s.EditorFontSize);
    }

    public static void ApplyToRoot(System.Windows.Controls.Control root, string fontFamily, int fontSize)
    {
        try
        {
            root.FontFamily = new System.Windows.Media.FontFamily(fontFamily);
            if (fontSize >= 8 && fontSize <= 28)
                root.FontSize = fontSize;
        }
        catch
        {
            try
            {
                root.FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
                root.FontSize = 12;
            }
            catch { /* ignore */ }
        }
    }
}
