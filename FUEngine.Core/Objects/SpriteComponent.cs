namespace FUEngine.Core;

/// <summary>
/// Sprite 2D: textura del proyecto (PNG/JPG), recortes opcionales para animación y tamaño en casillas en el mundo.
/// </summary>
public sealed class SpriteComponent : Component
{
    /// <summary>Ruta relativa al directorio del proyecto.</summary>
    public string? TexturePath { get; set; }

    /// <summary>Ancho visual en casillas (centro = <see cref="GameObject.Transform"/>).</summary>
    public float DisplayWidthTiles { get; set; } = 1f;

    /// <summary>Alto visual en casillas.</summary>
    public float DisplayHeightTiles { get; set; } = 1f;

    /// <summary>Desplazamiento fino de dibujado respecto a otros con el mismo <see cref="GameObject.RenderOrder"/>.</summary>
    public int SortOffset { get; set; }

    /// <summary>Frames en orden; vacío = usar la imagen completa como un único frame.</summary>
    public List<SpriteFrameRegion> FrameRegions { get; set; } = new();

    public int CurrentFrameIndex { get; set; }

    /// <summary>Si &gt; 0 y hay varios frames, avanza <see cref="CurrentFrameIndex"/> en bucle cada tick de simulación.</summary>
    public float AnimationFramesPerSecond { get; set; }

    /// <summary>Acumulador interno para animación automática (no serializar).</summary>
    public float AnimationTimeAccum { get; set; }

    /// <summary>Clave lógica de la última auto-animación nativa aplicada (p. ej. <c>Idle</c> / <c>Walk</c>); evita reiniciar el clip cada tick.</summary>
    public string? NativeAutoAnimationKey { get; set; }
}
