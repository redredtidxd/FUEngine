namespace FUEngine.Core;

/// <summary>Comprueba si un proyecto es compatible con la versión actual del motor. Evita abrir proyectos incompatibles.</summary>
public static class ProjectEngineCompatibilityChecker
{
    public static bool IsCompatible(string? projectEngineVersion)
    {
        if (string.IsNullOrEmpty(projectEngineVersion)) return true;
        return projectEngineVersion == EngineVersion.Current;
    }

    public static string GetWarningMessage(string? projectEngineVersion)
    {
        if (IsCompatible(projectEngineVersion)) return "";
        return $"El proyecto fue guardado con motor {projectEngineVersion}. Motor actual: {EngineVersion.Current}. Puede haber incompatibilidades.";
    }
}
