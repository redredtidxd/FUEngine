namespace FUEngine.Core;

/// <summary>Fuente de sonido (SFX, música, ambiente). Stub para futuro.</summary>
public class SoundSource
{
    public string Id { get; set; } = "";
    public string? Path { get; set; }
    public float Volume { get; set; } = 1f;
    public bool Loop { get; set; }
}
