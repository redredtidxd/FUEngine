using System.Collections.Generic;
using FUEngine.Core;

namespace FUEngine.Editor;

/// <summary>
/// Puente de migración por <see cref="FUEngine.Core.ProjectInfo.ProjectFormatVersion"/> (<c>projectFormatVersion</c> en el .FUE).
/// Regla: solo avanzar de N a N+1 con <see cref="Migrate_Step"/>; está prohibido saltar versiones sin pasar por los pasos intermedios.
/// Los modelos viven en Core; la orquestación y los pasos concretos están aquí para no acoplar el dominio a la app WPF.
/// </summary>
public static class ProjectFormatMigration
{
    public static bool NeedsUpgrade(ProjectInfo project) =>
        project.ProjectFormatVersion < ProjectSchema.CurrentFormatVersion;

    /// <summary>Aplica migraciones en memoria (p. ej. reproductor) sin escribir disco.</summary>
    public static void ApplySilentInMemory(ProjectInfo project)
    {
        if (!NeedsUpgrade(project)) return;
        ApplySafeUpgrade(project, new List<string>());
    }

    /// <summary>Aplica migración hasta <see cref="ProjectSchema.CurrentFormatVersion"/> y rellena advertencias.</summary>
    public static void ApplySafeUpgrade(ProjectInfo project, List<string> warnings)
    {
        var migrated = false;
        while (project.ProjectFormatVersion < ProjectSchema.CurrentFormatVersion)
        {
            migrated = true;
            var from = project.ProjectFormatVersion;
            var next = from + 1;
            Migrate_Step(project, from, next, warnings);
            project.ProjectFormatVersion = next;
        }
        if (migrated)
            project.EngineVersion = EngineVersion.Current;
    }

    /// <summary>Un paso explícito from → to (p. ej. Migrate_0_To_1, Migrate_1_To_2).</summary>
    private static void Migrate_Step(ProjectInfo project, int fromVersion, int toVersion, List<string> warnings)
    {
        if (toVersion != fromVersion + 1)
            throw new InvalidOperationException($"Migración interna inválida: se esperaba un paso unitario, recibido {fromVersion}→{toVersion}.");

        switch (fromVersion)
        {
            case 0:
                Migrate_0_To_1(project, warnings);
                break;
            case 1:
                Migrate_1_To_2(project, warnings);
                break;
            default:
                warnings.Add($"Migración genérica v{fromVersion}→v{toVersion}: sin transformación específica (solo versión en manifiesto).");
                break;
        }
    }

    /// <summary>Proyectos sin <c>projectFormatVersion</c> en JSON (0) o manifiestos muy antiguos.</summary>
    private static void Migrate_0_To_1(ProjectInfo project, List<string> warnings)
    {
        if (string.IsNullOrWhiteSpace(project.EngineVersion))
            warnings.Add("Se rellenó el campo de versión del motor (faltaba en el archivo anterior).");
        warnings.Add("Formato interno del proyecto actualizado de v0 a v1 (metadatos; no se elimina contenido de mapas ni scripts).");
    }

    /// <summary>Paso v1→v2: reservado para cambios de esquema del manifiesto; hoy solo ancla la cadena incremental.</summary>
    private static void Migrate_1_To_2(ProjectInfo project, List<string> warnings)
    {
        _ = project;
        warnings.Add("Formato interno del proyecto actualizado de v1 a v2. Los campos nuevos del manifiesto usan valores por defecto al cargar; mapas, objetos y scripts en disco no se modifican en este paso.");
    }
}
