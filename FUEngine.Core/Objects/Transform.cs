namespace FUEngine.Core;

/// <summary>Posición, rotación y escala de un GameObject.</summary>
public class Transform
{
    public float X { get; set; }
    public float Y { get; set; }
    public float RotationDegrees { get; set; }
    public float ScaleX { get; set; } = 1f;
    public float ScaleY { get; set; } = 1f;
}
