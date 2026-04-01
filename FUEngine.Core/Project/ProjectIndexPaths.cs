namespace FUEngine.Core;

/// <summary>
/// Rutas de archivos de índice (scripts, seeds, animaciones, triggers). En proyectos nuevos viven bajo <c>Data/</c>;
/// en proyectos antiguos siguen en la raíz; la resolución prioriza el archivo que exista.
/// </summary>
public static class ProjectIndexPaths
{
    public const string DataFolderName = "Data";

    /// <summary>Prioridad: <c>Data/nombre</c> si ese archivo existe; si no, raíz/nombre; si ninguno existe y existe carpeta Data, destino por defecto Data.</summary>
    public static string Resolve(string? projectDirectory, string fileName)
    {
        if (string.IsNullOrEmpty(projectDirectory)) return fileName;
        var root = Path.Combine(projectDirectory, fileName);
        var data = Path.Combine(projectDirectory, DataFolderName, fileName);
        if (File.Exists(data)) return data;
        if (File.Exists(root)) return root;
        if (Directory.Exists(Path.Combine(projectDirectory, DataFolderName)))
            return data;
        return root;
    }

    public static string ResolveScriptsJson(string? projectDirectory) => Resolve(projectDirectory, "scripts.json");

    public static string ResolveSeedsJson(string? projectDirectory) => Resolve(projectDirectory, "seeds.json");

    public static string ResolveAnimacionesJson(string? projectDirectory) => Resolve(projectDirectory, "animaciones.json");

    public static string ResolveTriggerZonesJson(string? projectDirectory) => Resolve(projectDirectory, "triggerZones.json");
}
