namespace FUEngine.Core;

/// <summary>Construye <see cref="TileData"/> a partir de un tile del catálogo (tileset).</summary>
public static class TileCatalogHelper
{
    public static TileData CreatePlacedTile(Tileset tileset, int catalogTileId, MapLayerDescriptor layer)
    {
        var def = tileset.GetTile(catalogTileId);
        bool solidLayer = layer.LayerType == LayerType.Solid;
        bool col = solidLayer || (def?.Collision ?? false);
        var tex = (tileset.TexturePath ?? "").Replace('\\', '/').Trim();
        return new TileData
        {
            TipoTile = TileType.Suelo,
            Colision = col,
            SourceImagePath = string.IsNullOrEmpty(tex) ? null : tex,
            CatalogTileId = catalogTileId,
            TilesetPath = string.IsNullOrEmpty(tex) ? null : tex,
            CatalogGridTileWidth = Math.Max(1, tileset.TileWidth),
            CatalogGridTileHeight = Math.Max(1, tileset.TileHeight)
        };
    }
}
