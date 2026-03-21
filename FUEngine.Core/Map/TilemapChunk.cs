using System.Collections.Generic;

namespace FUEngine.Core;

/// <summary>
/// Chunk que guarda solo IDs de tile (referencia al Tileset). Mapa ultra ligero.
/// Coordenadas locales dentro del chunk (0..Size-1).
/// </summary>
public class TilemapChunk
{
    public const int DefaultSize = 16;
    public int Size { get; }
    private readonly Dictionary<(int x, int y), int> _tileIds = new();

    public TilemapChunk(int size = DefaultSize)
    {
        Size = size;
    }

    public bool TryGetTileId(int localX, int localY, out int tileId)
    {
        if (localX < 0 || localX >= Size || localY < 0 || localY >= Size)
        {
            tileId = 0;
            return false;
        }
        return _tileIds.TryGetValue((localX, localY), out tileId);
    }

    public void SetTile(int localX, int localY, int tileId)
    {
        if (localX < 0 || localX >= Size || localY < 0 || localY >= Size) return;
        _tileIds[(localX, localY)] = tileId;
    }

    public void RemoveTile(int localX, int localY)
    {
        _tileIds.Remove((localX, localY));
    }

    public bool HasTile(int localX, int localY)
    {
        return localX >= 0 && localX < Size && localY >= 0 && localY < Size &&
               _tileIds.ContainsKey((localX, localY));
    }

    public IEnumerable<(int x, int y, int tileId)> EnumerateTiles()
    {
        foreach (var kv in _tileIds)
            yield return (kv.Key.x, kv.Key.y, kv.Value);
    }
}
