using System.Collections.Generic;
using System.IO;
using FUEngine.Core;

namespace FUEngine;

/// <summary>
/// Recoge las rutas de assets referenciados por la escena actual (mapa + objetos) para el filtro "Usados en escena".
/// Si hay muchos assets, conviene cachear el resultado por proyecto y actualizar solo al guardar escena o al cambiar de filtro (p. ej. en EditorWindow).
/// </summary>
public static class SceneAssetReferenceCollector
{
    public static HashSet<string> Collect(
        string projectDirectory,
        string mapPath,
        string objectsPath,
        ObjectLayer objectLayer)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(projectDirectory)) return set;

        var projectDir = projectDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (!string.IsNullOrEmpty(mapPath))
        {
            var fullMap = Path.IsPathRooted(mapPath) ? mapPath : Path.Combine(projectDir, mapPath);
            if (File.Exists(fullMap)) set.Add(fullMap);
        }

        if (!string.IsNullOrEmpty(objectsPath))
        {
            var fullObj = Path.IsPathRooted(objectsPath) ? objectsPath : Path.Combine(projectDir, objectsPath);
            if (File.Exists(fullObj)) set.Add(fullObj);
        }

        var scriptsPath = Path.Combine(projectDir, "scripts.json");
        if (File.Exists(scriptsPath)) set.Add(scriptsPath);

        var animPath = Path.Combine(projectDir, "animaciones.json");
        if (File.Exists(animPath)) set.Add(animPath);

        if (objectLayer?.Definitions != null)
        {
            foreach (var def in objectLayer.Definitions.Values)
            {
                if (!string.IsNullOrWhiteSpace(def.SpritePath))
                {
                    var fullSprite = Path.IsPathRooted(def.SpritePath) ? def.SpritePath : Path.Combine(projectDir, def.SpritePath);
                    set.Add(fullSprite);
                }
            }
        }

        return set;
    }
}
