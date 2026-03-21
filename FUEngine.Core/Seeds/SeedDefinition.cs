namespace FUEngine.Core;

/// <summary>
/// Plantilla reutilizable (Seed): configuración de objetos (y opcionalmente tiles) para colocar en el mapa.
/// </summary>
public class SeedDefinition
{
    public string Id { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    /// <summary>Objetos que forman el seed (posiciones relativas al origen del seed).</summary>
    public List<SeedObjectEntry> Objects { get; set; } = new();
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Referencia a un objeto dentro de un seed (definición + offset).
/// </summary>
public class SeedObjectEntry
{
    public string DefinitionId { get; set; } = "";
    public double OffsetX { get; set; }
    public double OffsetY { get; set; }
    public double Rotation { get; set; }
    public string? Nombre { get; set; }
}
