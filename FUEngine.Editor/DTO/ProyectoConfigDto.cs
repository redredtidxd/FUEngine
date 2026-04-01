namespace FUEngine.Editor;

/// <summary>
/// DTO para proyecto.config (JSON en la raíz del proyecto): nombre, logo, fechas, plantilla, autoguardado.
/// La app sincroniza rutas y crea carpetas faltantes al abrir.
/// </summary>
public class ProyectoConfigDto
{
    public string Nombre { get; set; } = "NuevoProyecto";
    /// <summary>Ruta relativa al proyecto (ej: logo.png). Siempre resolver con Path.Combine(ProjectRoot, Logo).</summary>
    public string Logo { get; set; } = "logo.png";
    public string? CreadoEn { get; set; }
    public string? UltimaModificacion { get; set; }
    /// <summary>Ruta absoluta o relativa donde está el proyecto (se actualiza al abrir si cambió).</summary>
    public string? UltimaRuta { get; set; }
    public string Plantilla { get; set; } = "Blank";
    public bool AutoguardadoActivo { get; set; } = true;
    public int IntervaloAutoguardadoMin { get; set; } = 5;
    public string? Descripcion { get; set; }
    public string? Autor { get; set; }
    public string Version { get; set; } = "0.1";
    /// <summary>Cantidad máxima de archivos de autoguardado por tipo.</summary>
    public int MaxBackupsAutoguardado { get; set; } = 10;
    /// <summary>Guardar solo si hubo cambios.</summary>
    public bool GuardarSoloCambios { get; set; } = true;
    /// <summary>Ruta de recursos externos (opcional).</summary>
    public string? RutaRecursosExternos { get; set; }
    /// <summary>Carpeta de exportación/build (relativa o absoluta).</summary>
    public string? RutaExportacionBuild { get; set; }
    public string FormatoArchivoPorDefecto { get; set; } = "JSON";
    public string NombreArchivoPorDefecto { get; set; } = "mapa_";
    /// <summary>Color del proyecto en listas del editor (hex o nombre).</summary>
    public string? ColorProyectoUI { get; set; }
    public bool IconosRapidos { get; set; } = true;
    /// <summary>Etiquetas / categorías para filtrar proyectos.</summary>
    public List<string>? Etiquetas { get; set; }
    /// <summary>Historial de cambios o notas de versión (opcional, lista simple).</summary>
    public List<string>? NotasCambios { get; set; }
    /// <summary>Historial de cambios con versión y fecha (para actualizaciones del proyecto).</summary>
    public List<EntradaHistorialDto>? HistorialCambios { get; set; }
}

/// <summary>Una entrada del historial de cambios del proyecto (versión, fecha, descripción).</summary>
public class EntradaHistorialDto
{
    public string Version { get; set; } = "0.1";
    public string? Fecha { get; set; }
    public string? Descripcion { get; set; }
}
