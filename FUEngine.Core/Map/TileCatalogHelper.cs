namespace FUEngine.Core;

/// <summary>Construye <see cref="TileData"/> a partir de un tile del catálogo (tileset).</summary>
public static class TileCatalogHelper
{
    public static TileData CreatePlacedTile(Tileset tileset, int catalogTileId, MapLayerDescriptor layer,
        int atlasSubRectX = 0, int atlasSubRectY = 0, int atlasSubRectW = 0, int atlasSubRectH = 0)
    {
        var def = tileset.GetTile(catalogTileId);
        bool solidLayer = layer.LayerType == LayerType.Solid;
        bool col = solidLayer || (def?.Collision ?? false);
        var tex = (tileset.TexturePath ?? "").Replace('\\', '/').Trim();
        var td = new TileData
        {
            TipoTile = TileType.Suelo,
            Colision = col,
            SourceImagePath = string.IsNullOrEmpty(tex) ? null : tex,
            CatalogTileId = catalogTileId,
            TilesetPath = string.IsNullOrEmpty(tex) ? null : tex,
            CatalogGridTileWidth = Math.Max(1, tileset.TileWidth),
            CatalogGridTileHeight = Math.Max(1, tileset.TileHeight)
        };
        if (atlasSubRectW > 0 && atlasSubRectH > 0)
        {
            td.AtlasSubRectX = atlasSubRectX;
            td.AtlasSubRectY = atlasSubRectY;
            td.AtlasSubRectW = atlasSubRectW;
            td.AtlasSubRectH = atlasSubRectH;
        }
        return td;
    }
}
