namespace FUEngine.Core;

/// <summary>
/// Definición de un tile dentro de un Tileset (id, colisión, material, bloqueo de luz, datos extra).
/// El rectángulo de textura se deduce de Id y tamaño del tileset (grid).
/// </summary>
public class Tile
{
    /// <summary>Índice del tile en el tileset (0, 1, 2... en orden de grid).</summary>
    public int Id { get; set; }
    public bool Collision { get; set; }
    /// <summary>Material opcional (ej: "sand", "stone", "water") para física o efectos.</summary>
    public string? Material { get; set; }
    /// <summary>Si true, el tile bloquea luz (pared opaca).</summary>
    public bool LightBlock { get; set; }
    /// <summary>Datos personalizados para scripts (JSON o clave-valor).</summary>
    public string? CustomData { get; set; }
    /// <summary>Velocidad de animación (frames por segundo) si el tile es animado.</summary>
    public float AnimationSpeed { get; set; }
    /// <summary>Id de animación en el tileset (lista de frame IDs). 0 = estático.</summary>
    public string? AnimationId { get; set; }
    /// <summary>Fricción del tile (0 = resbaladizo, 1 = normal). Para física.</summary>
    public float Friction { get; set; } = 0.5f;
    public List<string> Tags { get; set; } = new();
}
