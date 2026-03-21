using FUEngine.Runtime;

namespace FUEngine;

/// <summary>Delega la tabla <c>audio</c> de Lua en <see cref="PlayNaudioAudioEngine"/> durante Play.</summary>
public sealed class WpfPlayAudioApi : AudioApi
{
    private readonly PlayNaudioAudioEngine _engine;

    public WpfPlayAudioApi(PlayNaudioAudioEngine engine)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    public override void play(string id) => _engine.PlaySfxById(id);

    public override void play(string id, double volume)
    {
        if (volume > 0)
            _engine.PlaySfxById(id, (float)volume);
        else
            _engine.PlaySfxById(id);
    }

    public override void playMusic(string id) => _engine.PlayMusicById(id, loop: true);

    public override void playMusic(string id, bool loop) => _engine.PlayMusicById(id, loop);

    public override void playSfx(string id) => _engine.PlaySfxById(id);

    public override void stopMusic(double fadeSeconds = 0) => _engine.StopMusic(fadeSeconds);

    public override void setVolume(string bus, double value)
    {
        var v = (float)value;
        if (string.IsNullOrWhiteSpace(bus)) return;
        switch (bus.Trim().ToLowerInvariant())
        {
            case "master":
                _engine.SetMasterVolume(v);
                break;
            case "music":
                _engine.SetMusicBusVolume(v);
                break;
            case "sfx":
                _engine.SetSfxBusVolume(v);
                break;
        }
    }

    public override void stop(string id) { }

    public override void stopAll() => _engine.StopAll();

    public override void setMasterVolume(double v) => _engine.SetMasterVolume((float)v);
}
