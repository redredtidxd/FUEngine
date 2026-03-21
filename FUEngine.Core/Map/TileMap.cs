namespace FUEngine.Core;

/// <summary>
/// Mapa de tiles con múltiples capas independientes. Cada capa tiene su propio diccionario de chunks.
/// IsCollisionAt considera capas de tipo Solid como muro; en otras capas usa TileData.Colision.
/// </summary>
public class TileMap
{
    private readonly List<MapLayerDescriptor> _layerDescriptors = new();
    private readonly List<Dictionary<(int cx, int cy), Chunk>> _layerChunks = new();
    /// <summary>Chunks tocados en runtime (Lua/API); no se usan para eviction de chunks vacíos.</summary>
    private readonly HashSet<(int layer, int cx, int cy)> _runtimeTouchedChunks = new();

    public int ChunkSize { get; }

    public TileMap(int chunkSize = Chunk.DefaultSize)
    {
        ChunkSize = chunkSize;
        AddLayer(new MapLayerDescriptor { Name = "Suelo", LayerType = LayerType.Background, SortOrder = 0 });
    }

    /// <summary>Descriptor de cada capa (orden = índice de capa).</summary>
    public IReadOnlyList<MapLayerDescriptor> Layers => _layerDescriptors;

    private static (int cx, int cy) WorldToChunk(int worldX, int worldY, int chunkSize)
    {
        int cx = worldX < 0 ? (worldX + 1) / chunkSize - 1 : worldX / chunkSize;
        int cy = worldY < 0 ? (worldY + 1) / chunkSize - 1 : worldY / chunkSize;
        return (cx, cy);
    }

    /// <summary>Convierte coordenadas de celda del mundo a índice de chunk (Chebyshev / diccionario).</summary>
    public (int cx, int cy) WorldTileToChunk(int worldTileX, int worldTileY)
    {
        int cs = Math.Max(1, ChunkSize);
        return WorldToChunk(worldTileX, worldTileY, cs);
    }

    private static (int lx, int ly) WorldToLocal(int worldX, int worldY, int chunkSize)
    {
        int lx = ((worldX % chunkSize) + chunkSize) % chunkSize;
        int ly = ((worldY % chunkSize) + chunkSize) % chunkSize;
        return (lx, ly);
    }

    private Dictionary<(int cx, int cy), Chunk> GetChunksForLayer(int layerIndex)
    {
        if (layerIndex < 0 || layerIndex >= _layerChunks.Count)
            throw new ArgumentOutOfRangeException(nameof(layerIndex));
        return _layerChunks[layerIndex];
    }

    private Chunk GetOrCreateChunk(int layerIndex, int cx, int cy)
    {
        var chunks = GetChunksForLayer(layerIndex);
        if (!chunks.TryGetValue((cx, cy), out var chunk))
        {
            chunk = new Chunk(ChunkSize);
            chunks[(cx, cy)] = chunk;
        }
        return chunk;
    }

    public bool TryGetTile(int layerIndex, int worldX, int worldY, out TileData? data)
    {
        data = null;
        if (layerIndex < 0 || layerIndex >= _layerChunks.Count) return false;
        var (cx, cy) = WorldToChunk(worldX, worldY, ChunkSize);
        if (!_layerChunks[layerIndex].TryGetValue((cx, cy), out var chunk))
            return false;
        var (lx, ly) = WorldToLocal(worldX, worldY, ChunkSize);
        return chunk.TryGetTile(lx, ly, out data);
    }

    public void SetTile(int layerIndex, int worldX, int worldY, TileData tile)
    {
        var (cx, cy) = WorldToChunk(worldX, worldY, ChunkSize);
        var (lx, ly) = WorldToLocal(worldX, worldY, ChunkSize);
        GetOrCreateChunk(layerIndex, cx, cy).SetTile(lx, ly, tile);
    }

    /// <summary>Marca el chunk como modificado en runtime (p. ej. desde Lua).</summary>
    public void MarkRuntimeTouched(int layerIndex, int worldX, int worldY)
    {
        if (layerIndex < 0 || layerIndex >= _layerChunks.Count) return;
        var (cx, cy) = WorldToChunk(worldX, worldY, ChunkSize);
        _runtimeTouchedChunks.Add((layerIndex, cx, cy));
    }

    public void SetTileFromRuntime(int layerIndex, int worldX, int worldY, TileData tile)
    {
        SetTile(layerIndex, worldX, worldY, tile);
        MarkRuntimeTouched(layerIndex, worldX, worldY);
    }

    public void RemoveTileFromRuntime(int layerIndex, int worldX, int worldY)
    {
        RemoveTile(layerIndex, worldX, worldY);
        MarkRuntimeTouched(layerIndex, worldX, worldY);
    }

    public void RemoveTile(int layerIndex, int worldX, int worldY)
    {
        if (layerIndex < 0 || layerIndex >= _layerChunks.Count) return;
        var (cx, cy) = WorldToChunk(worldX, worldY, ChunkSize);
        if (_layerChunks[layerIndex].TryGetValue((cx, cy), out var chunk))
        {
            var (lx, ly) = WorldToLocal(worldX, worldY, ChunkSize);
            chunk.RemoveTile(lx, ly);
            if (chunk.IsEmpty)
                _layerChunks[layerIndex].Remove((cx, cy));
        }
    }

    /// <summary>
    /// Elimina entradas de chunk totalmente vacías con distancia Chebyshev <b>estrictamente mayor</b> que <paramref name="radiusChunks"/>.
    /// No elimina chunks con tiles.
    /// Si <paramref name="spillRuntimeTouchedEmpty"/> no es null: para vacíos marcados en runtime intenta persistir; si devuelve true, elimina la entrada y el marcado.
    /// Si es null y <paramref name="skipRuntimeTouched"/> es true, conserva vacíos tocados en runtime (solo memoria).
    /// Si es null y <paramref name="skipRuntimeTouched"/> es false, elimina también vacíos tocados (pérdida de estado si no hubo spill externo).
    /// </summary>
    public void EvictEmptyChunksBeyond(int centerCx, int centerCy, int radiusChunks, bool skipRuntimeTouched = false, Func<int, int, int, bool>? spillRuntimeTouchedEmpty = null)
    {
        if (radiusChunks < 0) return;
        for (int li = 0; li < _layerChunks.Count; li++)
        {
            var dict = _layerChunks[li];
            foreach (var kv in dict.ToList())
            {
                var (cx, cy) = kv.Key;
                int dist = Math.Max(Math.Abs(cx - centerCx), Math.Abs(cy - centerCy));
                if (dist <= radiusChunks) continue;
                if (!kv.Value.IsEmpty) continue;
                bool touched = _runtimeTouchedChunks.Contains((li, cx, cy));
                if (touched && spillRuntimeTouchedEmpty != null)
                {
                    if (!spillRuntimeTouchedEmpty(li, cx, cy)) continue;
                    dict.Remove((cx, cy));
                    _runtimeTouchedChunks.Remove((li, cx, cy));
                    continue;
                }
                if (touched && skipRuntimeTouched) continue;
                dict.Remove((cx, cy));
                if (touched)
                    _runtimeTouchedChunks.Remove((li, cx, cy));
            }
        }
    }

    /// <summary>Copia profunda del mapa (capas, chunks y tiles).</summary>
    public TileMap Clone()
    {
        var descClones = _layerDescriptors.Select(d => d.Clone()).ToList();
        var clone = new TileMap(ChunkSize);
        clone.ReplaceLayers(descClones);
        int cs = ChunkSize;
        for (int li = 0; li < _layerChunks.Count; li++)
        {
            foreach (var (cx, cy) in EnumerateChunkCoords(li))
            {
                var ch = GetChunk(li, cx, cy);
                if (ch == null) continue;
                foreach (var (lx, ly, data) in ch.EnumerateTiles())
                {
                    int wx = cx * cs + lx;
                    int wy = cy * cs + ly;
                    clone.SetTile(li, wx, wy, data.Clone());
                }
            }
        }
        return clone;
    }

    /// <summary>
    /// Indica si hay colisión en la celda. Capas tipo Solid: cualquier tile = muro.
    /// Otras capas: se considera TileData.Colision.
    /// </summary>
    public bool IsCollisionAt(int worldX, int worldY)
    {
        for (int i = 0; i < _layerDescriptors.Count; i++)
        {
            if (_layerDescriptors[i].LayerType == LayerType.Solid)
            {
                if (TryGetTile(i, worldX, worldY, out var data) && data != null)
                    return true;
            }
            else
            {
                if (TryGetTile(i, worldX, worldY, out var data) && data != null && data.Colision)
                    return true;
            }
        }
        return false;
    }

    /// <summary>Enumera todas las coordenadas de chunk que existen en cualquier capa.</summary>
    public IEnumerable<(int cx, int cy)> EnumerateChunkCoords()
    {
        var seen = new HashSet<(int, int)>();
        foreach (var chunks in _layerChunks)
        {
            foreach (var k in chunks.Keys)
                if (seen.Add(k)) yield return k;
        }
    }

    /// <summary>Enumera coordenadas de chunk de una capa.</summary>
    public IEnumerable<(int cx, int cy)> EnumerateChunkCoords(int layerIndex)
    {
        if (layerIndex < 0 || layerIndex >= _layerChunks.Count) yield break;
        foreach (var k in _layerChunks[layerIndex].Keys)
            yield return k;
    }

    public Chunk? GetChunk(int layerIndex, int cx, int cy)
    {
        if (layerIndex < 0 || layerIndex >= _layerChunks.Count) return null;
        return _layerChunks[layerIndex].TryGetValue((cx, cy), out var c) ? c : null;
    }

    public Chunk GetOrCreateChunkAt(int layerIndex, int cx, int cy)
    {
        return GetOrCreateChunk(layerIndex, cx, cy);
    }

    /// <summary>Añade una capa al final (nuevo diccionario de chunks vacío).</summary>
    public int AddLayer(MapLayerDescriptor descriptor)
    {
        descriptor.SortOrder = _layerDescriptors.Count;
        _layerDescriptors.Add(descriptor);
        _layerChunks.Add(new Dictionary<(int cx, int cy), Chunk>());
        return _layerDescriptors.Count - 1;
    }

    /// <summary>Elimina la capa en el índice dado; libera todos sus chunks.</summary>
    public void RemoveLayerAt(int layerIndex)
    {
        if (layerIndex < 0 || layerIndex >= _layerDescriptors.Count) return;
        _layerDescriptors.RemoveAt(layerIndex);
        _layerChunks.RemoveAt(layerIndex);
    }

    /// <summary>Reordena capas (intercambia SortOrder y posiciones en la lista).</summary>
    public void MoveLayer(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= _layerDescriptors.Count ||
            toIndex < 0 || toIndex >= _layerDescriptors.Count || fromIndex == toIndex) return;
        var desc = _layerDescriptors[fromIndex];
        var chunks = _layerChunks[fromIndex];
        _layerDescriptors.RemoveAt(fromIndex);
        _layerChunks.RemoveAt(fromIndex);
        _layerDescriptors.Insert(toIndex, desc);
        _layerChunks.Insert(toIndex, chunks);
        for (int i = 0; i < _layerDescriptors.Count; i++)
            _layerDescriptors[i].SortOrder = i;
    }

    /// <summary>Reemplaza todas las capas por la lista dada (para carga desde DTO).</summary>
    public void ReplaceLayers(IReadOnlyList<MapLayerDescriptor> descriptors)
    {
        _layerDescriptors.Clear();
        _layerChunks.Clear();
        foreach (var d in descriptors)
        {
            _layerDescriptors.Add(d);
            _layerChunks.Add(new Dictionary<(int cx, int cy), Chunk>());
        }
    }

    /// <summary>Comprueba si la capa tiene al menos un tile (para confirmar eliminación).</summary>
    public bool LayerHasAnyTiles(int layerIndex)
    {
        if (layerIndex < 0 || layerIndex >= _layerChunks.Count) return false;
        foreach (var chunk in _layerChunks[layerIndex].Values)
        {
            foreach (var _ in chunk.EnumerateTiles())
                return true;
        }
        return false;
    }

    // --- Compatibilidad: API por defecto usa capa 0 ---

    public bool TryGetTile(int worldX, int worldY, out TileData? data) => TryGetTile(0, worldX, worldY, out data);
    public void SetTile(int worldX, int worldY, TileData tile) => SetTile(0, worldX, worldY, tile);
    public void RemoveTile(int worldX, int worldY) => RemoveTile(0, worldX, worldY);
    public Chunk? GetChunk(int cx, int cy) => GetChunk(0, cx, cy);
    public Chunk GetOrCreateChunkAt(int cx, int cy) => GetOrCreateChunkAt(0, cx, cy);
}
