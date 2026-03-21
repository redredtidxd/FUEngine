using System.Collections.Generic;

namespace FUEngine.Core;

/// <summary>Fuentes de luz desacopladas del tilemap. El visor WPF usa <see cref="SceneLighting"/> con <see cref="LightComponent"/> en escena.</summary>
public class LightingManager
{
    private readonly List<LightSource> _lights = new();

    public IReadOnlyList<LightSource> Lights => _lights;

    public void AddLight(LightSource light) => _lights.Add(light);
    public void RemoveLight(LightSource light) => _lights.Remove(light);

    /// <summary>Iluminación 0–1 en casillas (combina luces registradas + ambiente). Sin luces → 1.</summary>
    public float SampleBrightnessAt(float worldTileX, float worldTileY, float ambient = SceneLighting.DefaultAmbientWhenLit)
    {
        if (_lights.Count == 0) return 1f;
        float sum = ambient;
        foreach (var L in _lights)
        {
            float dx = worldTileX - L.X;
            float dy = worldTileY - L.Y;
            float dist = MathF.Sqrt(dx * dx + dy * dy);
            float r = Math.Max(0.25f, L.Radius);
            if (dist >= r) continue;
            float t = 1f - dist / r;
            sum += Math.Clamp(L.Intensity, 0f, 8f) * t * t;
        }
        return Math.Clamp(sum, 0.06f, 1f);
    }
}
