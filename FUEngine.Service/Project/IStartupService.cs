namespace FUEngine.Service.Project;

/// <summary>
/// Gestiona la lista de proyectos recientes, su persistencia en AppData,
/// la detección automática de proyectos en carpetas y las estadísticas del Hub.
///
/// Nota: la implementación actual (<c>StartupService</c> en FUEngine) trabaja con
/// <c>RecentProjectInfo</c>, un modelo WPF con binding. Al migrar a esta capa,
/// el servicio debería trabajar con un DTO puro (<see cref="RecentProjectEntry"/>)
/// y la capa de presentación haría la conversión a su modelo con <c>INotifyPropertyChanged</c>.
/// </summary>
public interface IStartupService
{
    IReadOnlyList<RecentProjectEntry> LoadRecentProjects();
    RecentProjectEntry? LoadMostRecent();
    void AddRecentProject(string projectPath, string name, string? description, string? engineVersion = null);
    void RemoveFromRecent(string projectPath);
    void TogglePin(string projectPath);
    void SetTags(string projectPath, List<string> tags);
    void SetProjectType(string projectPath, string? projectType);
    (int Total, int OpenedToday, int Last7Days) GetStats();
    IReadOnlyList<RecentProjectEntry> DiscoverProjects(string directory, int maxDepth = 2);
}

/// <summary>
/// DTO puro para un proyecto reciente. Sin dependencias de WPF ni de presentación.
/// La capa de UI lo proyecta a su propio ViewModel con <c>INotifyPropertyChanged</c>.
/// </summary>
public sealed class RecentProjectEntry
{
    public string Path { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateTime LastOpened { get; set; }
    public string? OpenedWithEngineVersion { get; set; }
    public bool IsPinned { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? ProjectType { get; set; }
    public string? Resolution { get; set; }
    public int? Fps { get; set; }
    public int SceneCount { get; set; }
    public int ObjectCount { get; set; }
    public long ProjectSizeBytes { get; set; }
    public long AssetsSizeBytes { get; set; }
}
