namespace FUEngine.Core;

/// <summary>Asset de textura (PNG, sprite). Evita duplicados por path.</summary>
public class TextureAsset
{
    public string Id { get; set; } = "";
    public string Path { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
}
