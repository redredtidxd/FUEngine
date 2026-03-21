using System.IO;
using System.Text.Json;
using FUEngine.Core;
using FUEngine.Editor;

namespace FUEngine;

/// <summary>Escribe <c>proyecto.json</c> para builds exportados (escena inicial elegida).</summary>
public static class ProjectExportHelper
{
    /// <summary>
    /// Serializa el proyecto y ajusta MainMapPath / MainObjectsPath a la escena indicada.
    /// <paramref name="sceneIndex"/> &lt; 0 = no cambiar rutas principales.
    /// </summary>
    public static void WriteExportProjectJson(ProjectInfo source, string destinationJsonPath, int sceneIndex)
    {
        var temp = Path.Combine(Path.GetTempPath(), "fue_export_" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            ProjectSerialization.Save(source, temp);
            var json = File.ReadAllText(temp);
            var dto = JsonSerializer.Deserialize<ProjectDto>(json, SerializationDefaults.Options);
            if (dto == null)
                throw new InvalidOperationException("No se pudo serializar el proyecto para exportación.");

            if (sceneIndex >= 0 && source.Scenes != null && sceneIndex < source.Scenes.Count)
            {
                var s = source.Scenes[sceneIndex];
                dto.MainMapPath = string.IsNullOrWhiteSpace(s.MapPathRelative) ? "mapa.json" : s.MapPathRelative.Trim();
                dto.MainObjectsPath = string.IsNullOrWhiteSpace(s.ObjectsPathRelative) ? "objetos.json" : s.ObjectsPathRelative.Trim();
            }

            var outJson = JsonSerializer.Serialize(dto, SerializationDefaults.Options);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationJsonPath) ?? ".");
            File.WriteAllText(destinationJsonPath, outJson);
        }
        finally
        {
            try { File.Delete(temp); } catch { /* ignore */ }
        }
    }
}
