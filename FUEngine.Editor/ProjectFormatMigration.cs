using System.Collections.Generic;
using FUEngine.Core;

namespace FUEngine.Editor;

/// <summary>Migraciones solo aditivas del JSON de proyecto; no elimina datos.</summary>
public static class ProjectFormatMigration
{
    public static bool NeedsUpgrade(ProjectInfo project) =>
        project.ProjectFormatVersion < ProjectSchema.CurrentFormatVersion;

    /// <summary>Aplica migraciones en memoria (p. ej. reproductor) sin escribir disco.</summary>
    public static void ApplySilentInMemory(ProjectInfo project)
    {
        if (!NeedsUpgrade(project)) return;
        var _ = new List<string>();
        ApplySafeUpgrade(project, _);
    }

    /// <summary>Aplica migración hasta <see cref="ProjectSchema.CurrentFormatVersion"/> y rellena advertencias.</summary>
    public static void ApplySafeUpgrade(ProjectInfo project, List<string> warnings)
    {
        var migrated = false;
        while (project.ProjectFormatVersion < ProjectSchema.CurrentFormatVersion)
        {
            migrated = true;
            var next = project.ProjectFormatVersion + 1;
            MigrateToVersion(project, next, warnings);
            project.ProjectFormatVersion = next;
        }
        if (migrated)
            project.EngineVersion = EngineVersion.Current;
    }

    private static void MigrateToVersion(ProjectInfo project, int targetVersion, List<string> warnings)
    {
        switch (targetVersion)
        {
            case 1:
                if (string.IsNullOrWhiteSpace(project.EngineVersion))
                {
                    warnings.Add("Se rellenó el campo de versión del motor (faltaba en el archivo anterior).");
                }
                warnings.Add("Formato interno del proyecto actualizado a v1 (solo metadatos; no se elimina contenido).");
                break;
        }
    }
}
