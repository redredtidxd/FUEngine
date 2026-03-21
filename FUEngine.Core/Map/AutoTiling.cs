namespace FUEngine.Core;

/// <summary>
/// Auto-tiling: dado un tile base y los vecinos (N,S,E,W o 3x3), devuelve el tile ID correcto
/// (center, edge, corner) para que los bordes se vean bien.
/// Máscara de 4 bits: North=8, South=4, East=2, West=1 (vecino presente = 1).
/// </summary>
public static class AutoTiling
{
    /// <summary>Máscara: bit set = hay tile del mismo tipo en esa dirección.</summary>
    public static int GetTileIdFromNeighbors(int baseTileId, bool north, bool south, bool east, bool west)
    {
        int mask = (north ? 8 : 0) | (south ? 4 : 0) | (east ? 2 : 0) | (west ? 1 : 0);
        return GetTileIdFromMask(baseTileId, mask);
    }

    /// <summary>Con máscara 0-15, devuelve índice de tile (0=center, 1-4=edges, 5-8=corners). Por defecto devuelve baseTileId + offset.</summary>
    public static int GetTileIdFromMask(int baseTileId, int neighborMask)
    {
        // Convención simple: 16 tiles por tipo (baseTileId + 0..15). 0=full, 1-4=edge N/S/E/W, 5-8=corners, etc.
        if (neighborMask == 15) return baseTileId; // Rodeado = center
        int offset = neighborMask; // 0-14 para variantes
        return baseTileId + Math.Clamp(offset, 0, 15);
    }

    /// <summary>Indica si el auto-tiling está activo para este tileset/tile (requiere 16 variantes por tipo).</summary>
    public static bool SupportsAutoTiling(int tileId, int tilesPerType = 16) => tilesPerType >= 16;
}
