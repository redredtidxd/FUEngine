namespace FUEngine.Core;

/// <summary>Posición de anclaje en 3×3 (responsive al redimensionar el canvas padre).</summary>
public enum UITextAnchorPreset
{
    None = 0,
    TopLeft,
    TopCenter,
    TopRight,
    MiddleLeft,
    Center,
    MiddleRight,
    BottomLeft,
    BottomCenter,
    BottomRight
}

/// <summary>Origen local del bloque de texto respecto al rectángulo del elemento (0,0 = esquina superior izquierda del área útil).</summary>
public enum UITextPivotPreset
{
    TopLeft = 0,
    TopCenter,
    TopRight,
    MiddleLeft,
    Center,
    MiddleRight,
    BottomLeft,
    BottomCenter,
    BottomRight
}

/// <summary>Anclaje y pivote opcionales para Text/Button (complementa <see cref="UIAnchors"/>).</summary>
public sealed class UITextAnchorSettings
{
    /// <summary>Si distinto de <see cref="UITextAnchorPreset.None"/>, el inspector puede sincronizar <see cref="UIElement.Anchors"/> con la plantilla.</summary>
    public UITextAnchorPreset AnchorPreset { get; set; }

    public UITextPivotPreset PivotPreset { get; set; } = UITextPivotPreset.TopLeft;

    public UITextAnchorSettings Clone() => new()
    {
        AnchorPreset = AnchorPreset,
        PivotPreset = PivotPreset
    };

    /// <summary>Convierte el preset a anclas normalizadas (min=max en el punto de anclaje).</summary>
    public static UIAnchors ToAnchors(UITextAnchorPreset preset) => preset switch
    {
        UITextAnchorPreset.TopLeft => new UIAnchors { MinX = 0, MinY = 0, MaxX = 0, MaxY = 0 },
        UITextAnchorPreset.TopCenter => new UIAnchors { MinX = 0.5, MinY = 0, MaxX = 0.5, MaxY = 0 },
        UITextAnchorPreset.TopRight => new UIAnchors { MinX = 1, MinY = 0, MaxX = 1, MaxY = 0 },
        UITextAnchorPreset.MiddleLeft => new UIAnchors { MinX = 0, MinY = 0.5, MaxX = 0, MaxY = 0.5 },
        UITextAnchorPreset.Center => new UIAnchors { MinX = 0.5, MinY = 0.5, MaxX = 0.5, MaxY = 0.5 },
        UITextAnchorPreset.MiddleRight => new UIAnchors { MinX = 1, MinY = 0.5, MaxX = 1, MaxY = 0.5 },
        UITextAnchorPreset.BottomLeft => new UIAnchors { MinX = 0, MinY = 1, MaxX = 0, MaxY = 1 },
        UITextAnchorPreset.BottomCenter => new UIAnchors { MinX = 0.5, MinY = 1, MaxX = 0.5, MaxY = 1 },
        UITextAnchorPreset.BottomRight => new UIAnchors { MinX = 1, MinY = 1, MaxX = 1, MaxY = 1 },
        _ => new UIAnchors { MinX = 0, MinY = 0, MaxX = 0, MaxY = 0 }
    };

    /// <summary>Pivote en 0..1 del área interna (ancho/alto del rectángulo de layout del texto).</summary>
    public static (double X, double Y) ToPivotFractions(UITextPivotPreset p) => p switch
    {
        UITextPivotPreset.TopLeft => (0, 0),
        UITextPivotPreset.TopCenter => (0.5, 0),
        UITextPivotPreset.TopRight => (1, 0),
        UITextPivotPreset.MiddleLeft => (0, 0.5),
        UITextPivotPreset.Center => (0.5, 0.5),
        UITextPivotPreset.MiddleRight => (1, 0.5),
        UITextPivotPreset.BottomLeft => (0, 1),
        UITextPivotPreset.BottomCenter => (0.5, 1),
        UITextPivotPreset.BottomRight => (1, 1),
        _ => (0, 0)
    };
}
