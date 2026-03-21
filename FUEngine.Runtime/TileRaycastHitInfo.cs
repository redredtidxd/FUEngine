namespace FUEngine.Runtime;

/// <summary>Impacto de <see cref="WorldApi.raycastTiles"/> / <see cref="WorldApi.raycastCombined"/> cuando el primer obstáculo es el tilemap.</summary>
public sealed class TileRaycastHitInfo
{
    public TileRaycastHitInfo(double tileX, double tileY, double distance, double x, double y)
    {
        this.tileX = tileX;
        this.tileY = tileY;
        this.distance = distance;
        this.x = x;
        this.y = y;
    }

    public double tileX { get; }
    public double tileY { get; }
    public double distance { get; }
    public double x { get; }
    public double y { get; }
}
