using System.Collections.Generic;
using System.Linq;

namespace FUEngine.Core;

/// <summary>
/// Nodo raíz UI de la escena. Contiene todos los Canvas (MainMenu, Pause, etc.).
/// Estructura: Scene → Map + UI (UIRoot) → Canvas → elementos.
/// </summary>
public class UIRoot
{
    public List<UICanvas> Canvases { get; set; } = new();

    public UICanvas? GetCanvas(string id)
    {
        foreach (var c in Canvases)
            if (string.Equals(c.Id, id, System.StringComparison.OrdinalIgnoreCase))
                return c;
        return null;
    }

    public void AddCanvas(UICanvas canvas)
    {
        if (GetCanvas(canvas.Id) != null) return;
        Canvases.Add(canvas);
    }

    public void RemoveCanvas(UICanvas canvas) => Canvases.Remove(canvas);

    /// <summary>Copia profunda del UIRoot (para duplicar escena).</summary>
    public static UIRoot Clone(UIRoot root)
    {
        var r = new UIRoot();
        foreach (var c in root.Canvases)
            r.Canvases.Add(CloneCanvas(c));
        return r;
    }

    private static UICanvas CloneCanvas(UICanvas c)
    {
        var copy = new UICanvas
        {
            Id = c.Id,
            Name = c.Name,
            ResolutionWidth = c.ResolutionWidth,
            ResolutionHeight = c.ResolutionHeight,
            ZIndex = c.ZIndex
        };
        copy.Children.AddRange(c.Children.Select(CloneElement));
        return copy;
    }

    private static UIElement CloneElement(UIElement e)
    {
        var copy = new UIElement
        {
            Id = e.Id,
            Kind = e.Kind,
            Rect = e.Rect,
            Anchors = e.Anchors,
            Text = e.Text,
            LocalizationKey = e.LocalizationKey,
            TextStyleProfilePath = e.TextStyleProfilePath,
            TypewriterProfilePath = e.TypewriterProfilePath,
            ImagePath = e.ImagePath,
            SeedId = e.SeedId,
            BlocksInput = e.BlocksInput,
            TextStyle = e.TextStyle?.Clone(),
            TextLayout = e.TextLayout?.Clone(),
            Typewriter = e.Typewriter?.Clone(),
            TextAnchor = e.TextAnchor?.Clone()
        };
        foreach (var kv in e.PropertyOverrides)
            copy.PropertyOverrides[kv.Key] = kv.Value;
        copy.Children.AddRange(e.Children.Select(CloneElement));
        return copy;
    }

    /// <summary>Comprueba que el Id sea único en el árbol del canvas (excluyendo exclude).</summary>
    public static bool IsIdUniqueInCanvas(UICanvas canvas, string elementId, UIElement? exclude = null)
    {
        var existing = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        CollectIdsInto(canvas.Children, exclude, existing);
        return !existing.Contains(elementId);
    }

    private static void CollectIdsInto(List<UIElement> elements, UIElement? exclude, HashSet<string> into)
    {
        foreach (var e in elements)
        {
            if (e == exclude) continue;
            if (!string.IsNullOrEmpty(e.Id)) into.Add(e.Id);
            CollectIdsInto(e.Children, exclude, into);
        }
    }
}
