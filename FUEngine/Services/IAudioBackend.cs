namespace FUEngine;

/// <summary>Backend de reproducción de audio (editor = preview con MediaPlayer, runtime = juego con OpenAL/NAudio).</summary>
public interface IAudioBackend
{
    void Play(string id, string? fullPath);
    void Stop(string id);
    void StopAll();
    void SetMasterVolume(double volume);

    /// <summary>Detiene la reproducción de preview actual (solo editor).</summary>
    void StopPreview();
}
