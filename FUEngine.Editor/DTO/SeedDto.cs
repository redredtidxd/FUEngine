using System.Text.Json.Serialization;

namespace FUEngine.Editor;

public class SeedsDto
{
    [JsonPropertyName("seeds")]
    public List<SeedItemDto> Seeds { get; set; } = new();
}

public class SeedItemDto
{
    public string Id { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public List<SeedObjectEntryDto> Objects { get; set; } = new();
    public List<string> Tags { get; set; } = new();
}

public class SeedObjectEntryDto
{
    public string DefinitionId { get; set; } = "";
    public double OffsetX { get; set; }
    public double OffsetY { get; set; }
    public double Rotation { get; set; }
    public string? Nombre { get; set; }
}
