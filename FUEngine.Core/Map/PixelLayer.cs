using System.Collections.Generic;

namespace FUEngine.Core;

/// <summary>
/// Capa de píxeles (edición pixel a pixel). Para mundo destructible o dibujo directo.
/// Opcional: consume más memoria. Chunked para mapas grandes.
/// </summary>
public class PixelLayer
{
    public string Id { get; set; } = "";
    public int Width { get; set; } = 1024;
    public int Height { get; set; } = 1024;
    /// <summary>Color por coordenada (ARGB). Chunks de 32x32 para no tener un array gigante en memoria.</summary>
    private readonly Dictionary<(int cx, int cy), uint[,]> _chunks = new();
    private const int ChunkSize = 32;

    private static (int cx, int cy) PixelToChunk(int px, int py)
    {
        int cx = px < 0 ? (px + 1) / ChunkSize - 1 : px / ChunkSize;
        int cy = py < 0 ? (py + 1) / ChunkSize - 1 : py / ChunkSize;
        return (cx, cy);
    }

    private uint[,] GetOrCreateChunk(int cx, int cy)
    {
        if (!_chunks.TryGetValue((cx, cy), out var chunk))
        {
            chunk = new uint[ChunkSize, ChunkSize];
            _chunks[(cx, cy)] = chunk;
        }
        return chunk;
    }

    public uint GetPixel(int px, int py)
    {
        if (px < 0 || px >= Width || py < 0 || py >= Height) return 0;
        var (cx, cy) = PixelToChunk(px, py);
        var chunk = GetOrCreateChunk(cx, cy);
        int lx = ((px % ChunkSize) + ChunkSize) % ChunkSize;
        int ly = ((py % ChunkSize) + ChunkSize) % ChunkSize;
        return chunk[lx, ly];
    }

    public void SetPixel(int px, int py, uint color)
    {
        if (px < 0 || px >= Width || py < 0 || py >= Height) return;
        var (cx, cy) = PixelToChunk(px, py);
        var chunk = GetOrCreateChunk(cx, cy);
        int lx = ((px % ChunkSize) + ChunkSize) % ChunkSize;
        int ly = ((py % ChunkSize) + ChunkSize) % ChunkSize;
        chunk[lx, ly] = color;
    }
}
