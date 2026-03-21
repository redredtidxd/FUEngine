using System.IO;
using System.Text.Json;

namespace FUEngine;

/// <summary>
/// Reads per-asset .metadata files for Creative Suite (Tile/Paint).
/// Convention: for "path/to/asset.png", metadata file is "path/to/asset.png.metadata" with JSON:
/// { "CreatedBy": "FUEngine", "Source": "FUEngine_Tile" } or "FUEngine_Paint".
/// </summary>
public static class CreativeSuiteMetadata
{
    public const string CreatedByFuEngine = "FUEngine";
    public const string SourceTile = "FUEngine_Tile";
    public const string SourcePaint = "FUEngine_Paint";

    /// <summary>
    /// Tries to read the .metadata file for the given asset path.
    /// </summary>
    /// <param name="assetPath">Full path to the image file (e.g. .png).</param>
    /// <param name="createdBy">CreatedBy value if found.</param>
    /// <param name="source">Source value if found (e.g. FUEngine_Tile, FUEngine_Paint).</param>
    /// <returns>True if .metadata exists and contains CreatedBy and Source.</returns>
    public static bool TryGet(string assetPath, out string? createdBy, out string? source)
    {
        createdBy = null;
        source = null;
        if (string.IsNullOrWhiteSpace(assetPath)) return false;

        var metaPath = assetPath + ".metadata";
        if (!File.Exists(metaPath)) return false;

        try
        {
            var json = File.ReadAllText(metaPath);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("CreatedBy", out var cb))
                createdBy = cb.GetString();
            if (root.TryGetProperty("Source", out var src))
                source = src.GetString();
            return !string.IsNullOrEmpty(createdBy) && !string.IsNullOrEmpty(source);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Returns true if the asset is a Creative Suite Tile (CreatedBy FUEngine, Source FUEngine_Tile).
    /// </summary>
    public static bool IsTile(string assetPath)
    {
        return TryGet(assetPath, out var createdBy, out var source)
               && string.Equals(createdBy, CreatedByFuEngine, StringComparison.OrdinalIgnoreCase)
               && string.Equals(source, SourceTile, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns true if the asset is a Creative Suite Paint (CreatedBy FUEngine, Source FUEngine_Paint).
    /// </summary>
    public static bool IsPaint(string assetPath)
    {
        return TryGet(assetPath, out var createdBy, out var source)
               && string.Equals(createdBy, CreatedByFuEngine, StringComparison.OrdinalIgnoreCase)
               && string.Equals(source, SourcePaint, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns true if the file extension is an image we support for Creative Suite.
    /// </summary>
    public static bool IsImagePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif";
    }

    /// <summary>
    /// Writes a .metadata file for the given asset path.
    /// </summary>
    public static void Write(string assetPath, string source)
    {
        if (string.IsNullOrWhiteSpace(assetPath)) return;
        var metaPath = assetPath + ".metadata";
        var dto = new CreativeSuiteMetaDto { CreatedBy = CreatedByFuEngine, Source = source };
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(metaPath, json);
    }

    private class CreativeSuiteMetaDto
    {
        public string CreatedBy { get; set; } = "";
        public string Source { get; set; } = "";
    }
}
