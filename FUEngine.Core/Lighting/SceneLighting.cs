namespace FUEngine.Core;

/// <summary>
/// Iluminación escénica simple (2D): ambiente + <see cref="LightComponent"/> en <see cref="GameObject"/>.
/// El pipeline Vulkan sigue siendo básico; el visor WPF usa <see cref="SampleBrightness"/> para atenuar sprites/tiles.
/// </summary>
public static class SceneLighting
{
    public const float DefaultAmbientWhenLit = 0.2f;

    /// <summary>
    /// Factor 0–1 de luminosidad en coordenadas de casilla. Sin luces en escena → 1 (sin cambio visual).</summary>
    public static float SampleBrightness(IReadOnlyList<GameObject>? scene, float worldTileX, float worldTileY)
    {
        if (scene == null || scene.Count == 0) return 1f;
        bool any = false;
        foreach (var go in scene)
        {
            if (go.GetComponent<LightComponent>() != null) { any = true; break; }
        }
        if (!any) return 1f;

        float sum = DefaultAmbientWhenLit;
        foreach (var go in scene)
        {
            var L = go.GetComponent<LightComponent>();
            if (L == null) continue;
            float lx = go.Transform.X;
            float ly = go.Transform.Y;
            float dx = worldTileX - lx;
            float dy = worldTileY - ly;
            float dist = MathF.Sqrt(dx * dx + dy * dy);
            float r = Math.Max(0.25f, L.Radius);
            if (dist >= r) continue;
            float t = 1f - dist / r;
            sum += Math.Clamp(L.Intensity, 0f, 8f) * t * t;
        }
        return Math.Clamp(sum, 0.06f, 1f);
    }

    /// <summary>Multiplicadores RGB 0–1 por punto (luz ambiente + <see cref="LightComponent.ColorHex"/>). Sin luces → (1,1,1).</summary>
    public static (float R, float G, float B) SampleRgbTint(IReadOnlyList<GameObject>? scene, float worldTileX, float worldTileY)
    {
        if (scene == null || scene.Count == 0) return (1f, 1f, 1f);
        bool any = false;
        foreach (var go in scene)
        {
            if (go.GetComponent<LightComponent>() != null) { any = true; break; }
        }
        if (!any) return (1f, 1f, 1f);

        float r = 0.12f, g = 0.12f, b = 0.14f;
        foreach (var go in scene)
        {
            var L = go.GetComponent<LightComponent>();
            if (L == null) continue;
            float lx = go.Transform.X;
            float ly = go.Transform.Y;
            float dx = worldTileX - lx;
            float dy = worldTileY - ly;
            float dist = MathF.Sqrt(dx * dx + dy * dy);
            float rad = Math.Max(0.25f, L.Radius);
            if (dist >= rad) continue;
            float t = 1f - dist / rad;
            float f = Math.Clamp(L.Intensity, 0f, 8f) * t * t;
            var (lr, lg, lb) = ParseLightColor(L.ColorHex);
            r += lr * f;
            g += lg * f;
            b += lb * f;
        }
        return (Math.Clamp(r, 0.06f, 1f), Math.Clamp(g, 0.06f, 1f), Math.Clamp(b, 0.06f, 1f));
    }

    private static (float r, float g, float b) ParseLightColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return (1f, 1f, 1f);
        var s = hex.Trim();
        if (s.StartsWith('#')) s = s[1..];
        if (s.Length == 6 && uint.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out uint v))
        {
            int ri = (int)((v >> 16) & 255);
            int gi = (int)((v >> 8) & 255);
            int bi = (int)(v & 255);
            return (ri / 255f, gi / 255f, bi / 255f);
        }
        return (1f, 1f, 1f);
    }
}
