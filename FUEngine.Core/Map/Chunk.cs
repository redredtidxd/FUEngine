namespace FUEngine.Core;

/// <summary>
/// Chunk de tiles. Coordenadas locales dentro del chunk (0..Size-1).
/// </summary>
public class Chunk
{
    public const int DefaultSize = 16;

    public int Size { get; }
    private readonly Dictionary<(int x, int y), TileData> _tiles = new();

    public bool IsEmpty => _tiles.Count == 0;

    public Chunk(int size = DefaultSize)
    {
        Size = size;
    }

    public bool TryGetTile(int localX, int localY, out TileData? data)
    {
        if (localX < 0 || localX >= Size || localY < 0 || localY >= Size)
        {
            data = null;
            return false;
        }
        return _tiles.TryGetValue((localX, localY), out data);
    }

    public void SetTile(int localX, int localY, TileData tile)
    {
        if (localX < 0 || localX >= Size || localY < 0 || localY >= Size)
            return;
        _tiles[(localX, localY)] = tile.Clone();
    }

    public void RemoveTile(int localX, int localY)
    {
        _tiles.Remove((localX, localY));
    }

    public bool HasTile(int localX, int localY)
    {
        return localX >= 0 && localX < Size && localY >= 0 && localY < Size &&
               _tiles.ContainsKey((localX, localY));
    }

    /// <summary>
    /// Enumera todas las tiles del chunk (coordenadas locales + datos).
    /// </summary>
    public IEnumerable<(int x, int y, TileData data)> EnumerateTiles()
    {
        foreach (var kv in _tiles)
            yield return (kv.Key.x, kv.Key.y, kv.Value);
    }
}
