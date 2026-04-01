namespace FUEngine.Core;

/// <summary>
/// Consultas de física 2D sobre <see cref="ColliderComponent"/> (sin tilemap).
/// <see cref="WorldApi.raycast"/> puede incluir lógica de host; <c>physics.*</c> usa solo esta capa de colliders.
/// </summary>
public static class ScenePhysicsQueries
{
    public static bool RaycastSolids(
        IReadOnlyList<GameObject> sceneObjects,
        double originX, double originY,
        double dirX, double dirY, double maxDistance,
        GameObject? ignoreOwner,
        out double bestT,
        out GameObject? hitGo)
    {
        bestT = double.PositiveInfinity;
        hitGo = null;
        if (maxDistance <= 0 || sceneObjects.Count == 0) return false;
        double len = Math.Sqrt(dirX * dirX + dirY * dirY);
        if (len < 1e-12) return false;
        double ux = dirX / len, uy = dirY / len;

        foreach (var go in sceneObjects)
        {
            if (go.PendingDestroy) continue;
            if (ignoreOwner != null && ReferenceEquals(go, ignoreOwner)) continue;
            var c = go.GetComponent<ColliderComponent>();
            if (c == null || c.IsTrigger || !c.BlocksMovement) continue;
            GetWorldAabb(go, c, out var cx, out var cy, out var hx, out var hy);
            double minX = cx - hx, maxX = cx + hx, minY = cy - hy, maxY = cy + hy;
            if (!RaySegmentIntersectsAabb(originX, originY, ux, uy, maxDistance, minX, minY, maxX, maxY, out double t))
                continue;
            if (t < bestT)
            {
                bestT = t;
                hitGo = go;
            }
        }

        return hitGo != null && !double.IsPositiveInfinity(bestT);
    }

    /// <summary>Colliders cuyo AABB intersecta un círculo en casillas (centro + radio).</summary>
    public static List<GameObject> OverlapCircle(IReadOnlyList<GameObject> sceneObjects, double cx, double cy, double radius, bool includeTriggers)
    {
        var list = new List<GameObject>();
        if (radius <= 0 || sceneObjects.Count == 0) return list;
        double r2 = radius * radius;
        foreach (var go in sceneObjects)
        {
            if (go.PendingDestroy) continue;
            var c = go.GetComponent<ColliderComponent>();
            if (c == null) continue;
            if (c.IsTrigger && !includeTriggers) continue;
            GetWorldAabb(go, c, out var ax, out var ay, out var hx, out var hy);
            double qx = Math.Clamp(cx, ax - hx, ax + hx);
            double qy = Math.Clamp(cy, ay - hy, ay + hy);
            double dx = cx - qx, dy = cy - qy;
            if (dx * dx + dy * dy <= r2)
                list.Add(go);
        }
        return list;
    }

    public static void GetWorldAabb(GameObject go, ColliderComponent c, out double cx, out double cy, out double hx, out double hy)
    {
        var t = go.Transform;
        cx = t.X + c.OffsetX;
        cy = t.Y + c.OffsetY;
        hx = Math.Abs(c.TileHalfWidth * t.ScaleX);
        hy = Math.Abs(c.TileHalfHeight * t.ScaleY);
        if (hx < 1e-6) hx = 1e-6;
        if (hy < 1e-6) hy = 1e-6;
    }

    public static bool RaySegmentIntersectsAabb(
        double ox, double oy, double ux, double uy, double tSeg,
        double minX, double minY, double maxX, double maxY, out double tHit)
    {
        tHit = 0;
        const double Eps = 1e-12;
        double t0 = 0;
        double t1 = tSeg;

        if (Math.Abs(ux) < Eps)
        {
            if (ox < minX || ox > maxX) return false;
        }
        else
        {
            double inv = 1.0 / ux;
            double tn = (minX - ox) * inv;
            double tf = (maxX - ox) * inv;
            if (tn > tf) (tn, tf) = (tf, tn);
            t0 = Math.Max(t0, tn);
            t1 = Math.Min(t1, tf);
            if (t0 > t1) return false;
        }

        if (Math.Abs(uy) < Eps)
        {
            if (oy < minY || oy > maxY) return false;
        }
        else
        {
            double inv = 1.0 / uy;
            double tn = (minY - oy) * inv;
            double tf = (maxY - oy) * inv;
            if (tn > tf) (tn, tf) = (tf, tn);
            t0 = Math.Max(t0, tn);
            t1 = Math.Min(t1, tf);
            if (t0 > t1) return false;
        }

        if (t1 < 0) return false;
        if (t0 > tSeg) return false;
        tHit = t0 >= 0 ? t0 : 0;
        return true;
    }
}
