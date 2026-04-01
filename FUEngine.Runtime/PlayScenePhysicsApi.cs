using FUEngine.Core;

namespace FUEngine.Runtime;

/// <summary>
/// Tabla Lua <c>physics</c>: raycast y overlap solo contra <see cref="ColliderComponent"/> en escena (no tilemap).
/// Distinto de <see cref="WorldApi.raycast"/>, que usa la implementación del host (puede alinear objetos con otros criterios).
/// </summary>
public sealed class PlayScenePhysicsApi : PhysicsApi
{
    private readonly Func<IReadOnlyList<GameObject>> _getSceneObjects;
    private readonly Func<GameObject, SelfProxy> _toProxy;

    public PlayScenePhysicsApi(Func<IReadOnlyList<GameObject>> getSceneObjects, Func<GameObject, SelfProxy> toProxy)
    {
        _getSceneObjects = getSceneObjects ?? throw new ArgumentNullException(nameof(getSceneObjects));
        _toProxy = toProxy ?? throw new ArgumentNullException(nameof(toProxy));
    }

    public override object? raycast(double x1, double y1, double x2, double y2)
    {
        var objects = _getSceneObjects();
        double dx = x2 - x1, dy = y2 - y1;
        double len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 1e-9) return null;
        double ux = dx / len, uy = dy / len;
        if (ScenePhysicsQueries.RaycastSolids(objects, x1, y1, ux, uy, len, null, out var t, out var hitGo) && hitGo != null)
        {
            double hitX = x1 + ux * t;
            double hitY = y1 + uy * t;
            return new RaycastHitInfo(_toProxy(hitGo), t, hitX, hitY);
        }
        return null;
    }

    public override object? overlapCircle(double centerX, double centerY, double radius)
    {
        var hits = ScenePhysicsQueries.OverlapCircle(_getSceneObjects(), centerX, centerY, radius, includeTriggers: true);
        var list = new List<SelfProxy>(hits.Count);
        foreach (var go in hits)
            list.Add(_toProxy(go));
        return list;
    }
}
