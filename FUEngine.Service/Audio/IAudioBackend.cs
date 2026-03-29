namespace FUEngine.Service.Audio;

/// <summary>
/// Backend de reproducción de audio. La implementación concreta varía según
/// el contexto: preview en el editor (WPF/MediaPlayer) o juego en ejecución
/// (NAudio, OpenAL, etc.). <see cref="IAudioSystem"/> lo consume sin conocer
/// la plataforma subyacente.
/// </summary>
public interface IAudioBackend
{
    void Play(string id, string? fullPath);
    void Stop(string id);
    void StopAll();
    void SetMasterVolume(double volume);
    void StopPreview();
}
