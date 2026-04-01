using System.Collections.Generic;
using System.IO;
using System.Windows.Threading;
using FUEngine.Editor;
using NAudio.Wave;

namespace FUEngine;

/// <summary>Audio en modo Play: música (NAudio + loop/fade) y SFX con voces limitadas.</summary>
public sealed class PlayNaudioAudioEngine : IDisposable
{
    private const int SfxVoiceCount = 12;
    private readonly Dispatcher _dispatcher;
    private readonly string _projectRoot;
    private IReadOnlyDictionary<string, AudioManifestSerialization.SoundEntry> _manifest =
        new Dictionary<string, AudioManifestSerialization.SoundEntry>(StringComparer.OrdinalIgnoreCase);

    private float _master = 1f;
    private float _musicBus = 0.7f;
    private float _sfxBus = 1f;

    private WaveOutEvent? _musicOut;
    private AudioFileReader? _musicReader;
    private float _musicClipVolume = 1f;
    private bool _musicLoopEnabled;
    private readonly DispatcherTimer? _fadeTimer;
    private float _fadeStartVolume;
    private float _fadeElapsed;
    private float _fadeDuration;

    private readonly WaveOutEvent?[] _sfxOut = new WaveOutEvent[SfxVoiceCount];
    private readonly AudioFileReader?[] _sfxReader = new AudioFileReader[SfxVoiceCount];
    private readonly int[] _sfxTick = new int[SfxVoiceCount];
    private int _sfxClock;

    private bool _disposed;
    private readonly object _disposeLock = new();

    public PlayNaudioAudioEngine(string projectRoot, Dispatcher dispatcher)
    {
        _projectRoot = projectRoot ?? "";
        _dispatcher = dispatcher;
        _fadeTimer = new DispatcherTimer(DispatcherPriority.Normal, _dispatcher) { Interval = TimeSpan.FromMilliseconds(40) };
        _fadeTimer.Tick += OnFadeTick;
    }

    public void LoadManifest(string manifestAbsolutePath)
    {
        ThrowIfDisposed();
        _manifest = AudioManifestSerialization.LoadOrEmpty(manifestAbsolutePath, _projectRoot);
    }

    public void SetVolumes(float master, float music, float sfx)
    {
        ThrowIfDisposed();
        _master = Math.Clamp(master, 0f, 1f);
        _musicBus = Math.Clamp(music, 0f, 1f);
        _sfxBus = Math.Clamp(sfx, 0f, 1f);
        ApplyMusicVolumeFromBuses();
    }

    public void SetMasterVolume(float v)
    {
        ThrowIfDisposed();
        _master = Math.Clamp(v, 0f, 1f);
        ApplyMusicVolumeFromBuses();
    }

    public void SetMusicBusVolume(float v)
    {
        ThrowIfDisposed();
        _musicBus = Math.Clamp(v, 0f, 1f);
        ApplyMusicVolumeFromBuses();
    }

    public void SetSfxBusVolume(float v)
    {
        ThrowIfDisposed();
        _sfxBus = Math.Clamp(v, 0f, 1f);
    }

    /// <summary>Ruta de archivo relativa al proyecto o absoluta (p. ej. música de inicio).</summary>
    public void PlayMusicFromPath(string relativeOrAbsolutePath, bool loop)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(relativeOrAbsolutePath)) return;
        var path = Path.IsPathRooted(relativeOrAbsolutePath)
            ? relativeOrAbsolutePath
            : Path.GetFullPath(Path.Combine(_projectRoot, relativeOrAbsolutePath.Trim().Replace('/', Path.DirectorySeparatorChar)));
        if (!File.Exists(path)) return;
        StopMusicInternal(immediate: true);
        StartMusicFromFile(path, clipVolume: 1f, loop);
    }

    public void PlayMusicById(string id, bool loop)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(id) || !_manifest.TryGetValue(id.Trim(), out var e)) return;
        StopMusicInternal(immediate: true);
        StartMusicFromFile(e.AbsolutePath, e.Volume, loop || e.IsLoop);
    }

    public void PlaySfxById(string id, float? volumeMultiplier = null)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(id) || !_manifest.TryGetValue(id.Trim(), out var e)) return;
        var mul = volumeMultiplier is > 0 ? volumeMultiplier.Value : 1f;
        PlaySfxFromFile(e.AbsolutePath, e.Volume * mul);
    }

    public void StopMusic(double fadeSeconds = 0)
    {
        ThrowIfDisposed();
        if (_musicOut == null) return;
        if (fadeSeconds <= 0.001)
        {
            StopMusicInternal(immediate: true);
            return;
        }

        _musicLoopEnabled = false;
        _fadeTimer?.Stop();
        _fadeStartVolume = _musicReader?.Volume ?? 1f;
        _fadeElapsed = 0;
        _fadeDuration = (float)Math.Clamp(fadeSeconds, 0.05, 120.0);
        _fadeTimer?.Start();
    }

    public void StopAll()
    {
        ThrowIfDisposed();
        StopAllInternal();
    }

    /// <summary>Detiene música y SFX sin comprobar disposed (uso desde <see cref="Dispose"/> y limpieza interna).</summary>
    private void StopAllInternal()
    {
        StopMusicInternal(immediate: true);
        for (var i = 0; i < SfxVoiceCount; i++)
            ClearSfxSlot(i);
    }

    public void Dispose()
    {
        lock (_disposeLock)
        {
            if (_disposed) return;
            if (_fadeTimer != null)
            {
                _fadeTimer.Stop();
                _fadeTimer.Tick -= OnFadeTick;
            }
            StopAllInternal();
            _disposed = true;
        }
    }

    private void StartMusicFromFile(string absolutePath, float clipVolume, bool loop)
    {
        try
        {
            _musicClipVolume = Math.Clamp(clipVolume, 0f, 2f);
            _musicReader = new AudioFileReader(absolutePath);
            _musicLoopEnabled = loop;
            ApplyMusicVolumeFromBuses();
            _musicOut = new WaveOutEvent();
            _musicOut.Init(_musicReader);
            _musicOut.PlaybackStopped += OnMusicPlaybackStopped;
            _musicOut.Play();
        }
        catch
        {
            StopMusicInternal(immediate: true);
        }
    }

    private void OnMusicPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        _dispatcher.BeginInvoke(() =>
        {
            if (_disposed) return;
            if (_musicReader == null || _musicOut == null) return;
            if (_musicLoopEnabled)
            {
                try
                {
                    _musicReader.Position = 0;
                    _musicOut.Play();
                }
                catch
                {
                    StopMusicInternal(immediate: true);
                }
            }
            else
                StopMusicInternal(immediate: true);
        });
    }

    private void ApplyMusicVolumeFromBuses()
    {
        if (_musicReader == null) return;
        _musicReader.Volume = _musicClipVolume * _master * _musicBus;
    }

    private void OnFadeTick(object? sender, EventArgs e)
    {
        if (_musicReader == null || _musicOut == null)
        {
            _fadeTimer?.Stop();
            return;
        }
        _fadeElapsed += 0.04f;
        var t = _fadeDuration > 0 ? Math.Min(1f, _fadeElapsed / _fadeDuration) : 1f;
        _musicReader.Volume = Math.Max(0f, _fadeStartVolume * (1f - t));
        if (t >= 1f - 1e-4f)
        {
            _fadeTimer?.Stop();
            StopMusicInternal(immediate: true);
        }
    }

    private void StopMusicInternal(bool immediate)
    {
        _fadeTimer?.Stop();
        _musicLoopEnabled = false;
        if (_musicOut != null)
        {
            try
            {
                _musicOut.PlaybackStopped -= OnMusicPlaybackStopped;
                _musicOut.Stop();
            }
            catch { /* ignore */ }
            _musicOut.Dispose();
            _musicOut = null;
        }
        _musicReader?.Dispose();
        _musicReader = null;
    }

    private void PlaySfxFromFile(string absolutePath, float clipVolume)
    {
        if (!File.Exists(absolutePath)) return;
        var slot = AcquireSfxSlot();
        ClearSfxSlot(slot);
        try
        {
            var reader = new AudioFileReader(absolutePath)
            {
                Volume = Math.Clamp(clipVolume, 0f, 2f) * _master * _sfxBus
            };
            var w = new WaveOutEvent();
            w.Init(reader);
            var captured = slot;
            w.PlaybackStopped += (_, _) => _dispatcher.BeginInvoke(() => ClearSfxSlot(captured));
            _sfxOut[slot] = w;
            _sfxReader[slot] = reader;
            _sfxTick[slot] = ++_sfxClock;
            w.Play();
        }
        catch
        {
            ClearSfxSlot(slot);
        }
    }

    private int AcquireSfxSlot()
    {
        for (var i = 0; i < SfxVoiceCount; i++)
        {
            var o = _sfxOut[i];
            if (o == null) return i;
            if (o.PlaybackState == PlaybackState.Stopped) return i;
        }
        var best = 0;
        var bestTick = int.MaxValue;
        for (var i = 0; i < SfxVoiceCount; i++)
        {
            if (_sfxTick[i] < bestTick)
            {
                bestTick = _sfxTick[i];
                best = i;
            }
        }
        return best;
    }

    private void ClearSfxSlot(int slot)
    {
        if ((uint)slot >= SfxVoiceCount) return;
        try
        {
            _sfxOut[slot]?.Stop();
        }
        catch { /* ignore */ }
        _sfxOut[slot]?.Dispose();
        _sfxOut[slot] = null;
        _sfxReader[slot]?.Dispose();
        _sfxReader[slot] = null;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(PlayNaudioAudioEngine));
    }
}
