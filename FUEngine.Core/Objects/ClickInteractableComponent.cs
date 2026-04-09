namespace FUEngine.Core;

/// <summary>Forma del área clicable en espacio local (tras rotación del objeto).</summary>
public enum ClickInteractableShapeKind
{
    Box = 0,
    Circle = 1,
    /// <summary>Hit-test por alpha del sprite de la definición (requiere SpritePath).</summary>
    PixelPerfect = 2
}

/// <summary>Filtro de entrada: ratón (PC), toque (móvil) o ambos. En WPF Play solo hay ratón; el motor ignora hover si el filtro es solo toque.</summary>
public enum ClickInteractableInputKind
{
    Mouse = 0,
    Touch = 1,
    Both = 2
}

/// <summary>
/// Área interactiva en <strong>espacio mundo</strong> (casillas): hit-test en Play frente a clics/taps en el viewport.
/// Los datos persisten en <see cref="ObjectInstance"/>; en Play se copian aquí.
/// </summary>
public sealed class ClickInteractableComponent : Component
{
    public bool InteractEnabled { get; set; } = true;

    public ClickInteractableShapeKind Shape { get; set; } = ClickInteractableShapeKind.Box;

    /// <summary>Ancho del rectángulo en casillas (eje local antes de rotar).</summary>
    public float BoxWidthTiles { get; set; } = 1f;

    /// <summary>Alto del rectángulo en casillas.</summary>
    public float BoxHeightTiles { get; set; } = 1f;

    public float CircleRadiusTiles { get; set; } = 0.5f;

    /// <summary>Desplazamiento del centro del área respecto a <see cref="Transform"/> del objeto (casillas).</summary>
    public float OffsetXTiles { get; set; }

    public float OffsetYTiles { get; set; }

    /// <summary>Si true, en PC el editor/visor puede cambiar cursor al pasar el ratón (sin efecto en Android sin puntero).</summary>
    public bool HoverEffect { get; set; }

    public ClickInteractableInputKind InputFilter { get; set; } = ClickInteractableInputKind.Both;

    /// <summary>Máxima distancia en casillas desde el protagonista hasta el punto clicado; 0 = sin límite.</summary>
    public float MaxDistanceFromPlayerTiles { get; set; }

    /// <summary>Prioridad exclusiva del rayo de clic; mayor = encima si se solapan (desempate por orden de render).</summary>
    public int ClickZPriority { get; set; }

    /// <summary>Si true, un muro de tile (colisión) entre el protagonista y el punto bloquea el clic.</summary>
    public bool RequireLineOfSight { get; set; }

    /// <summary>Multiplicador temporal de escala al pulsar (1 = desactivado; p. ej. 0.94).</summary>
    public float OnPressScaleMul { get; set; } = 1f;

    /// <summary>Tinte al hover (#RRGGBB); el visor WPF puede usarlo en ampliaciones futuras.</summary>
    public string? HoverTintHex { get; set; }

    /// <summary>Ids de script en <c>scripts.json</c> para one-shot (opcional; la vía habitual son hooks Lua en el mismo objeto).</summary>
    public string? ScriptIdOnClick { get; set; }

    public string? ScriptIdOnPointerEnter { get; set; }

    public string? ScriptIdOnPointerExit { get; set; }
}
