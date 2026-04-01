namespace FUEngine;

/// <summary>
/// Sistema de audio: usa un backend (editor o runtime) y el registro para resolver ID → path.
/// No mezcla assets con reproducción: el registro es externo.
/// </summary>
public sealed class AudioSystem
{
    private readonly IAudioBackend _backend;
    private readonly AudioAssetRegistry _registry;
    private double _masterVolume = 1.0;

    public AudioSystem(IAudioBackend backend, AudioAssetRegistry registry)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public void Play(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        _registry.TryGetPath(id, out var path);
        _backend.Play(id, path);
    }

    public void PlayMusic(string id) => Play(id);

    public void Stop(string id) => _backend.Stop(id);

    public void StopAll() => _backend.StopAll();

    public void SetMasterVolume(double volume)
    {
        _masterVolume = Math.Clamp(volume, 0, 1);
        _backend.SetMasterVolume(_masterVolume);
    }

    /// <summary>Preview en el editor (mismo backend que Play pero solo para UI del tab). Siempre para el anterior antes de reproducir.</summary>
    public void PlayPreview(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        _backend.StopPreview();
        _registry.TryGetPath(id, out var path);
        _backend.Play(id, path);
    }

    public void StopPreview() => _backend.StopPreview();
}
