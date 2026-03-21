namespace FUEngine.Core;

/// <summary>Anclas normalizadas (0-1) para layout responsive. (minX, minY) y (maxX, maxY) definen esquinas relativas al padre.</summary>
public struct UIAnchors
{
    public double MinX { get; set; }
    public double MinY { get; set; }
    public double MaxX { get; set; }
    public double MaxY { get; set; }
}
