namespace FUEngine.Core;

/// <summary>
/// Caja AABB en el mismo espacio que <see cref="Transform"/> (coordenadas de casilla).
/// Centro del AABB = posición del transform + offset; tamaño = mitad en casillas de ancho/alto × escala del transform.
/// </summary>
public sealed class ColliderComponent : Component
{
    /// <summary>Mitad del ancho en unidades de casilla (sin multiplicar por escala), típicamente definición.Width/2.</summary>
    public float TileHalfWidth { get; set; } = 0.5f;

    /// <summary>Mitad del alto en unidades de casilla (sin multiplicar por escala).</summary>
    public float TileHalfHeight { get; set; } = 0.5f;

    /// <summary>Ancho base en casillas (2 × mitad).</summary>
    public float Width => TileHalfWidth * 2f;

    /// <summary>Alto base en casillas (2 × mitad).</summary>
    public float Height => TileHalfHeight * 2f;

    public float OffsetX { get; set; }
    public float OffsetY { get; set; }

    /// <summary>Si true, genera onTriggerEnter/onTriggerExit y no participa en bloqueo sólido.</summary>
    public bool IsTrigger { get; set; }

    /// <summary>Si true, los cuerpos dinámicos no pueden solaparse (resolución AABB).</summary>
    public bool BlocksMovement { get; set; } = true;

    /// <summary>Si true, este cuerpo no se desplaza por la resolución (paredes, suelo).</summary>
    public bool IsStatic { get; set; } = true;

    /// <summary>Masa para separación dinámico–dinámico (mayor = menos desplazamiento). Mínimo efectivo 0.01.</summary>
    public float Mass { get; set; } = 1f;
}
