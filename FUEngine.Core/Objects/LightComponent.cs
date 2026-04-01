namespace FUEngine.Core;

/// <summary>Componente de luz en la posición del Transform (radio, intensidad, color).</summary>
public class LightComponent : Component
{
    public float Radius { get; set; } = 5f;
    public float Intensity { get; set; } = 1f;
    public string ColorHex { get; set; } = "#ffffff";
}
