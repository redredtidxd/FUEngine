using FUEngine.Core;
using FUEngine.Editor;

namespace FUEngine;

/// <summary>
/// Datos copiados de una zona del mapa (tiles + objetos) para duplicado masivo.
/// </summary>
public class ZoneClipboard
{
    public int OriginX { get; set; }
    public int OriginY { get; set; }
    public List<ZoneTileEntry> Tiles { get; set; } = new();
    public List<ZoneObjectEntry> Objects { get; set; } = new();

    public bool HasContent => Tiles.Count > 0 || Objects.Count > 0;
}

public class ZoneTileEntry
{
    public int X { get; set; }
    public int Y { get; set; }
    public TileData Data { get; set; } = new();
}

public class ZoneObjectEntry
{
    public string DefinitionId { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public double Rotation { get; set; }
    public string Nombre { get; set; } = "";
}
