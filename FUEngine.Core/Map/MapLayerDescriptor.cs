using System.Collections.Generic;

namespace FUEngine.Core;

/// <summary>
/// Descriptor de una capa del tilemap: metadatos (nombre, tipo, visibilidad, opacidad, parallax, etc.).
/// El Id es único y generado por el motor; se usa en serialización para vincular chunks a la capa.
/// </summary>
public class MapLayerDescriptor
{
    /// <summary>Identificador único interno (no editable en UI).</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = "Capa";

    public LayerType LayerType { get; set; } = LayerType.Background;

    /// <summary>Orden de dibujado (0 = fondo, mayor = delante).</summary>
    public int SortOrder { get; set; }

    public bool IsVisible { get; set; } = true;

    public bool IsLocked { get; set; }

    /// <summary>Opacidad 0-100.</summary>
    public int Opacity { get; set; } = 100;

    public LayerBlendMode BlendMode { get; set; } = LayerBlendMode.Normal;

    /// <summary>Parallax (ej. 0.5 = la capa se mueve más lento que la cámara).</summary>
    public float ParallaxX { get; set; } = 1f;
    public float ParallaxY { get; set; } = 1f;

    /// <summary>Desplazamiento en píxeles de toda la capa.</summary>
    public float OffsetX { get; set; }
    public float OffsetY { get; set; }

    /// <summary>Máscara de capa de colisión (bit mask para física).</summary>
    public uint CollisionLayer { get; set; } = 1;

    /// <summary>Máscara con la que esta capa colisiona (bit mask).</summary>
    public uint CollisionMask { get; set; } = 0xFFFF;

    /// <summary>Si true, la capa se dibuja encima del jugador; si false, debajo.</summary>
    public bool RenderAbovePlayer { get; set; }

    /// <summary>Ruta relativa al proyecto de la textura de fondo (imagen de pintura). Si está definida, se dibuja antes que los tiles.</summary>
    public string? BackgroundTexturePath { get; set; }

    /// <summary>Ruta relativa al JSON del tileset (ej. Assets/Tilesets/terrain.tileset.json) para Lua/API por ID en esta capa.</summary>
    public string? TilesetAssetPath { get; set; }

    /// <summary>Ruta relativa al proyecto del script Lua de capa (mismo criterio que ScriptComponent). Vacío = sin script.</summary>
    public string? LayerScriptId { get; set; }

    /// <summary>Si false, no se carga ni ejecuta el script de capa en Play.</summary>
    public bool LayerScriptEnabled { get; set; } = true;

    /// <summary>Propiedades inyectadas en el entorno Lua del script de capa (como en objetos).</summary>
    public List<ScriptPropertyEntry> LayerScriptProperties { get; set; } = new();

    public MapLayerDescriptor Clone()
    {
        return new MapLayerDescriptor
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = Name,
            LayerType = LayerType,
            SortOrder = SortOrder,
            IsVisible = IsVisible,
            IsLocked = IsLocked,
            Opacity = Opacity,
            BlendMode = BlendMode,
            ParallaxX = ParallaxX,
            ParallaxY = ParallaxY,
            OffsetX = OffsetX,
            OffsetY = OffsetY,
            CollisionLayer = CollisionLayer,
            CollisionMask = CollisionMask,
            RenderAbovePlayer = RenderAbovePlayer,
            BackgroundTexturePath = BackgroundTexturePath,
            TilesetAssetPath = TilesetAssetPath,
            LayerScriptId = LayerScriptId,
            LayerScriptEnabled = LayerScriptEnabled,
            LayerScriptProperties = LayerScriptProperties?.ConvertAll(p => new ScriptPropertyEntry { Key = p.Key, Type = p.Type, Value = p.Value }) ?? new List<ScriptPropertyEntry>()
        };
    }
}
