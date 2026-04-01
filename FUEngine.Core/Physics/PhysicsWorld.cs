namespace FUEngine.Core;

/// <summary>
/// Único paso de física AABB en Play: resolución contra tilemap, entre colliders, y pares trigger (callbacks).
/// No mantiene un segundo modelo (<see cref="CollisionBody"/>); opera solo sobre <see cref="ColliderComponent"/> en escena.
/// </summary>
public sealed class PhysicsWorld
{
    /// <summary>
    /// Un tick: empujes vs tiles, vs estáticos/dinámicos, y enter/salida de triggers.
    /// </summary>
    public void StepPlayScene(
        IReadOnlyList<GameObject> sceneObjects,
        TileMap? tileMap,
        Func<GameObject, string> getStableId,
        HashSet<(string triggerId, string otherId)> triggerPairsLastFrame,
        Action<GameObject, GameObject> onTriggerEnter,
        Action<GameObject, GameObject> onTriggerExit)
    {
        var withCol = new List<GameObject>(sceneObjects.Count);
        foreach (var go in sceneObjects)
        {
            if (go.PendingDestroy) continue;
            if (go.GetComponent<ColliderComponent>() != null)
                withCol.Add(go);
        }

        if (tileMap != null)
        {
            for (int pass = 0; pass < 3; pass++)
            {
                foreach (var go in withCol)
                {
                    var d = go.GetComponent<ColliderComponent>();
                    if (d == null || d.IsTrigger || !d.BlocksMovement || d.IsStatic) continue;
                    ResolveDynamicAgainstTiles(go, d, tileMap);
                }
            }
        }

        for (int pass = 0; pass < 4; pass++)
        {
            foreach (var go in withCol)
            {
                var d = go.GetComponent<ColliderComponent>();
                if (d == null || d.IsTrigger || !d.BlocksMovement || d.IsStatic) continue;

                foreach (var other in withCol)
                {
                    if (ReferenceEquals(other, go)) continue;
                    var s = other.GetComponent<ColliderComponent>();
                    if (s == null || s.IsTrigger || !s.BlocksMovement || !s.IsStatic) continue;
                    if (!OverlapsAabb(go, d, other, s)) continue;
                    ResolveDynamicVsStatic(go, d, other, s);
                }
            }
        }

        for (int pass = 0; pass < 4; pass++)
        {
            for (int i = 0; i < withCol.Count; i++)
            {
                var goA = withCol[i];
                var a = goA.GetComponent<ColliderComponent>();
                if (a == null || a.IsTrigger || !a.BlocksMovement || a.IsStatic) continue;
                for (int j = i + 1; j < withCol.Count; j++)
                {
                    var goB = withCol[j];
                    var b = goB.GetComponent<ColliderComponent>();
                    if (b == null || b.IsTrigger || !b.BlocksMovement || b.IsStatic) continue;
                    if (!OverlapsAabb(goA, a, goB, b)) continue;
                    ResolveDynamicVsDynamic(goA, a, goB, b);
                }
            }
        }

        var thisFrame = new HashSet<(string triggerId, string otherId)>();
        for (int i = 0; i < withCol.Count; i++)
        {
            var goA = withCol[i];
            var ca = goA.GetComponent<ColliderComponent>();
            if (ca == null) continue;
            var idA = getStableId(goA);
            if (string.IsNullOrEmpty(idA)) idA = goA.Name;

            for (int j = i + 1; j < withCol.Count; j++)
            {
                var goB = withCol[j];
                var cb = goB.GetComponent<ColliderComponent>();
                if (cb == null) continue;
                if (!OverlapsAabb(goA, ca, goB, cb)) continue;
                var idB = getStableId(goB);
                if (string.IsNullOrEmpty(idB)) idB = goB.Name;

                if (ca.IsTrigger)
                    thisFrame.Add((idA, idB));
                if (cb.IsTrigger)
                    thisFrame.Add((idB, idA));
            }
        }

        foreach (var p in thisFrame)
        {
            if (triggerPairsLastFrame.Contains(p)) continue;
            var tgo = FindGoByStableId(sceneObjects, getStableId, p.triggerId);
            var ogo = FindGoByStableId(sceneObjects, getStableId, p.otherId);
            if (tgo == null || ogo == null) continue;
            onTriggerEnter(tgo, ogo);
        }

        foreach (var p in triggerPairsLastFrame)
        {
            if (thisFrame.Contains(p)) continue;
            var tgo = FindGoByStableId(sceneObjects, getStableId, p.triggerId);
            var ogo = FindGoByStableId(sceneObjects, getStableId, p.otherId);
            if (tgo == null || ogo == null) continue;
            onTriggerExit(tgo, ogo);
        }

        triggerPairsLastFrame.Clear();
        foreach (var p in thisFrame)
            triggerPairsLastFrame.Add(p);
    }

    private static GameObject? FindGoByStableId(IReadOnlyList<GameObject> scene, Func<GameObject, string> getStableId, string id)
    {
        foreach (var go in scene)
        {
            if (go.PendingDestroy) continue;
            var sid = getStableId(go);
            if (string.IsNullOrEmpty(sid)) sid = go.Name;
            if (string.Equals(sid, id, StringComparison.Ordinal))
                return go;
        }
        return null;
    }

    private static bool OverlapsAabb(GameObject goA, ColliderComponent a, GameObject goB, ColliderComponent b)
    {
        ScenePhysicsQueries.GetWorldAabb(goA, a, out var acx, out var acy, out var ahx, out var ahy);
        ScenePhysicsQueries.GetWorldAabb(goB, b, out var bcx, out var bcy, out var bhx, out var bhy);
        return acx + ahx > bcx - bhx && acx - ahx < bcx + bhx &&
               acy + ahy > bcy - bhy && acy - ahy < bcy + bhy;
    }

    private static float EffectiveMass(ColliderComponent c) => Math.Max(0.01f, c.Mass);

    private static void ResolveDynamicVsStatic(GameObject dyn, ColliderComponent d, GameObject st, ColliderComponent s)
    {
        ScenePhysicsQueries.GetWorldAabb(dyn, d, out var dcx, out var dcy, out var dhx, out var dhy);
        ScenePhysicsQueries.GetWorldAabb(st, s, out var scx, out var scy, out var shx, out var shy);

        double dMinX = dcx - dhx, dMaxX = dcx + dhx, dMinY = dcy - dhy, dMaxY = dcy + dhy;
        double sMinX = scx - shx, sMaxX = scx + shx, sMinY = scy - shy, sMaxY = scy + shy;

        double overlapX = Math.Min(dMaxX, sMaxX) - Math.Max(dMinX, sMinX);
        double overlapY = Math.Min(dMaxY, sMaxY) - Math.Max(dMinY, sMinY);
        if (overlapX <= 0 || overlapY <= 0) return;

        if (overlapX < overlapY)
        {
            float push = (float)overlapX;
            if (dcx < scx)
                dyn.Transform.X -= push;
            else
                dyn.Transform.X += push;
        }
        else
        {
            float push = (float)overlapY;
            if (dcy < scy)
                dyn.Transform.Y -= push;
            else
                dyn.Transform.Y += push;
        }
    }

    private static void ResolveDynamicVsDynamic(GameObject goA, ColliderComponent a, GameObject goB, ColliderComponent b)
    {
        ScenePhysicsQueries.GetWorldAabb(goA, a, out var acx, out var acy, out var ahx, out var ahy);
        ScenePhysicsQueries.GetWorldAabb(goB, b, out var bcx, out var bcy, out var bhx, out var bhy);

        double aMinX = acx - ahx, aMaxX = acx + ahx, aMinY = acy - ahy, aMaxY = acy + ahy;
        double bMinX = bcx - bhx, bMaxX = bcx + bhx, bMinY = bcy - bhy, bMaxY = bcy + bhy;

        double overlapX = Math.Min(aMaxX, bMaxX) - Math.Max(aMinX, bMinX);
        double overlapY = Math.Min(aMaxY, bMaxY) - Math.Max(aMinY, bMinY);
        if (overlapX <= 0 || overlapY <= 0) return;

        float mA = EffectiveMass(a);
        float mB = EffectiveMass(b);
        float invSum = 1f / (mA + mB);

        if (overlapX < overlapY)
        {
            float total = (float)overlapX;
            float shareA = total * (mB * invSum);
            float shareB = total * (mA * invSum);
            if (acx < bcx)
            {
                goA.Transform.X -= shareA;
                goB.Transform.X += shareB;
            }
            else
            {
                goA.Transform.X += shareA;
                goB.Transform.X -= shareB;
            }
        }
        else
        {
            float total = (float)overlapY;
            float shareA = total * (mB * invSum);
            float shareB = total * (mA * invSum);
            if (acy < bcy)
            {
                goA.Transform.Y -= shareA;
                goB.Transform.Y += shareB;
            }
            else
            {
                goA.Transform.Y += shareA;
                goB.Transform.Y -= shareB;
            }
        }
    }

    /// <summary>Empuja un dinámico fuera de las celdas con colisión del mapa (varias pasadas recomendadas en el caller).</summary>
    public static void ResolveDynamicAgainstTiles(GameObject dyn, ColliderComponent d, TileMap map)
    {
        ScenePhysicsQueries.GetWorldAabb(dyn, d, out var cx, out var cy, out var hx, out var hy);
        double minX = cx - hx, maxX = cx + hx, minY = cy - hy, maxY = cy + hy;
        int minTx = (int)Math.Floor(minX);
        int maxTx = (int)Math.Floor(maxX - 1e-6);
        int minTy = (int)Math.Floor(minY);
        int maxTy = (int)Math.Floor(maxY - 1e-6);
        for (int ty = minTy; ty <= maxTy; ty++)
        for (int tx = minTx; tx <= maxTx; tx++)
        {
            if (!map.IsCollisionAt(tx, ty)) continue;
            ResolveDynamicVsAxisAlignedBlock(dyn, d, tx + 0.5, ty + 0.5, 0.5, 0.5);
        }
    }

    private static void ResolveDynamicVsAxisAlignedBlock(GameObject dyn, ColliderComponent d, double scx, double scy, double shx, double shy)
    {
        ScenePhysicsQueries.GetWorldAabb(dyn, d, out var dcx, out var dcy, out var dhx, out var dhy);
        double dMinX = dcx - dhx, dMaxX = dcx + dhx, dMinY = dcy - dhy, dMaxY = dcy + dhy;
        double sMinX = scx - shx, sMaxX = scx + shx, sMinY = scy - shy, sMaxY = scy + shy;
        double overlapX = Math.Min(dMaxX, sMaxX) - Math.Max(dMinX, sMinX);
        double overlapY = Math.Min(dMaxY, sMaxY) - Math.Max(dMinY, sMinY);
        if (overlapX <= 0 || overlapY <= 0) return;
        if (overlapX < overlapY)
        {
            float push = (float)overlapX;
            if (dcx < scx) dyn.Transform.X -= push;
            else dyn.Transform.X += push;
        }
        else
        {
            float push = (float)overlapY;
            if (dcy < scy) dyn.Transform.Y -= push;
            else dyn.Transform.Y += push;
        }
    }
}
