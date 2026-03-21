using System.Collections.Generic;

namespace FUEngine.Core;

/// <summary>
/// Un canvas de UI (MainMenu, Pause, Inventory). Resolución base y orden de dibujado.
/// </summary>
public class UICanvas
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    /// <summary>Resolución base del canvas (ej. 1920x1080). Escalado según viewport.</summary>
    public int ResolutionWidth { get; set; } = 1920;
    public int ResolutionHeight { get; set; } = 1080;
    /// <summary>Orden de dibujado; mayor = encima. Usado también para focus cuando varios visibles.</summary>
    public int ZIndex { get; set; } = 0;
    /// <summary>Elementos raíz del canvas (no se usa Canvas como Kind en raíz; son Panel, etc.).</summary>
    public List<UIElement> Children { get; set; } = new();
}
