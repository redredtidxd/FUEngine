using System.Speech.Synthesis;

namespace FUEngine;

/// <summary>TTS opcional (Windows) para modo accesible en Play.</summary>
public sealed class UiAccessibilityTts : IDisposable
{
    private SpeechSynthesizer? _synth;
    private readonly object _lock = new();

    public bool IsEnabled { get; set; }

    public void EnsureSynthesizer()
    {
        if (!IsEnabled) return;
        lock (_lock)
        {
            _synth ??= new SpeechSynthesizer();
        }
    }

    public void Speak(string? text, bool interruptPrevious = true)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(text)) return;
        try
        {
            lock (_lock)
            {
                EnsureSynthesizer();
                if (_synth == null) return;
                if (interruptPrevious)
                    _synth.SpeakAsyncCancelAll();
                _synth.SpeakAsync(text.Trim());
            }
        }
        catch
        {
            /* Sin voz instalada o permiso denegado */
        }
    }

    public void Stop()
    {
        try
        {
            lock (_lock)
            {
                _synth?.SpeakAsyncCancelAll();
            }
        }
        catch { /* ignore */ }
    }

    public void Dispose()
    {
        try
        {
            lock (_lock)
            {
                _synth?.Dispose();
                _synth = null;
            }
        }
        catch { /* ignore */ }
    }
}
