using System;

namespace FUEngine.Runtime.Mathematics;

/// <summary>
/// Classic Perlin noise (2D and 3D), deterministic via seed.
/// Use for procedural tiles (terrain, clouds, marble). Returns values roughly in [-1, 1].
/// </summary>
public sealed class PerlinNoise
{
    private readonly int[] _perm = new int[512];

    public PerlinNoise(int seed = 0)
    {
        var p = new int[256];
        var rng = new Random(seed);
        for (int i = 0; i < 256; i++)
            p[i] = i;
        for (int i = 255; i >= 1; i--)
        {
            int j = rng.Next(i + 1);
            (p[i], p[j]) = (p[j], p[i]);
        }
        for (int i = 0; i < 512; i++)
            _perm[i] = p[i & 255];
    }

    private static double Fade(double t) => t * t * t * (t * (t * 6 - 15) + 10);
    private static double Lerp(double a, double b, double t) => a + t * (b - a);
    private static double Grad(int hash, double x, double y, double z)
    {
        int h = hash & 15;
        double u = h < 8 ? x : y;
        double v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }

    /// <summary>2D Perlin noise. Returns value approximately in [-1, 1].</summary>
    public double Noise(double x, double y) => Noise(x, y, 0);

    /// <summary>3D Perlin noise. Returns value approximately in [-1, 1].</summary>
    public double Noise(double x, double y, double z)
    {
        int X = (int)Math.Floor(x) & 255;
        int Y = (int)Math.Floor(y) & 255;
        int Z = (int)Math.Floor(z) & 255;
        x -= Math.Floor(x);
        y -= Math.Floor(y);
        z -= Math.Floor(z);
        double u = Fade(x);
        double v = Fade(y);
        double w = Fade(z);
        int A = _perm[X] + Y;
        int AA = _perm[A] + Z;
        int AB = _perm[A + 1] + Z;
        int B = _perm[X + 1] + Y;
        int BA = _perm[B] + Z;
        int BB = _perm[B + 1] + Z;
        return Lerp(
            Lerp(
                Lerp(Grad(_perm[AA], x, y, z), Grad(_perm[BA], x - 1, y, z), u),
                Lerp(Grad(_perm[AB], x, y - 1, z), Grad(_perm[BB], x - 1, y - 1, z), u),
                v),
            Lerp(
                Lerp(Grad(_perm[AA + 1], x, y, z - 1), Grad(_perm[BA + 1], x - 1, y, z - 1), u),
                Lerp(Grad(_perm[AB + 1], x, y - 1, z - 1), Grad(_perm[BB + 1], x - 1, y - 1, z - 1), u),
                v),
            w);
    }
}
