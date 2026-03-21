namespace FUEngine.Core;

/// <summary>Asset de sonido (WAV, OGG). Evita duplicados por path.</summary>
public class SoundAsset
{
    public string Id { get; set; } = "";
    public string Path { get; set; } = "";
    public float DurationSeconds { get; set; }
}
