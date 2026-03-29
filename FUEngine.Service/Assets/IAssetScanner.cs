namespace FUEngine.Service.Assets;

/// <summary>
/// Escanea un proyecto para detectar assets no referenciados (sprites, sonidos,
/// scripts) que podrían eliminarse para reducir el tamaño del paquete exportado.
/// </summary>
public interface IAssetScanner
{
    IReadOnlyList<UnusedAssetInfo> FindUnusedAssets(string projectDirectory);
}

public sealed record UnusedAssetInfo(
    string RelativePath,
    string Category,
    long SizeBytes);
