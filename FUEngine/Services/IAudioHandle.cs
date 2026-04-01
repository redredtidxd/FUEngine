namespace FUEngine;

/// <summary>
/// Handle de una instancia de sonido en reproducción. Permite controlar por canal (stop, volumen).
/// Futuro: loops, spatial audio, fade.
/// </summary>
public interface IAudioHandle
{
    void Stop();
    void SetVolume(double volume);
}
