using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using FUEngine.Core;

namespace FUEngine.Editor;

public static class MapSerialization
{
    public static void Save(TileMap map, string path)
    {
        var dto = ToDto(map);
        var json = JsonSerializer.Serialize(dto, SerializationDefaults.Options);
        File.WriteAllText(path, json);
    }

    public static TileMap Load(string path)
    {
        if (!File.Exists(path))
            return new TileMap(Chunk.DefaultSize);
        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"No se pudo leer el archivo del mapa: {ex.Message}", ex);
        }
        MapDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<MapDto>(json, SerializationDefaults.Options);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"JSON del mapa inválido (línea {ex.LineNumber}, posición {ex.BytePositionInLine}): {ex.Message}", ex);
        }
        if (dto == null)
            throw new InvalidOperationException("El archivo del mapa está vacío o mal formado.");
        return FromDto(dto);
    }

    public static MapDto ToDto(TileMap map)
    {
        var layers = new List<LayerDto>();
        foreach (var desc in map.Layers)
        {
            layers.Add(new LayerDto
            {
                Id = desc.Id,
                Name = desc.Name,
                LayerType = (int)desc.LayerType,
                SortOrder = desc.SortOrder,
                IsVisible = desc.IsVisible,
                IsLocked = desc.IsLocked,
                Opacity = desc.Opacity,
                BlendMode = (int)desc.BlendMode,
                ParallaxX = desc.ParallaxX,
                ParallaxY = desc.ParallaxY,
                OffsetX = desc.OffsetX,
                OffsetY = desc.OffsetY,
                CollisionLayer = desc.CollisionLayer,
                CollisionMask = desc.CollisionMask,
                RenderAbovePlayer = desc.RenderAbovePlayer,
                BackgroundTexturePath = desc.BackgroundTexturePath,
                TilesetAssetPath = desc.TilesetAssetPath,
                LayerScriptId = desc.LayerScriptId,
                LayerScriptEnabled = desc.LayerScriptEnabled,
                LayerScriptProperties = (desc.LayerScriptProperties ?? new List<ScriptPropertyEntry>())
                    .Select(p => new ScriptPropertyEntryDto { Key = p.Key, Type = p.Type ?? "string", Value = p.Value ?? "" }).ToList()
            });
        }

        var chunks = new List<ChunkDto>();
        for (int layerIndex = 0; layerIndex < map.Layers.Count; layerIndex++)
        {
            var layerId = map.Layers[layerIndex].Id;
            foreach (var (cx, cy) in map.EnumerateChunkCoords(layerIndex))
            {
                var chunk = map.GetChunk(layerIndex, cx, cy);
                if (chunk == null) continue;
                var tiles = new List<TileDto>();
                foreach (var (x, y, data) in chunk.EnumerateTiles())
                {
                    var tileDto = new TileDto
                    {
                        X = x,
                        Y = y,
                        TipoTile = (int)data.TipoTile,
                        Colision = data.Colision,
                        Interactivo = data.Interactivo,
                        Transparente = data.Transparente,
                        Height = data.Height,
                        ScriptId = data.ScriptId,
                        LayerId = layerIndex,
                        Tags = data.Tags ?? new List<string>(),
                        SourceImagePath = data.SourceImagePath,
                        CatalogTileId = data.CatalogTileId,
                        TilesetPath = data.TilesetPath,
                        CatalogGridTileWidth = data.CatalogGridTileWidth,
                        CatalogGridTileHeight = data.CatalogGridTileHeight
                    };
                    if (data.PixelOverlay != null && data.PixelOverlay.RgbaData != null && data.PixelOverlay.RgbaData.Length > 0)
                    {
                        tileDto.OverlayBase64 = Convert.ToBase64String(data.PixelOverlay.RgbaData);
                        tileDto.OverlayWidth = data.PixelOverlay.Width;
                        tileDto.OverlayHeight = data.PixelOverlay.Height;
                    }
                    tiles.Add(tileDto);
                }
                chunks.Add(new ChunkDto { LayerId = layerId, Cx = cx, Cy = cy, Tiles = tiles });
            }
        }
        return new MapDto { ChunkSize = map.ChunkSize, Layers = layers, Chunks = chunks };
    }

    public static TileMap FromDto(MapDto dto)
    {
        if (dto == null) return new TileMap(Chunk.DefaultSize);
        var map = new TileMap(dto.ChunkSize);

        bool legacy = dto.Layers == null || dto.Layers.Count == 0;

        if (legacy)
        {
            // Formato antiguo: un solo grid; todos los chunks van a la capa 0 (Suelo).
            foreach (var cd in dto.Chunks ?? new List<ChunkDto>())
            {
                var chunk = map.GetOrCreateChunkAt(0, cd.Cx, cd.Cy);
                foreach (var t in cd.Tiles ?? new List<TileDto>())
                    SetTileFromDto(chunk, t);
            }
            return map;
        }

        // Formato con capas: vincular por LayerId.
        if (dto.Layers == null) return map;
        var descriptors = new List<MapLayerDescriptor>();
        for (int li = 0; li < dto.Layers.Count; li++)
        {
            var ld = dto.Layers[li];
            var layerName = string.IsNullOrWhiteSpace(ld.Name) ? $"Capa {li}" : ld.Name!;
            descriptors.Add(new MapLayerDescriptor
            {
                Id = ld.Id ?? Guid.NewGuid().ToString("N"),
                Name = layerName,
                LayerType = (LayerType)ld.LayerType,
                SortOrder = ld.SortOrder,
                IsVisible = ld.IsVisible,
                IsLocked = ld.IsLocked,
                Opacity = ld.Opacity,
                BlendMode = (LayerBlendMode)ld.BlendMode,
                ParallaxX = ld.ParallaxX,
                ParallaxY = ld.ParallaxY,
                OffsetX = ld.OffsetX,
                OffsetY = ld.OffsetY,
                CollisionLayer = ld.CollisionLayer,
                CollisionMask = ld.CollisionMask,
                RenderAbovePlayer = ld.RenderAbovePlayer,
                BackgroundTexturePath = ld.BackgroundTexturePath,
                TilesetAssetPath = ld.TilesetAssetPath,
                LayerScriptId = ld.LayerScriptId,
                LayerScriptEnabled = ld.LayerScriptEnabled,
                LayerScriptProperties = (ld.LayerScriptProperties ?? new List<ScriptPropertyEntryDto>())
                    .Select(p => new ScriptPropertyEntry { Key = p.Key, Type = p.Type ?? "string", Value = p.Value ?? "" }).ToList()
            });
        }
        map.ReplaceLayers(descriptors);

        var layerIdToIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < map.Layers.Count; i++)
            layerIdToIndex[map.Layers[i].Id] = i;

        foreach (var cd in dto.Chunks ?? new List<ChunkDto>())
        {
            var layerId = cd.LayerId;
            if (string.IsNullOrEmpty(layerId) || !layerIdToIndex.TryGetValue(layerId, out int layerIndex))
                continue;
            var chunk = map.GetOrCreateChunkAt(layerIndex, cd.Cx, cd.Cy);
            foreach (var t in cd.Tiles ?? new List<TileDto>())
                SetTileFromDto(chunk, t);
        }
        return map;
    }

    private static void SetTileFromDto(Chunk chunk, TileDto t)
    {
        var tileData = new TileData
        {
            TipoTile = (TileType)t.TipoTile,
            Colision = t.Colision,
            Interactivo = t.Interactivo,
            Transparente = t.Transparente,
            Height = t.Height,
            ScriptId = t.ScriptId,
            LayerId = t.LayerId,
            Tags = t.Tags ?? new List<string>(),
            SourceImagePath = t.SourceImagePath,
            CatalogTileId = t.CatalogTileId,
            TilesetPath = t.TilesetPath,
            CatalogGridTileWidth = t.CatalogGridTileWidth,
            CatalogGridTileHeight = t.CatalogGridTileHeight
        };
        if (!string.IsNullOrEmpty(t.OverlayBase64) && t.OverlayWidth > 0 && t.OverlayHeight > 0)
        {
            try
            {
                var overlayBytes = Convert.FromBase64String(t.OverlayBase64);
                tileData.PixelOverlay = new TilePixelOverlay(t.OverlayWidth, t.OverlayHeight);
                int expectedLen = tileData.PixelOverlay.RgbaData.Length;
                int copyLen = Math.Min(overlayBytes.Length, expectedLen);
                Array.Copy(overlayBytes, tileData.PixelOverlay.RgbaData, copyLen);
                if (overlayBytes.Length < expectedLen)
                    Trace.TraceWarning($"[MapSerialization] Overlay de tile incompleto: {overlayBytes.Length} bytes, se esperaban {expectedLen}.");
            }
            catch (Exception ex) { Trace.TraceWarning($"[MapSerialization] Overlay de tile inválido: {ex.Message}"); }
        }
                chunk.SetTile(t.X, t.Y, tileData);
    }

    /// <summary>Un chunk para caché de streaming (Play): capa por <see cref="ChunkDto.LayerId"/>.</summary>
    public static ChunkDto? ToChunkDto(TileMap map, int layerIndex, int cx, int cy)
    {
        if (map == null || layerIndex < 0 || layerIndex >= map.Layers.Count) return null;
        var chunk = map.GetChunk(layerIndex, cx, cy);
        if (chunk == null) return null;
        var tiles = new List<TileDto>();
        foreach (var (x, y, data) in chunk.EnumerateTiles())
        {
            var tileDto = new TileDto
            {
                X = x,
                Y = y,
                TipoTile = (int)data.TipoTile,
                Colision = data.Colision,
                Interactivo = data.Interactivo,
                Transparente = data.Transparente,
                Height = data.Height,
                ScriptId = data.ScriptId,
                LayerId = layerIndex,
                Tags = data.Tags ?? new List<string>(),
                SourceImagePath = data.SourceImagePath,
                CatalogTileId = data.CatalogTileId,
                TilesetPath = data.TilesetPath,
                CatalogGridTileWidth = data.CatalogGridTileWidth,
                CatalogGridTileHeight = data.CatalogGridTileHeight
            };
            if (data.PixelOverlay != null && data.PixelOverlay.RgbaData != null && data.PixelOverlay.RgbaData.Length > 0)
            {
                tileDto.OverlayBase64 = Convert.ToBase64String(data.PixelOverlay.RgbaData);
                tileDto.OverlayWidth = data.PixelOverlay.Width;
                tileDto.OverlayHeight = data.PixelOverlay.Height;
            }
            tiles.Add(tileDto);
        }
        return new ChunkDto
        {
            LayerId = map.Layers[layerIndex].Id,
            Cx = cx,
            Cy = cy,
            Tiles = tiles
        };
    }

    public static void SaveChunkDtoToFile(ChunkDto dto, string path)
    {
        var json = JsonSerializer.Serialize(dto, SerializationDefaults.Options);
        File.WriteAllText(path, json);
    }

    /// <summary>Fusiona un chunk desde JSON (streaming). Devuelve false si el archivo o la capa no son válidos.</summary>
    public static bool TryMergeChunkFromFile(TileMap map, string path)
    {
        if (map == null || string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;
        string json;
        try { json = File.ReadAllText(path); }
        catch { return false; }
        ChunkDto? dto;
        try { dto = JsonSerializer.Deserialize<ChunkDto>(json, SerializationDefaults.Options); }
        catch { return false; }
        if (dto == null || string.IsNullOrEmpty(dto.LayerId)) return false;
        int layerIndex = -1;
        for (int i = 0; i < map.Layers.Count; i++)
        {
            if (string.Equals(map.Layers[i].Id, dto.LayerId, StringComparison.OrdinalIgnoreCase))
            {
                layerIndex = i;
                break;
            }
        }
        if (layerIndex < 0) return false;
        var chunk = map.GetOrCreateChunkAt(layerIndex, dto.Cx, dto.Cy);
        foreach (var t in dto.Tiles ?? new List<TileDto>())
            SetTileFromDto(chunk, t);
        return true;
    }
}
