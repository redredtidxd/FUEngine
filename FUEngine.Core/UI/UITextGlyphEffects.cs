namespace FUEngine.Core;

/// <summary>Efectos procedurales por carácter (viewport WPF).</summary>
public sealed class UITextGlyphEffects
{
    public bool ShakeEnabled { get; set; }

    /// <summary>Desplazamiento máximo aproximado en píxeles lógicos.</summary>
    public double ShakeIntensityPixels { get; set; } = 2;

    public bool WaveEnabled { get; set; }

    public double WaveAmplitudePixels { get; set; } = 4;

    /// <summary>Frecuencia espacial (índice de carácter).</summary>
    public double WaveFrequency { get; set; } = 1.2;

    public bool RainbowEnabled { get; set; }

    /// <summary>Ciclos del arcoíris por segundo sobre el matiz.</summary>
    public double RainbowCyclesPerSecond { get; set; } = 0.25;

    public UITextGlyphEffects Clone() => new()
    {
        ShakeEnabled = ShakeEnabled,
        ShakeIntensityPixels = ShakeIntensityPixels,
        WaveEnabled = WaveEnabled,
        WaveAmplitudePixels = WaveAmplitudePixels,
        WaveFrequency = WaveFrequency,
        RainbowEnabled = RainbowEnabled,
        RainbowCyclesPerSecond = RainbowCyclesPerSecond
    };
}
