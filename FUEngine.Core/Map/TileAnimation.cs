using System.Collections.Generic;

namespace FUEngine.Core;

/// <summary>
/// Animación de tile: secuencia de frame IDs del tileset (ej: water_1, water_2, water_3).
/// Velocidad en frames por segundo.
/// </summary>
public class TileAnimation
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    /// <summary>IDs de tile en el tileset, en orden (frame 0, 1, 2...).</summary>
    public List<int> FrameTileIds { get; set; } = new();
    /// <summary>Frames por segundo (0.2 = lento, 4 = rápido).</summary>
    public float Speed { get; set; } = 1f;
    public bool Loop { get; set; } = true;
}
