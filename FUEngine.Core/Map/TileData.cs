namespace FUEngine.Core;

/// <summary>
/// Datos de una celda de tile en el mapa.
/// Soporta 2.5D (altura) y transparencia para luces/ambientes.
/// </summary>
public class TileData
{
    public TileType TipoTile { get; set; }
    public bool Colision { get; set; }
    public bool Interactivo { get; set; }
    /// <summary>Si true, la celda no bloquea luz ni efectos de blending (ej: ventanas, zonas de dithering).</summary>
    public bool Transparente { get; set; }
    /// <summary>Altura/Z para pseudo-3D o 2.5D (orden de dibujo, rampas, volumen).</summary>
    public int Height { get; set; }
    /// <summary>Identificador del script asociado (para tipo Especial o eventos por tile).</summary>
    public string? ScriptId { get; set; }
    /// <summary>Índice de capa (0 = suelo, 1 = decorativo, etc.). Ver ProjectInfo.LayerNames.</summary>
    public int LayerId { get; set; }
    /// <summary>Etiquetas para filtros, scripts y búsqueda (ej: "puerta", "trampa", "spawn").</summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>Ruta a imagen importada (PNG, etc.) como base del tile. Si null, se usa el color por TipoTile.</summary>
    public string? SourceImagePath { get; set; }

    /// <summary>ID en el catálogo del tileset (0 = no asociado al catálogo / mapa pintado a mano clásico).</summary>
    public int CatalogTileId { get; set; }

    /// <summary>Ruta relativa al proyecto del atlas PNG del tileset (cuando <see cref="CatalogTileId"/> &gt; 0).</summary>
    public string? TilesetPath { get; set; }

    /// <summary>Ancho en píxeles de una celda en el atlas (para recorte cuando hay catálogo).</summary>
    public int CatalogGridTileWidth { get; set; }

    /// <summary>Alto en píxeles de una celda en el atlas.</summary>
    public int CatalogGridTileHeight { get; set; }

    /// <summary>Recorte opcional dentro del PNG del atlas (píxeles). Si <see cref="AtlasSubRectW"/> y <see cref="AtlasSubRectH"/> son &gt; 0, sustituye al recorte por rejilla de <see cref="CatalogTileId"/>.</summary>
    public int AtlasSubRectX { get; set; }
    public int AtlasSubRectY { get; set; }
    public int AtlasSubRectW { get; set; }
    public int AtlasSubRectH { get; set; }

    /// <summary>Overlay editable a nivel píxel (mismas dimensiones que el tile). Se dibuja encima de la base.</summary>
    public TilePixelOverlay? PixelOverlay { get; set; }

    public TileData Clone()
    {
        return new TileData
        {
            TipoTile = TipoTile,
            Colision = Colision,
            Interactivo = Interactivo,
            Transparente = Transparente,
            Height = Height,
            ScriptId = ScriptId,
            LayerId = LayerId,
            Tags = new List<string>(Tags),
            SourceImagePath = SourceImagePath,
            CatalogTileId = CatalogTileId,
            TilesetPath = TilesetPath,
            CatalogGridTileWidth = CatalogGridTileWidth,
            CatalogGridTileHeight = CatalogGridTileHeight,
            AtlasSubRectX = AtlasSubRectX,
            AtlasSubRectY = AtlasSubRectY,
            AtlasSubRectW = AtlasSubRectW,
            AtlasSubRectH = AtlasSubRectH,
            PixelOverlay = PixelOverlay?.Clone()
        };
    }
}
