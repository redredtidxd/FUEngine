namespace FUEngine.Core;

/// <summary>Máquina de escribir opcional sobre el texto del elemento.</summary>
public sealed class UITypewriterSettings
{
    public bool Enabled { get; set; }

    public double CharsPerSecond { get; set; } = 32;

    public bool FadeInPerChar { get; set; }

    /// <summary>Duración del fade 0→1 por carácter cuando FadeInPerChar (segundos).</summary>
    public double FadeInDurationSeconds { get; set; } = 0.08;

    public bool PunctuationPausesEnabled { get; set; } = true;

    public double PauseAfterCommaSeconds { get; set; } = 0.12;

    public double PauseAfterPeriodSeconds { get; set; } = 0.28;

    public double PauseAfterQuestionSeconds { get; set; } = 0.35;

    public double PauseAfterExclamationSeconds { get; set; } = 0.35;

    /// <summary>Ruta de audio relativa al proyecto (.wav / .ogg).</summary>
    public string SoundPath { get; set; } = "";

    public UITypewriterSoundTrigger SoundTrigger { get; set; } = UITypewriterSoundTrigger.EachCharacter;

    public double SoundVolume { get; set; } = 1;

    public UITypewriterSettings Clone() => new()
    {
        Enabled = Enabled,
        CharsPerSecond = CharsPerSecond,
        FadeInPerChar = FadeInPerChar,
        FadeInDurationSeconds = FadeInDurationSeconds,
        PunctuationPausesEnabled = PunctuationPausesEnabled,
        PauseAfterCommaSeconds = PauseAfterCommaSeconds,
        PauseAfterPeriodSeconds = PauseAfterPeriodSeconds,
        PauseAfterQuestionSeconds = PauseAfterQuestionSeconds,
        PauseAfterExclamationSeconds = PauseAfterExclamationSeconds,
        SoundPath = SoundPath,
        SoundTrigger = SoundTrigger,
        SoundVolume = SoundVolume
    };
}
