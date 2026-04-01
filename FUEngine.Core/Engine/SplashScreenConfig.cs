namespace FUEngine.Core;

/// <summary>
/// Configuración del splash screen (sin dependencias de UI).
/// Usado por el motor y por proyectos compilados.
/// </summary>
public class SplashScreenConfig
{
    /// <summary>Configuración por defecto del motor (logo, duración, fades).</summary>
    public static SplashScreenConfig Default { get; } = new SplashScreenConfig();

    public string LogoPath { get; set; } = "assets/logo_fuengine.png";
    public int DurationMs { get; set; } = 2500;
    public bool FadeIn { get; set; } = true;
    public bool FadeOut { get; set; } = true;
    public int FadeInMs { get; set; } = 500;
    public int FadeOutMs { get; set; } = 500;
    /// <summary>Ruta a sprite sheet para animación frame a frame (opcional).</summary>
    public string? SpriteSheetPath { get; set; }
    /// <summary>Intervalo en ms entre frames (ej. 100 para estilo 8-16 bits).</summary>
    public int FrameIntervalMs { get; set; } = 100;
    /// <summary>Ruta a sonido/jingle opcional al mostrar.</summary>
    public string? SoundPath { get; set; }
}
