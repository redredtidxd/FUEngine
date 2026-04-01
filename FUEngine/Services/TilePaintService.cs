using FUEngine.Core;

namespace FUEngine;

/// <summary>
/// Domain service for tile painting operations (e.g. flood fill).
/// Returns data for the caller to apply and push to history; no UI or history dependency.
/// </summary>
public static class TilePaintService
{
    /// <summary>
    /// Computes the set of (tx, ty) positions to fill with a flood-fill from (startTx, startTy).
    /// Only fills cells with the same TileType as the seed. Respects isInsideSelection (e.g. current tile selection rect).
    /// </summary>
    /// <param name="tileMap">The map.</param>
    /// <param name="startTx">Start X in tiles.</param>
    /// <param name="startTy">Start Y in tiles.</param>
    /// <param name="isInsideSelection">Predicate: true if (tx, ty) is inside the allowed fill region (e.g. selection rect). Use () => true for no restriction.</param>
    /// <param name="maxFill">Maximum number of cells to fill (prevents runaway).</param>
    /// <param name="layerIndex">Layer to read/fill (default 0).</param>
    /// <returns>List of (tx, ty) to paint; caller applies new tile and pushes history.</returns>
    public static IReadOnlyList<(int tx, int ty)> ComputeBucketFill(
        TileMap tileMap,
        int startTx,
        int startTy,
        Func<int, int, bool> isInsideSelection,
        int maxFill = 2000,
        int layerIndex = 0)
    {
        if (tileMap == null || !tileMap.TryGetTile(layerIndex, startTx, startTy, out var seedData) || seedData == null)
            return Array.Empty<(int, int)>();
        if (!isInsideSelection(startTx, startTy))
            return Array.Empty<(int, int)>();

        var result = new List<(int, int)>();
        var visited = new HashSet<(int, int)>();
        var stack = new Stack<(int, int)>();
        stack.Push((startTx, startTy));

        while (stack.Count > 0 && result.Count < maxFill)
        {
            var (tx, ty) = stack.Pop();
            if (visited.Contains((tx, ty))) continue;
            if (!isInsideSelection(tx, ty)) continue;
            if (!tileMap.TryGetTile(layerIndex, tx, ty, out var cur) || cur == null || cur.TipoTile != seedData.TipoTile)
                continue;
            visited.Add((tx, ty));
            result.Add((tx, ty));
            stack.Push((tx + 1, ty));
            stack.Push((tx - 1, ty));
            stack.Push((tx, ty + 1));
            stack.Push((tx, ty - 1));
        }

        return result;
    }
}
