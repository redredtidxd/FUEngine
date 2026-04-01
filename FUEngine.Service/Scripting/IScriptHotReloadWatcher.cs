namespace FUEngine.Service.Scripting;

/// <summary>
/// Vigila cambios en archivos .lua del proyecto y notifica al sistema de Play
/// para recargar scripts en caliente (hot reload) sin detener el juego.
/// </summary>
public interface IScriptHotReloadWatcher : IDisposable
{
    void Start();
    void Stop();
    bool IsWatching { get; }
}
