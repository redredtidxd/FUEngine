namespace FUEngine.Core;

/// <summary>
/// Definición de una animación: secuencia de frames + FPS.
/// </summary>
public class AnimationDefinition
{
    public string Id { get; set; } = "";
    public string Nombre { get; set; } = "";
    /// <summary>
    /// Rutas de imagen o índices de sprite por frame.
    /// </summary>
    public IReadOnlyList<string> Frames { get; set; } = Array.Empty<string>();
    /// <summary>
    /// Frames por segundo (o duración por frame en ms si se prefiere).
    /// </summary>
    public int Fps { get; set; } = 8;
}
