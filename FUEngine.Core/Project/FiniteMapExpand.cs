namespace FUEngine.Core;

/// <summary>
/// Expansión de mapa finito por celdas de chunk: un clic añade un chunk vacío (ChunkSize×ChunkSize casillas) en el borde del conjunto existente.
/// </summary>
public static class FiniteMapExpand
{
    public static int FloorDiv(int a, int b)
    {
        if (b <= 0) throw new ArgumentOutOfRangeException(nameof(b));
        return (int)Math.Floor((double)a / b);
    }

    public static bool HasAnyChunkPresent(TileMap map)
    {
        foreach (var _ in map.EnumerateChunkCoords())
            return true;
        return false;
    }

    /// <summary>
    /// Rellena <paramref name="targets"/> con coordenadas de chunk (vacías) que pueden añadirse con un clic: borde del rectángulo del proyecto si no hay datos, o frontera del grafo de chunks si ya hay alguno.
    /// </summary>
    public static void CollectExpandTargetChunks(ProjectInfo p, TileMap map, HashSet<(int cx, int cy)> targets)
    {
        targets.Clear();
        int cs = Math.Max(1, p.ChunkSize);
        if (!HasAnyChunkPresent(map))
        {
            int ox = p.MapBoundsOriginWorldTileX;
            int oy = p.MapBoundsOriginWorldTileY;
            int mw = Math.Max(1, p.MapWidth);
            int mh = Math.Max(1, p.MapHeight);
            int minCx = FloorDiv(ox, cs);
            int maxCx = FloorDiv(ox + mw - 1, cs);
            int minCy = FloorDiv(oy, cs);
            int maxCy = FloorDiv(oy + mh - 1, cs);
            for (int cx = minCx; cx <= maxCx; cx++)
            {
                targets.Add((cx, minCy - 1));
                targets.Add((cx, maxCy + 1));
            }
            for (int cy = minCy; cy <= maxCy; cy++)
            {
                targets.Add((minCx - 1, cy));
                targets.Add((maxCx + 1, cy));
            }
            return;
        }

        foreach (var (cx, cy) in map.EnumerateChunkCoords())
        {
            if (!map.HasAnyChunkAt(cx, cy - 1)) targets.Add((cx, cy - 1));
            if (!map.HasAnyChunkAt(cx, cy + 1)) targets.Add((cx, cy + 1));
            if (!map.HasAnyChunkAt(cx - 1, cy)) targets.Add((cx - 1, cy));
            if (!map.HasAnyChunkAt(cx + 1, cy)) targets.Add((cx + 1, cy));
        }
    }

    public static bool IsExpandTargetChunk(ProjectInfo p, TileMap map, int tcx, int tcy)
    {
        int cs = Math.Max(1, p.ChunkSize);
        if (map.HasAnyChunkAt(tcx, tcy)) return false;

        if (!HasAnyChunkPresent(map))
        {
            int ox = p.MapBoundsOriginWorldTileX;
            int oy = p.MapBoundsOriginWorldTileY;
            int mw = Math.Max(1, p.MapWidth);
            int mh = Math.Max(1, p.MapHeight);
            int minCx = FloorDiv(ox, cs);
            int maxCx = FloorDiv(ox + mw - 1, cs);
            int minCy = FloorDiv(oy, cs);
            int maxCy = FloorDiv(oy + mh - 1, cs);
            bool onNorth = tcx >= minCx && tcx <= maxCx && tcy == minCy - 1;
            bool onSouth = tcx >= minCx && tcx <= maxCx && tcy == maxCy + 1;
            bool onWest = tcy >= minCy && tcy <= maxCy && tcx == minCx - 1;
            bool onEast = tcy >= minCy && tcy <= maxCy && tcx == maxCx + 1;
            return onNorth || onSouth || onWest || onEast;
        }

        return map.HasAnyChunkAt(tcx - 1, tcy) || map.HasAnyChunkAt(tcx + 1, tcy)
            || map.HasAnyChunkAt(tcx, tcy - 1) || map.HasAnyChunkAt(tcx, tcy + 1);
    }

    /// <summary>Actualiza origen y tamaño del rectángulo de juego según la unión de todos los chunks (cualquier capa).</summary>
    public static void SyncProjectBoundsFromChunkUnion(ProjectInfo p, TileMap map)
    {
        if (!map.TryGetWorldBoundsFromChunkUnion(out int minWx, out int minWy, out int maxWxEx, out int maxWyEx))
            return;
        p.MapBoundsOriginWorldTileX = minWx;
        p.MapBoundsOriginWorldTileY = minWy;
        p.MapWidth = maxWxEx - minWx;
        p.MapHeight = maxWyEx - minWy;
    }
}
