namespace FUEngine.Runtime;

/// <summary>Resultado de <see cref="WorldApi.raycastCombined"/>: el impacto más cercano entre tilemap y colliders sólidos.</summary>
public sealed class CombinedRaycastHitInfo
{
    public CombinedRaycastHitInfo(string kind, SelfProxy? hit, double tileX, double tileY, double distance, double x, double y)
    {
        this.kind = kind;
        this.hit = hit;
        this.tileX = tileX;
        this.tileY = tileY;
        this.distance = distance;
        this.x = x;
        this.y = y;
    }

    /// <summary>"tile" o "object".</summary>
    public string kind { get; }
    public SelfProxy? hit { get; }
    public double tileX { get; }
    public double tileY { get; }
    public double distance { get; }
    public double x { get; }
    public double y { get; }
}
