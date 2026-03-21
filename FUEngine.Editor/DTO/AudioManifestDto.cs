namespace FUEngine.Editor;

/// <summary>Raíz de audio.json (modo Play / editor).</summary>
public sealed class AudioManifestDto
{
    /// <summary>Entradas por id (preferido).</summary>
    public List<AudioManifestSoundDto>? Sounds { get; set; }

    /// <summary>Alias opcional (documentación interna / compatibilidad).</summary>
    public List<AudioManifestSoundDto>? Clips { get; set; }
}

public sealed class AudioManifestSoundDto
{
    public string? Id { get; set; }
    public string? Path { get; set; }
    public float? Volume { get; set; }
    public bool? IsLoop { get; set; }
    public float? PitchVar { get; set; }
}
