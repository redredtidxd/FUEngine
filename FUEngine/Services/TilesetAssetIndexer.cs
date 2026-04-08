using System.IO;
using System.Linq;

namespace FUEngine.Services;

/// <summary>
/// Lista rutas absolutas de definiciones de tileset ya registradas en el proyecto (no escanea PNG sueltos).
/// </summary>
public static class TilesetAssetIndexer
{
    public static IReadOnlyList<string> EnumerateRegisteredTilesets(string projectRoot)
    {
        if (string.IsNullOrWhiteSpace(projectRoot) || !Directory.Exists(projectRoot))
            return Array.Empty<string>();

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pat in new[] { "*.fuetileset", "*.tileset.json" })
        {
            try
            {
                foreach (var f in Directory.EnumerateFiles(projectRoot, pat, SearchOption.AllDirectories))
                    set.Add(f);
            }
            catch { /* ignore */ }
        }

        return set.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
