using System.IO;
using FUEngine.Editor;

namespace FUEngine;

/// <summary>Ruta canónica del manifiesto del proyecto (Project.FUE o JSON legacy) y comprobación de selección en el explorador.</summary>
public static class ProjectManifestPaths
{
    /// <summary>Misma prioridad que <see cref="Windows.EditorWindow"/> al resolver el archivo de proyecto.</summary>
    public static string? GetCanonicalManifestPath(string? projectDirectory)
    {
        if (string.IsNullOrEmpty(projectDirectory) || !Directory.Exists(projectDirectory)) return null;
        var fue = Path.Combine(projectDirectory, NewProjectStructure.ProjectFileName);
        var proyectoJson = Path.Combine(projectDirectory, "proyecto.json");
        var projectJson = Path.Combine(projectDirectory, "Project.json");
        if (File.Exists(fue)) return fue;
        if (File.Exists(proyectoJson)) return proyectoJson;
        if (File.Exists(projectJson)) return projectJson;
        return fue;
    }

    public static bool IsActiveProjectManifestFile(string? fullPath, string? projectDirectory)
    {
        if (string.IsNullOrEmpty(fullPath) || string.IsNullOrEmpty(projectDirectory)) return false;
        var canon = GetCanonicalManifestPath(projectDirectory);
        if (string.IsNullOrEmpty(canon) || !File.Exists(canon)) return false;
        return string.Equals(Path.GetFullPath(fullPath), Path.GetFullPath(canon), StringComparison.OrdinalIgnoreCase);
    }
}
