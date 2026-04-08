using System.Collections.Generic;

namespace FUEngine.Core;

/// <summary>
/// Un elemento de UI (Button, Text, Image, Panel). IDs únicos por Canvas.
/// Si SeedId está definido, es una instancia de prefab/Seed; cada Canvas tiene su propia instancia.
/// </summary>
public class UIElement
{
    public string Id { get; set; } = "";
    public UIElementKind Kind { get; set; }
    public UIRect Rect { get; set; }
    public UIAnchors Anchors { get; set; }
    /// <summary>Texto para Button o Text.</summary>
    public string Text { get; set; } = "";

    /// <summary>Si no vacío, el texto mostrado se resuelve desde <c>Data/localization.json</c> (clave).</summary>
    public string LocalizationKey { get; set; } = "";

    /// <summary>Ruta relativa al proyecto a un perfil <c>.fuetextstyle</c> (JSON de tipografía; se fusiona sobre el estilo del elemento).</summary>
    public string TextStyleProfilePath { get; set; } = "";

    /// <summary>Ruta relativa a <c>.fuetypewriter</c> (JSON; se fusiona sobre el typewriter del elemento).</summary>
    public string TypewriterProfilePath { get; set; } = "";
    /// <summary>Ruta de imagen para Image (relativa al proyecto).</summary>
    public string ImagePath { get; set; } = "";
    /// <summary>Si no vacío, este elemento es instancia de un Seed UI; cada Canvas tiene su propia instancia.</summary>
    public string SeedId { get; set; } = "";
    /// <summary>Hijos (para Panel o Canvas).</summary>
    public List<UIElement> Children { get; set; } = new();
    /// <summary>Overrides de propiedades respecto al prefab (solo cuando SeedId está definido). Clave = nombre propiedad, Valor = string serializable.</summary>
    public Dictionary<string, string> PropertyOverrides { get; set; } = new();
    /// <summary>Si true, el elemento no recibe input (clicks pasan a través).</summary>
    public bool BlocksInput { get; set; } = true;

    /// <summary>Tipografía y estética; relevante para <see cref="UIElementKind.Text"/> y <see cref="UIElementKind.Button"/>.</summary>
    public UITextStyle? TextStyle { get; set; }

    /// <summary>Ajuste de líneas y desbordamiento.</summary>
    public UITextLayoutSettings? TextLayout { get; set; }

    /// <summary>Máquina de escribir en runtime.</summary>
    public UITypewriterSettings? Typewriter { get; set; }

    /// <summary>Anclaje visual 3×3 y pivote del texto (Text/Button).</summary>
    public UITextAnchorSettings? TextAnchor { get; set; }

    public static bool SupportsRichTextComponents(UIElementKind kind) =>
        kind is UIElementKind.Text or UIElementKind.Button;
}
