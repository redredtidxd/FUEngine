namespace FUEngine.Core;

/// <summary>Tipografía y estética de texto para elementos UI Text/Button.</summary>
public sealed class UITextStyle
{
    /// <summary>Nombre de familia del sistema (p. ej. Segoe UI) o ruta relativa al proyecto a .ttf/.otf.</summary>
    public string FontFamily { get; set; } = "Segoe UI";

    public double FontSize { get; set; } = 14;

    public UITextFontUnit FontSizeUnit { get; set; } = UITextFontUnit.Pixels;

    /// <summary>Color en #RRGGBB o #AARRGGBB.</summary>
    public string Color { get; set; } = "#FFFFFFFF";

    /// <summary>0..1 multiplicador adicional sobre el canal alfa del color.</summary>
    public double Opacity { get; set; } = 1;

    public UITextAlignmentKind Alignment { get; set; } = UITextAlignmentKind.Left;

    public bool EnableKerning { get; set; } = true;

    public bool RichTextEnabled { get; set; }

    public double OutlineThickness { get; set; }

    public string OutlineColor { get; set; } = "#FF000000";

    public double ShadowOffsetX { get; set; }

    public double ShadowOffsetY { get; set; }

    public string ShadowColor { get; set; } = "#80000000";

    public double ShadowBlur { get; set; }

    /// <summary>Multiplicador de interlineado (1 = por defecto del motor).</summary>
    public double LineSpacing { get; set; } = 1;

    /// <summary>Espaciado extra entre letras en píxeles lógicos (puede ser negativo).</summary>
    public double LetterSpacing { get; set; }

    /// <summary>Temblor, onda y arcoíris por glifo (runtime).</summary>
    public UITextGlyphEffects? GlyphEffects { get; set; }

    public UITextStyle Clone() => new()
    {
        FontFamily = FontFamily,
        FontSize = FontSize,
        FontSizeUnit = FontSizeUnit,
        Color = Color,
        Opacity = Opacity,
        Alignment = Alignment,
        EnableKerning = EnableKerning,
        RichTextEnabled = RichTextEnabled,
        OutlineThickness = OutlineThickness,
        OutlineColor = OutlineColor,
        ShadowOffsetX = ShadowOffsetX,
        ShadowOffsetY = ShadowOffsetY,
        ShadowColor = ShadowColor,
        ShadowBlur = ShadowBlur,
        LineSpacing = LineSpacing,
        LetterSpacing = LetterSpacing,
        GlyphEffects = GlyphEffects?.Clone()
    };
}
