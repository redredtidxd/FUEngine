namespace FUEngine.Core.Graphics;

/// <summary>
/// Abstracción del dispositivo gráfico. La implementación por defecto del motor es Vulkan.
/// </summary>
public interface IGraphicsDevice : IDisposable
{
    /// <summary>Indica si el dispositivo está inicializado y listo para renderizar.</summary>
    bool IsValid { get; }

    /// <summary>Ancho actual del framebuffer (0 si no hay swapchain).</summary>
    int Width { get; }

    /// <summary>Alto actual del framebuffer (0 si no hay swapchain).</summary>
    int Height { get; }

    /// <summary>Color de limpieza (R,G,B,A en [0,1]).</summary>
    void SetClearColor(float r, float g, float b, float a = 1f);

    /// <summary>Inicio de frame: adquiere imagen del swapchain (si aplica) y prepara comandos.</summary>
    void BeginFrame();

    /// <summary>Fin de frame: envía comandos y presenta (si aplica).</summary>
    void EndFrame();

    /// <summary>Limpia el framebuffer actual con el color de clear.</summary>
    void Clear();
}
