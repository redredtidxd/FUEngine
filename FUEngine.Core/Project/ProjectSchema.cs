namespace FUEngine.Core;

/// <summary>
/// Versión de esquema del archivo de proyecto (<c>Project.FUE</c>): migraciones solo aditivas; subir al añadir campos nuevos al JSON.
/// </summary>
public static class ProjectSchema
{
    /// <summary>Valor que escribe el motor actual en proyectos nuevos y tras migración.</summary>
    public const int CurrentFormatVersion = 2;
}
