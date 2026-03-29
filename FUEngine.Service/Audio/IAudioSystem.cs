namespace FUEngine.Service.Audio;

/// <summary>
/// Fachada de alto nivel para reproducir sonidos y música.
/// Resuelve IDs de audio contra un registro de assets y delega
/// la reproducción al <see cref="IAudioBackend"/> configurado.
/// </summary>
public interface IAudioSystem
{
    void Play(string id);
    void PlayMusic(string id);
    void Stop(string id);
    void StopAll();
    void SetMasterVolume(double volume);
    void PlayPreview(string id);
    void StopPreview();
}
