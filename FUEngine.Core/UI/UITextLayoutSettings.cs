namespace FUEngine.Core;

/// <summary>Ajuste de líneas, guiones por ancho y desbordamiento.</summary>
public sealed class UITextLayoutSettings
{
    public bool WordWrap { get; set; } = true;

    /// <summary>Si true, parte palabras largas con '-' al límite del ancho (heurística por ancho, no silabario).</summary>
    public bool HyphenationEnabled { get; set; }

    /// <summary>Mínimo de caracteres visibles antes del guion al partir una palabra (evita "a-").</summary>
    public int HyphenMinPrefixChars { get; set; } = 2;

    public UITextOverflowMode OverflowMode { get; set; } = UITextOverflowMode.Ellipsis;

    public UITextLayoutSettings Clone() => new()
    {
        WordWrap = WordWrap,
        HyphenationEnabled = HyphenationEnabled,
        HyphenMinPrefixChars = HyphenMinPrefixChars,
        OverflowMode = OverflowMode
    };
}
