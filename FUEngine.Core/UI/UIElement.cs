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
}
