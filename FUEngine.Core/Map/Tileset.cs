using System.Collections.Generic;

namespace FUEngine.Core;

/// <summary>
/// Tileset: imagen base (Texture) + tamaño de tile + definiciones de cada tile.
/// El motor corta la textura en grid TileWidth x TileHeight; cada celda es un Tile con Id = índice.
/// </summary>
public class Tileset
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    /// <summary>Ruta a la imagen (PNG).</summary>
    public string TexturePath { get; set; } = "";
    public int TileWidth { get; set; } = 16;
    public int TileHeight { get; set; } = 16;

    /// <summary>Definiciones por Id (índice en el grid). Puede haber menos entradas que celdas; las sin definir usan defaults.</summary>
    private readonly Dictionary<int, Tile> _tiles = new();

    public void SetTile(Tile tile)
    {
        _tiles[tile.Id] = tile;
    }

    public Tile? GetTile(int tileId)
    {
        return _tiles.TryGetValue(tileId, out var t) ? t : null;
    }

    /// <summary>Obtiene o crea una definición mínima para el tileId (colisión=false, etc.).</summary>
    public Tile GetOrCreateTile(int tileId)
    {
        if (_tiles.TryGetValue(tileId, out var t)) return t;
        var tile = new Tile { Id = tileId };
        _tiles[tileId] = tile;
        return tile;
    }

    public IEnumerable<(int id, Tile tile)> EnumerateTiles()
    {
        foreach (var kv in _tiles)
            yield return (kv.Key, kv.Value);
    }
}
