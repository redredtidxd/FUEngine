namespace FUEngine.Editor;

/// <summary>
/// DTO para guardar/cargar el mapa (capas, chunks y tiles).
/// Chunks se vinculan a la capa por LayerId para no depender del orden.
/// </summary>
public class MapDto
{
    public int ChunkSize { get; set; }
    public List<LayerDto> Layers { get; set; } = new();
    public List<ChunkDto> Chunks { get; set; } = new();
}

public class LayerDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int LayerType { get; set; }
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsLocked { get; set; }
    public int Opacity { get; set; } = 100;
    public int BlendMode { get; set; }
    public float ParallaxX { get; set; } = 1f;
    public float ParallaxY { get; set; } = 1f;
    public float OffsetX { get; set; }
    public float OffsetY { get; set; }
    public uint CollisionLayer { get; set; } = 1;
    public uint CollisionMask { get; set; } = 0xFFFF;
    public bool RenderAbovePlayer { get; set; }
    public string? BackgroundTexturePath { get; set; }
    public string? TilesetAssetPath { get; set; }
}

public class ChunkDto
{
    /// <summary>Id de la capa a la que pertenece este chunk.</summary>
    public string? LayerId { get; set; }
    public int Cx { get; set; }
    public int Cy { get; set; }
    public List<TileDto> Tiles { get; set; } = new();
}

public class TileDto
{
    public int X { get; set; }
    public int Y { get; set; }
    public int TipoTile { get; set; }
    public bool Colision { get; set; }
    public bool Interactivo { get; set; }
    public bool Transparente { get; set; }
    public int Height { get; set; }
    public string? ScriptId { get; set; }
    public int LayerId { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? SourceImagePath { get; set; }
    public int CatalogTileId { get; set; }
    public string? TilesetPath { get; set; }
    public int CatalogGridTileWidth { get; set; }
    public int CatalogGridTileHeight { get; set; }
    public string? OverlayBase64 { get; set; }
    public int OverlayWidth { get; set; }
    public int OverlayHeight { get; set; }
}
