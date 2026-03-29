namespace FUEngine.Service;

/// <summary>
/// Integración con Discord Rich Presence: muestra en el perfil del usuario
/// qué proyecto está editando y desde cuándo. Optional — si Discord no está
/// instalado o la conexión falla, el servicio no bloquea el arranque.
/// </summary>
public interface IDiscordPresenceService : IDisposable
{
    void SetProject(string projectName);
    void Clear();
    bool IsConnected { get; }
}
