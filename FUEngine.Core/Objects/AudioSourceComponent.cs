namespace FUEngine.Core;

/// <summary>Metadatos de audio espacial (reproducción vía API Lua <c>audio</c> / motor).</summary>
public sealed class AudioSourceComponent : Component
{
    public string? AudioClipId { get; set; }
    public float Volume { get; set; } = 1f;
    public float Pitch { get; set; } = 1f;
    public bool Loop { get; set; }
    /// <summary>1 = atenuación por distancia (simplificada en motor si se implementa).</summary>
    public float SpatialBlend { get; set; } = 1f;
}
