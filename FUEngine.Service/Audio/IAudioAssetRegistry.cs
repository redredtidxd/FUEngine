namespace FUEngine.Service.Audio;

/// <summary>
/// Registro de assets de audio: resuelve un identificador lógico (ej. "explosion_01")
/// a la ruta completa del archivo en disco. Separado del backend de reproducción para
/// que la gestión del catálogo no dependa de la implementación de sonido.
/// </summary>
public interface IAudioAssetRegistry
{
    bool TryGetPath(string id, out string? path);
    void Register(string id, string fullPath);
    void Unregister(string id);
    void Clear();
}
