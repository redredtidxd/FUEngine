using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace FUEngine;

public class RecentProjectInfo : INotifyPropertyChanged
{
    private string _path = "";
    private DateTime _lastOpened;

    public string Path { get => _path; set { if (_path == value) return; _path = value ?? ""; OnPropertyChanged(); OnPropertyChanged(nameof(ShortPath)); } }
    public string Name { get; set; } = "";
    /// <summary>Descripción del proyecto (vacío por defecto; JSON antiguo puede deserializar null).</summary>
    public string? Description { get; set; } = "";
    public DateTime LastOpened { get => _lastOpened; set { if (_lastOpened == value) return; _lastOpened = value; OnPropertyChanged(); OnPropertyChanged(nameof(LastOpenedDisplay)); } }
    /// <summary>Ruta corta para mostrar (carpeta del proyecto o path truncado).</summary>
    public string ShortPath
    {
        get
        {
            if (string.IsNullOrEmpty(Path)) return "";
            try
            {
                var dir = System.IO.Path.GetDirectoryName(Path);
                if (string.IsNullOrEmpty(dir)) return Path;
                var name = System.IO.Path.GetFileName(dir.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar));
                if (!string.IsNullOrEmpty(name)) return name;
                return dir.Length > 40 ? "..." + dir.Substring(dir.Length - 37) : dir;
            }
            catch { return Path.Length > 40 ? "..." + Path.Substring(Path.Length - 37) : Path; }
        }
    }
    /// <summary>Texto para "última vez abierto" (ej. "Hace 2 días" o fecha corta).</summary>
    public string LastOpenedDisplay
    {
        get
        {
            var diff = DateTime.Now - LastOpened;
            if (diff.TotalMinutes < 1) return "Ahora";
            if (diff.TotalHours < 1) return $"Hace {(int)diff.TotalMinutes} min";
            if (diff.TotalDays < 1) return $"Hace {(int)diff.TotalHours} h";
            if (diff.TotalDays < 2) return "Ayer";
            if (diff.TotalDays <= 7) return $"Hace {(int)diff.TotalDays} días";
            return LastOpened.ToString("dd/MM/yy");
        }
    }
    /// <summary>Versión de FUEngine con la que se abrió por última vez.</summary>
    public string? OpenedWithEngineVersion { get; set; }
    /// <summary>Texto para mostrar versión del motor (vacío si no hay).</summary>
    public string VersionDisplay => string.IsNullOrEmpty(OpenedWithEngineVersion) ? "" : " · Motor: " + OpenedWithEngineVersion;

    /// <summary>Proyecto fijado: se mantiene arriba en la lista.</summary>
    public bool IsPinned { get; set; }
    /// <summary>Etiquetas personalizadas: Prototipo, Trabajo, Experimento, Demo, etc.</summary>
    public List<string> Tags { get; set; } = new();
    /// <summary>Tipo/género del proyecto: Pixel Art, RPG, Plataforma, FPS, Shooter, etc.</summary>
    public string? ProjectType { get; set; }
    /// <summary>Resolución objetivo del proyecto (ej. 320x180).</summary>
    public string? Resolution { get; set; }
    /// <summary>Frame rate objetivo del proyecto.</summary>
    public int? Fps { get; set; }

    /// <summary>Texto para badge: resolución y FPS (ej. "320×180 · 60 FPS").</summary>
    public string? ResolutionFpsDisplay
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(Resolution)) parts.Add(Resolution);
            if (Fps.HasValue && Fps.Value > 0) parts.Add(Fps.Value + " FPS");
            return parts.Count == 0 ? null : string.Join(" · ", parts);
        }
    }

    /// <summary>Última modificación del archivo (rellenado al cargar).</summary>
    public DateTime LastModified { get; set; }
    /// <summary>Tamaño total del proyecto en bytes (rellenado al cargar).</summary>
    public long ProjectSizeBytes { get; set; }
    /// <summary>Número de escenas (rellenado al cargar).</summary>
    public int SceneCount { get; set; }
    /// <summary>Tamaño total de assets en bytes (rellenado al cargar).</summary>
    public long AssetsSizeBytes { get; set; }

    /// <summary>Texto formateado de última modificación (ej. "17/03/25 14:30").</summary>
    public string LastModifiedDisplay => LastModified == default ? "—" : LastModified.ToString("dd/MM/yy HH:mm");
    /// <summary>Texto formateado de tamaño (ej. "12,5 MB").</summary>
    public string ProjectSizeDisplay => FormatSize(ProjectSizeBytes);
    /// <summary>Texto formateado de assets (ej. "3,2 MB").</summary>
    public string AssetsSizeDisplay => FormatSize(AssetsSizeBytes);

    private static string FormatSize(long bytes)
    {
        if (bytes <= 0) return "—";
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }

    private ImageSource? _preview;
    /// <summary>Mini-preview del mapa (48x48), generado al cargar el dashboard.</summary>
    public ImageSource? Preview { get => _preview; set { _preview = value; OnPropertyChanged(); } }
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
