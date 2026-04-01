namespace FUEngine.Core;

/// <summary>Fuente de luz (posición, radio, color, intensidad). Stub para futuro.</summary>
public class LightSource
{
    public string Id { get; set; } = System.Guid.NewGuid().ToString("N");
    public float X { get; set; }
    public float Y { get; set; }
    public float Radius { get; set; } = 5f;
    public float Intensity { get; set; } = 1f;
    public string? ColorHex { get; set; } = "#ffffff";
}
