namespace FUEngine.Core;

/// <summary>
/// Valores por defecto y normalización para <see cref="ProjectInfo.RenderAntiAliasMode"/>,
/// <see cref="ProjectInfo.TextureFilterMode"/> y MSAA. El runtime gráfico puede leerlos cuando exista soporte.
/// </summary>
public static class ProjectRenderSettings
{
    public const string AntiAliasNone = "none";
    public const string AntiAliasFxaa = "fxaa";
    public const string AntiAliasMsaa = "msaa";

    public const string FilterNearest = "nearest";
    public const string FilterBilinear = "bilinear";

    public static string NormalizeAntiAliasMode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return AntiAliasNone;
        return value.Trim().ToLowerInvariant() switch
        {
            AntiAliasFxaa => AntiAliasFxaa,
            AntiAliasMsaa => AntiAliasMsaa,
            _ => AntiAliasNone
        };
    }

    public static string NormalizeTextureFilter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return FilterNearest;
        return value.Trim().ToLowerInvariant() switch
        {
            FilterBilinear => FilterBilinear,
            "linear" => FilterBilinear,
            "point" => FilterNearest,
            _ => FilterNearest
        };
    }

    /// <summary>0 = desactivado; 2, 4 u 8 muestras típicas de MSAA.</summary>
    public static int NormalizeMsaaSamples(int value) => value switch { 2 => 2, 4 => 4, 8 => 8, _ => 0 };
}
