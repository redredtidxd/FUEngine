using System.Collections.Generic;
using FUEngine.Core;

namespace FUEngine.Editor;

/// <summary>
/// Copia del estado del mapa (tiles + objetos) para revertir después.
/// </summary>
public class MapSnapshot
{
    public List<(int x, int y, TileData data)> Tiles { get; } = new();
    public List<ObjectInstance> Objects { get; } = new();
}
