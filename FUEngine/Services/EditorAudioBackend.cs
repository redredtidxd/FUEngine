using System.Windows.Media;

namespace FUEngine;

/// <summary>Backend de audio para el editor: preview con MediaPlayer (WPF). No depende del runtime del juego.</summary>
public sealed class EditorAudioBackend : IAudioBackend
{
    private MediaPlayer? _player;
    private string? _currentId;

    public void Play(string id, string? fullPath)
    {
        if (_player != null)
        {
            try { _player.Stop(); } catch { /* ignore */ }
            try { _player.Close(); } catch { /* ignore */ }
            _player = null;
        }
        _currentId = null;
        if (string.IsNullOrEmpty(fullPath) || !System.IO.File.Exists(fullPath)) return;
        _player = new MediaPlayer();
        _currentId = id;
        _player.Open(new Uri(fullPath, UriKind.Absolute));
        _player.Play();
    }

    public void Stop(string id)
    {
        if (string.Equals(_currentId, id, StringComparison.OrdinalIgnoreCase))
            StopPreview();
    }

    public void StopAll() => StopPreview();

    public void SetMasterVolume(double volume)
    {
        if (_player != null)
            _player.Volume = Math.Clamp(volume, 0, 1);
    }

    public void StopPreview()
    {
        if (_player != null)
        {
            try { _player.Stop(); } catch { /* ignore */ }
            try { _player.Close(); } catch { /* ignore */ }
            _player = null;
        }
        _currentId = null;
    }
}
