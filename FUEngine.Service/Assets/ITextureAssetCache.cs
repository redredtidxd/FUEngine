namespace FUEngine.Service.Assets;

/// <summary>
/// Caché de metadatos de texturas (dimensiones, existencia en disco). Evita
/// accesos repetidos al sistema de archivos al resolver sprites, animaciones
/// y thumbnails durante la edición y el runtime.
/// </summary>
public interface ITextureAssetCache
{
    bool TryGetDimensions(string relativePath, out int width, out int height);
    void Invalidate(string relativePath);
    void Clear();
}
