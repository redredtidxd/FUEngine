namespace FUEngine.Runtime;

/// <summary>Cámara para vista del juego (posición, zoom, seguimiento).</summary>
public class Camera
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Zoom { get; set; } = 1f;
}
