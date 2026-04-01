using System.Collections.Generic;

namespace FUEngine.Core;

/// <summary>
/// Una capa de tilemap: usa un Tileset y guarda solo IDs de tile por celda (ultra ligero).
/// Ejemplo: Tilemap_Background, Tilemap_Walls, Tilemap_Details.
/// </summary>
public class TilemapLayer
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    /// <summary>Referencia al Tileset que define los tiles (textura + propiedades).</summary>
    public string TilesetId { get; set; } = "";
    public int ChunkSize { get; set; } = TilemapChunk.DefaultSize;
    /// <summary>Orden de dibujado (0 = fondo, mayor = delante).</summary>
    public int SortOrder { get; set; }

    private readonly Dictionary<(int cx, int cy), TilemapChunk> _chunks = new();

    private static (int cx, int cy) WorldToChunk(int worldX, int worldY, int chunkSize)
    {
        int cx = worldX < 0 ? (worldX + 1) / chunkSize - 1 : worldX / chunkSize;
        int cy = worldY < 0 ? (worldY + 1) / chunkSize - 1 : worldY / chunkSize;
        return (cx, cy);
    }

    private static (int lx, int ly) WorldToLocal(int worldX, int worldY, int chunkSize)
    {
        int lx = ((worldX % chunkSize) + chunkSize) % chunkSize;
        int ly = ((worldY % chunkSize) + chunkSize) % chunkSize;
        return (lx, ly);
    }

    private TilemapChunk GetOrCreateChunk(int cx, int cy)
    {
        if (!_chunks.TryGetValue((cx, cy), out var chunk))
        {
            chunk = new TilemapChunk(ChunkSize);
            _chunks[(cx, cy)] = chunk;
        }
        return chunk;
    }

    public bool TryGetTileId(int worldX, int worldY, out int tileId)
    {
        var (cx, cy) = WorldToChunk(worldX, worldY, ChunkSize);
        if (!_chunks.TryGetValue((cx, cy), out var chunk))
        {
            tileId = 0;
            return false;
        }
        var (lx, ly) = WorldToLocal(worldX, worldY, ChunkSize);
        return chunk.TryGetTileId(lx, ly, out tileId);
    }

    public void SetTile(int worldX, int worldY, int tileId)
    {
        var (cx, cy) = WorldToChunk(worldX, worldY, ChunkSize);
        var (lx, ly) = WorldToLocal(worldX, worldY, ChunkSize);
        GetOrCreateChunk(cx, cy).SetTile(lx, ly, tileId);
    }

    public void RemoveTile(int worldX, int worldY)
    {
        var (cx, cy) = WorldToChunk(worldX, worldY, ChunkSize);
        if (_chunks.TryGetValue((cx, cy), out var chunk))
        {
            var (lx, ly) = WorldToLocal(worldX, worldY, ChunkSize);
            chunk.RemoveTile(lx, ly);
        }
    }

    public IEnumerable<(int cx, int cy)> EnumerateChunkCoords()
    {
        foreach (var k in _chunks.Keys)
            yield return k;
    }

    public TilemapChunk? GetChunk(int cx, int cy) =>
        _chunks.TryGetValue((cx, cy), out var c) ? c : null;

    public TilemapChunk GetOrCreateChunkAt(int cx, int cy) => GetOrCreateChunk(cx, cy);
}
