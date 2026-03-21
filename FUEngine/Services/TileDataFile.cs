using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FUEngine;

/// <summary>Point for polygon vertices (pixels relative to image).</summary>
public class PointDto
{
    public float X { get; set; }
    public float Y { get; set; }
}

/// <summary>Collision shape DTO. Coordinates in pixels relative to image size.</summary>
public class CollisionShapeDto
{
    public string Type { get; set; } = "Box"; // Box | Circle | Polygon | Capsule
    public string Layer { get; set; } = "Solid"; // Solid | Trigger | OneWay
    // Box
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    // Circle
    public float CenterX { get; set; }
    public float CenterY { get; set; }
    public float Radius { get; set; }
    // Polygon
    public List<PointDto>? Points { get; set; }
    // Capsule
    public float X1 { get; set; }
    public float Y1 { get; set; }
    public float X2 { get; set; }
    public float Y2 { get; set; }
}

/// <summary>
/// JSON format for .tiledata: palette and grid size for Creative Suite tiles.
/// For animated tiles: Fps and FrameCount; the PNG is a horizontal spritesheet (frameWidth = GridSize, frameCount = FrameCount).
/// CollisionShapes: null = engine uses full AABB; empty list = no collision (phantom).
/// </summary>
public class TileDataDto
{
    public List<string>? Palette { get; set; }
    public int GridSize { get; set; } = 16;
    /// <summary>Frames per second for animated tiles. When &gt; 0 and FrameCount &gt; 1, the tile is animated.</summary>
    public int Fps { get; set; } = 8;
    /// <summary>Number of frames in the spritesheet. 1 = static tile.</summary>
    public int FrameCount { get; set; } = 1;
    /// <summary>Pivot for rotation (0-1 normalized, or pixels; editor can use either).</summary>
    public float PivotX { get; set; }
    public float PivotY { get; set; }
    /// <summary>Collision shapes in image pixels. null = full AABB; empty = no collision.</summary>
    public List<CollisionShapeDto>? CollisionShapes { get; set; }
}

public static class TileDataFile
{
    public static TileDataDto? Load(string tiledataPath)
    {
        if (string.IsNullOrEmpty(tiledataPath) || !File.Exists(tiledataPath)) return null;
        try
        {
            var json = File.ReadAllText(tiledataPath);
            return JsonSerializer.Deserialize<TileDataDto>(json);
        }
        catch
        {
            return null;
        }
    }

    public static void Save(string tiledataPath, TileDataDto dto)
    {
        if (string.IsNullOrEmpty(tiledataPath)) return;
        try
        {
            var dir = Path.GetDirectoryName(tiledataPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(tiledataPath, json);
        }
        catch { /* ignore */ }
    }

    /// <summary>Gets the .tiledata path for a given .png path.</summary>
    public static string GetTileDataPath(string pngPath)
    {
        if (string.IsNullOrEmpty(pngPath)) return "";
        return Path.ChangeExtension(pngPath, ".tiledata");
    }
}
