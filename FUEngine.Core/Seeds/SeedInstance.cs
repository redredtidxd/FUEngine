namespace FUEngine.Core;

/// <summary>Instancia de un seed en la escena/mapa: referencia al SeedDefinition + posición.</summary>
public class SeedInstance
{
    public string SeedId { get; set; } = "";
    public float X { get; set; }
    public float Y { get; set; }
    public float Rotation { get; set; }
}
