namespace FUEngine.Core;

/// <summary>Rayo en cuadrícula 2D (DDA) contra <see cref="TileMap.IsCollisionAt"/>; mapa infinito seguro (celdas vacías no cargadas = sin colisión).</summary>
public readonly struct TileRaycastResult
{
    public bool Hit { get; init; }
    public int TileX { get; init; }
    public int TileY { get; init; }
    /// <summary>Distancia euclídea a lo largo del rayo desde el origen hasta el impacto (mismas unidades que maxDistance).</summary>
    public double Distance { get; init; }
    public double HitX { get; init; }
    public double HitY { get; init; }
}

public static class TileMapRaycast
{
    /// <summary>
    /// (dirX, dirY) se normaliza internamente. maxDistance en unidades de mundo (casillas): el rayo no recorre más que eso.
    /// </summary>
    public static TileRaycastResult Raycast(TileMap map, double originX, double originY, double dirX, double dirY, double maxDistance)
    {
        if (map == null || maxDistance <= 0)
            return new TileRaycastResult { Hit = false };

        double len = Math.Sqrt(dirX * dirX + dirY * dirY);
        if (len < 1e-12)
            return new TileRaycastResult { Hit = false };

        double rdx = dirX / len;
        double rdy = dirY / len;

        int mx = (int)Math.Floor(originX);
        int my = (int)Math.Floor(originY);

        if (map.IsCollisionAt(mx, my))
        {
            return new TileRaycastResult
            {
                Hit = true,
                TileX = mx,
                TileY = my,
                Distance = 0,
                HitX = originX,
                HitY = originY
            };
        }

        double deltaDistX = rdx == 0 ? double.PositiveInfinity : Math.Abs(1.0 / rdx);
        double deltaDistY = rdy == 0 ? double.PositiveInfinity : Math.Abs(1.0 / rdy);

        int stepX;
        double sideDistX;
        if (rdx < 0)
        {
            stepX = -1;
            sideDistX = (originX - mx) * deltaDistX;
        }
        else
        {
            stepX = 1;
            sideDistX = (mx + 1.0 - originX) * deltaDistX;
        }

        int stepY;
        double sideDistY;
        if (rdy < 0)
        {
            stepY = -1;
            sideDistY = (originY - my) * deltaDistY;
        }
        else
        {
            stepY = 1;
            sideDistY = (my + 1.0 - originY) * deltaDistY;
        }

        int guard = 0;
        const int maxGuard = 1_000_000;

        while (guard++ < maxGuard)
        {
            double travelled;
            if (sideDistX < sideDistY)
            {
                travelled = sideDistX;
                if (travelled > maxDistance)
                    return new TileRaycastResult { Hit = false };
                sideDistX += deltaDistX;
                mx += stepX;
            }
            else
            {
                travelled = sideDistY;
                if (travelled > maxDistance)
                    return new TileRaycastResult { Hit = false };
                sideDistY += deltaDistY;
                my += stepY;
            }

            if (map.IsCollisionAt(mx, my))
            {
                double hx = originX + rdx * travelled;
                double hy = originY + rdy * travelled;
                return new TileRaycastResult
                {
                    Hit = true,
                    TileX = mx,
                    TileY = my,
                    Distance = travelled,
                    HitX = hx,
                    HitY = hy
                };
            }
        }

        return new TileRaycastResult { Hit = false };
    }
}
