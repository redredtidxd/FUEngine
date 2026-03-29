namespace FUEngine.Service.Project;

/// <summary>
/// Valida la integridad de un proyecto: archivos faltantes, referencias rotas,
/// assets huérfanos, scripts sin registrar, etc. Útil al abrir un proyecto
/// y como diagnóstico bajo demanda.
/// </summary>
public interface IProjectIntegrityChecker
{
    /// <summary>Ejecuta la comprobación y devuelve una lista de problemas encontrados.</summary>
    IReadOnlyList<ProjectIssue> Check(string projectDirectory);
}

/// <summary>Problema detectado en un proyecto.</summary>
public sealed record ProjectIssue(
    ProjectIssueSeverity Severity,
    string Category,
    string Message,
    string? FilePath = null);

public enum ProjectIssueSeverity
{
    Info,
    Warning,
    Error
}
