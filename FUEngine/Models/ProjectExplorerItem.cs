using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FUEngine;

/// <summary>
/// Nodo del árbol del explorador de proyecto (carpeta o archivo).
/// </summary>
public class ProjectExplorerItem
{
    public string Name { get; set; } = "";
    public string FullPath { get; set; } = "";
    public bool IsFolder { get; set; }
    public ProjectFileType FileType { get; set; }
    /// <summary>Carácter o emoji para mostrar en el árbol.</summary>
    public string Icon { get; set; } = "📄";
    public bool IsModified { get; set; }
    public bool IsMissing { get; set; }
    public ObservableCollection<ProjectExplorerItem> Children { get; set; } = new();

    /// <summary>Tags desde ExplorerMetadataService (ej. Enemy, UI, Music).</summary>
    public List<string>? Tags { get; set; }
    /// <summary>Color hex para distinguir en el árbol (ej. #FFAA00).</summary>
    public string? Color { get; set; }
    /// <summary>Rating/prioridad 0-5.</summary>
    public int? Rating { get; set; }
    /// <summary>Metadata personalizada clave-valor.</summary>
    public Dictionary<string, string>? CustomMetadata { get; set; }
    /// <summary>Si está bloqueado no se permite renombrar/eliminar sin confirmación.</summary>
    public bool IsLocked { get; set; }
    /// <summary>Project.FUE / proyecto.json canónico del proyecto abierto (resaltado en el árbol).</summary>
    public bool IsProjectManifestFile { get; set; }
}

public enum ProjectFileType
{
    Folder,
    Project,
    Map,
    Objects,
    Scripts,
    Animations,
    Sprite,
    TileSet,
    Sound,
    /// <summary>Descriptor de escena (<c>.scene</c> en <c>Scenes/</c>).</summary>
    Scene,
    /// <summary>Prefab / seed (<c>.seed</c>).</summary>
    Seed,
    Generic
}
