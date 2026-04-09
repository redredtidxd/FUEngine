using System;

namespace FUEngine.Core;

/// <summary>Hit-test de <see cref="ClickInteractableComponent"/> en coordenadas mundo (casillas).</summary>
public static class ClickInteractableHitTesting
{
    /// <summary>
    /// Comprueba si <paramref name="worldX"/>/<paramref name="worldY"/> cae dentro del área.
    /// El centro base es <c>transform + offset</c>; se aplica rotación del objeto.
    /// </summary>
    public static bool ContainsWorldPoint(GameObject go, ClickInteractableComponent c, double worldX, double worldY)
    {
        if (c == null || go?.Transform == null) return false;
        if (!c.InteractEnabled || !go.RuntimeActive || go.PendingDestroy) return false;
        if (c.Shape == ClickInteractableShapeKind.PixelPerfect)
            return false;

        double cx = go.Transform.X + c.OffsetXTiles;
        double cy = go.Transform.Y + c.OffsetYTiles;
        double dx = worldX - cx;
        double dy = worldY - cy;
        double rad = -go.Transform.RotationDegrees * (Math.PI / 180.0);
        double cos = Math.Cos(rad);
        double sin = Math.Sin(rad);
        double lx = dx * cos - dy * sin;
        double ly = dx * sin + dy * cos;

        double sx = Math.Abs(go.Transform.ScaleX) > 1e-6 ? go.Transform.ScaleX : 1f;
        double sy = Math.Abs(go.Transform.ScaleY) > 1e-6 ? go.Transform.ScaleY : 1f;

        if (c.Shape == ClickInteractableShapeKind.Circle)
        {
            double r = c.CircleRadiusTiles > 0 ? c.CircleRadiusTiles : 0.5f;
            r *= Math.Max(Math.Abs(sx), Math.Abs(sy));
            return lx * lx + ly * ly <= r * r;
        }

        double hw = Math.Max(1e-4, c.BoxWidthTiles * Math.Abs(sx) * 0.5);
        double hh = Math.Max(1e-4, c.BoxHeightTiles * Math.Abs(sy) * 0.5);
        return lx >= -hw && lx <= hw && ly >= -hh && ly <= hh;
    }
}
