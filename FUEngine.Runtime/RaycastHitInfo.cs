namespace FUEngine.Runtime;

/// <summary>Resultado de <see cref="WorldApi.raycast"/> para Lua: proxy del objeto, distancia y punto de impacto (casillas).</summary>
public sealed class RaycastHitInfo
{
    public RaycastHitInfo(SelfProxy? hit, double distance, double x, double y)
    {
        this.hit = hit;
        this.distance = distance;
        this.x = x;
        this.y = y;
    }

    public SelfProxy? hit { get; }
    public double distance { get; }
    public double x { get; }
    public double y { get; }
}
