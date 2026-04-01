namespace FUEngine.Core;

/// <summary>
/// Definición de una escena del proyecto. La escena es una entidad de proyecto:
/// contiene mapa, objetos, y layout de tabs por defecto (Mapa, Consola, Scripts, etc.).
/// </summary>
public class SceneDefinition
{
    /// <summary>Identificador único de la escena (ej: "Start", "End").</summary>
    public string Id { get; set; } = "";

    /// <summary>Nombre para mostrar en la barra de escenas.</summary>
    public string Name { get; set; } = "";

    /// <summary>Ruta relativa al proyecto del mapa (ej: "Maps/Start/map.map").</summary>
    public string MapPathRelative { get; set; } = "";

    /// <summary>Ruta relativa al proyecto de los objetos (ej: "Objects/Start/objects.objects").</summary>
    public string ObjectsPathRelative { get; set; } = "";

    /// <summary>Carpeta relativa al proyecto donde se guardan los JSON de UI por canvas (ej: "UI" o "Scenes/Start/UI").</summary>
    public string UIFolderRelative { get; set; } = "UI";

    /// <summary>Tabs opcionales por defecto al abrir la escena (además de Mapa y Consola). Ej: ["Scripts"], ["Explorador"].</summary>
    public List<string> DefaultTabKinds { get; set; } = new();

    /// <summary>Ruta absoluta del mapa (ProjectDirectory + MapPathRelative).</summary>
    public string GetMapPath(string projectDirectory) =>
        Path.Combine(projectDirectory ?? "", MapPathRelative ?? "");

    /// <summary>Ruta absoluta de objetos (ProjectDirectory + ObjectsPathRelative).</summary>
    public string GetObjectsPath(string projectDirectory) =>
        Path.Combine(projectDirectory ?? "", ObjectsPathRelative ?? "");

    /// <summary>Ruta absoluta de la carpeta UI de la escena (ProjectDirectory + UIFolderRelative).</summary>
    public string GetUIFolder(string projectDirectory) =>
        Path.Combine(projectDirectory ?? "", UIFolderRelative ?? "UI");
}
